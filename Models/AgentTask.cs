using System;

namespace BuildHub.Models
{
    public enum TaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class AgentTask
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public string UserPrompt { get; set; } = string.Empty;
        public string? Response { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan? Duration { get; set; }

        public AgentTask()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            Status = TaskStatus.Pending;
        }

        public AgentTask(Guid agentId, string userPrompt) : this()
        {
            AgentId = agentId;
            UserPrompt = userPrompt;
        }
    }
}
