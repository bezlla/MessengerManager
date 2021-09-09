﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MessengerManager.Core.Configurations.Telegram;
using MessengerManager.Core.Configurations.Vk;
using MessengerManager.Core.Handlers.TelegramHandlers;
using MessengerManager.Core.Services.TelegramManager;
using MessengerManager.Domain.Entities;
using MessengerManager.Domain.Interfaces;
using MessengerManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace MessengerManager
{
    public class MessengerManagerServiceHost
    {
        private readonly IConfiguration _configuration;

        public IServiceProvider ServiceProvider { get; set; }

        public MessengerManagerServiceHost(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public virtual async Task Start(CancellationToken token)
        {
            var serviceCollection = new ServiceCollection();

            AddLogging(serviceCollection);
            AddDbContext(serviceCollection);
            AddServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            
            await ConfigureDbContext(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            
            ConfigureHandlers(token);
        }

        public virtual void AddLogging(IServiceCollection serviceCollection)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("APP", "MessengerManager")
                .WriteTo.Console()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("IdentityServer4", Serilog.Events.LogEventLevel.Error)
                .CreateLogger();
            serviceCollection.AddLogging(x => x.AddSerilog());
        }

        public virtual void AddDbContext(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<MessengerManagerDbContext>(opt =>
            {
                opt.UseNpgsql(_configuration["POSTGRESQL"]);
            });
        }

        public virtual void AddRepositories(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IGenericRepository<ChatThreadEntity>, EfGenericRepository<ChatThreadEntity>>();
            serviceCollection.AddScoped<IGenericRepository<MessageEntity>, EfGenericRepository<MessageEntity>>();
        }
        
        public virtual async Task ConfigureDbContext(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IUnitOfWork>(x => x.GetRequiredService<MessengerManagerDbContext>());
                
            var dbContext = ServiceProvider.GetRequiredService<MessengerManagerDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        public virtual void ConfigureHandlers(CancellationToken token)
        {
            var telegramBotClient = ServiceProvider.GetRequiredService<ITelegramBotClient>();
            var telegramMessageHandler = ServiceProvider.GetRequiredService<TelegramMessageHandler>();

            BaseTelegramHandler.StartHandler(telegramBotClient, UpdateType.Message,
                telegramMessageHandler.UpdateHandler, telegramMessageHandler.ErrorHandler, token)
                .ConfigureAwait(false);
        }
        public virtual void AddServices(IServiceCollection serviceCollection)
        {
            AddRepositories(serviceCollection);
            
            AddTelegramHandlers(serviceCollection);
        }

        private async Task AddVkHandlers(IServiceCollection serviceCollection)
        {
            var vkConfiguration = _configuration.GetSection(nameof(VkConfiguration))
                .Get<VkConfiguration>();

            var vkClient = new VkApi();
            await vkClient.AuthorizeAsync(new ApiAuthParams
            {
                ApplicationId = vkConfiguration.ApplicationId,
                Login = vkConfiguration.Login,
                Password = vkConfiguration.Password,
                Settings = Settings.All
            });

            serviceCollection.AddSingleton<IVkApi>(vkClient);
        }

        private void AddTelegramHandlers(IServiceCollection serviceCollection)
        {
            var telegramConfiguration = _configuration.GetSection(nameof(TelegramConfiguration))
                .Get<TelegramConfiguration>();

            serviceCollection.Configure<TelegramConfiguration>(opt =>
            {
                opt.Token = telegramConfiguration.Token;
                opt.MainChatId = telegramConfiguration.MainChatId;
                opt.SupportChatId = telegramConfiguration.SupportChatId;
            });

            var telegramClient = new TelegramBotClient(telegramConfiguration.Token);

            serviceCollection.AddSingleton<ITelegramBotClient>(telegramClient);
            serviceCollection.AddScoped<ITelegramBotManager, TelegramBotManager>();

            serviceCollection.AddSingleton<TelegramMessageHandler>();
        }
    }
}