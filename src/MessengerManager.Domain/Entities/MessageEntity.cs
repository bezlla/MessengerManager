﻿using System;

namespace MessengerManager.Domain.Entities
{
    public class MessageEntity : Entity
    {
        private const int ChatThreadNameLength = 128;
        private const int OwnerLength = 256;
        private const int TextLength = 1024;
        private MessageEntity(long vkPeerId)
        {
            VkPeerId = vkPeerId;
        }

        public MessageEntity(string owner,
            string text,
            string chatThreadName,
            DateTime date, long vkPeerId)
        {
            Owner = owner;
            Text = text;
            ChatThreadName = chatThreadName; 
            Date = date;
            VkPeerId = vkPeerId;
        }
        public string Owner { get; private set; }
        public string Text { get; private set; }
        public string ChatThreadName { get; private set; }
        public long VkPeerId { get; private set; }
        public DateTime Date { get; private set; }
        public bool Sent { get; private set; }

        public void SetSent()
        {
            Sent = true;
            SetUpdated();
        }
    }
}