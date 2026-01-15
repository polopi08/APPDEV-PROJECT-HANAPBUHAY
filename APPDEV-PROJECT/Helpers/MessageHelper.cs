using System;
using System.Collections.Generic;
using System.Linq;

namespace APPDEV_PROJECT.Helpers
{
    public class MessageHelper
    {
        public class Conversation
        {
            public int ConversationId { get; set; }
            public int WorkerId { get; set; }
            public string WorkerName { get; set; }
            public string LastMessage { get; set; }
            public DateTime LastMessageTime { get; set; }
            public bool IsOnline { get; set; }
        }

        public class Message
        {
            public int MessageId { get; set; }
            public int ConversationId { get; set; }
            public int SenderId { get; set; }
            public string SenderName { get; set; }
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsFromClient { get; set; }
        }

        // Mock data for conversations
        private static List<Conversation> mockConversations = new()
        {
            new() { ConversationId = 1, WorkerId = 1, WorkerName = "Kyle Bernido", LastMessage = "Can you start tomorrow?", LastMessageTime = DateTime.Now.AddHours(-2), IsOnline = true },
            new() { ConversationId = 2, WorkerId = 2, WorkerName = "Vivian Yambao", LastMessage = "Thank you for the project!", LastMessageTime = DateTime.Now.AddHours(-5), IsOnline = false },
            new() { ConversationId = 3, WorkerId = 3, WorkerName = "Viaani Ubalde", LastMessage = "I'll be there by 10 AM", LastMessageTime = DateTime.Now.AddHours(-1), IsOnline = true },
            new() { ConversationId = 4, WorkerId = 5, WorkerName = "Giselle Valdez", LastMessage = "Plumbing job completed!", LastMessageTime = DateTime.Now.AddDays(-1), IsOnline = false },
            new() { ConversationId = 5, WorkerId = 6, WorkerName = "Sophia Cutue", LastMessage = "Looking forward to working with you", LastMessageTime = DateTime.Now.AddHours(-3), IsOnline = true }
        };

        private static List<Message> mockMessages = new()
        {
            new() { MessageId = 1, ConversationId = 1, SenderId = 1, SenderName = "Kyle Bernido", Content = "Can you start tomorrow?", Timestamp = DateTime.Now.AddHours(-2), IsFromClient = false },
            new() { MessageId = 2, ConversationId = 1, SenderId = 0, SenderName = "You", Content = "Yes, I can start tomorrow morning", Timestamp = DateTime.Now.AddHours(-1.5), IsFromClient = true }
        };

        /// <summary>
        /// Get all conversations for a client
        /// </summary>
        public static List<Conversation> GetConversations()
        {
            return mockConversations.OrderByDescending(c => c.LastMessageTime).ToList();
        }

        /// <summary>
        /// Get messages for a specific conversation
        /// </summary>
        public static List<Message> GetMessages(int conversationId)
        {
            return mockMessages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.Timestamp).ToList();
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public static Message SendMessage(int conversationId, string content)
        {
            var message = new Message
            {
                MessageId = mockMessages.Count + 1,
                ConversationId = conversationId,
                SenderId = 0, // Client ID
                SenderName = "You",
                Content = content,
                Timestamp = DateTime.Now,
                IsFromClient = true
            };

            mockMessages.Add(message);

            // Update conversation's last message
            var conversation = mockConversations.FirstOrDefault(c => c.ConversationId == conversationId);
            if (conversation != null)
            {
                conversation.LastMessage = content;
                conversation.LastMessageTime = DateTime.Now;
            }

            return message;
        }

        /// <summary>
        /// Search conversations
        /// </summary>
        public static List<Conversation> SearchConversations(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
                return GetConversations();

            return mockConversations
                .Where(c => c.WorkerName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();
        }
    }
}
