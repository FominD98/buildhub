using System;
using System.Collections.Generic;
using BuildHub.Services;

namespace BuildHub.Models
{
    public class Agent
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public AiProvider Provider { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public List<AgentTask> TaskHistory { get; set; } = new List<AgentTask>();
        public AgentSettings Settings { get; set; } = new AgentSettings();

        public Agent()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }
    }

    public class AgentSettings
    {
        public double Temperature { get; set; } = 0.7;
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 60;
        public bool SaveHistory { get; set; } = true;
    }
}
