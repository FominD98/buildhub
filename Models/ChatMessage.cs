using System;

namespace BuildHub.Models
{
    public enum MessageRole
    {
        User,
        Assistant,
        System
    }

    public class ChatMessage
    {
        public Guid Id { get; set; }
        public MessageRole Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string? AgentName { get; set; }
        public bool IsThinking { get; set; }

        public ChatMessage()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
            Content = string.Empty;
        }

        public ChatMessage(MessageRole role, string content, string? agentName = null)
        {
            Id = Guid.NewGuid();
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
            AgentName = agentName;
            IsThinking = false;
        }
    }
}
