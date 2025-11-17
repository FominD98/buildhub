using System;

namespace BuildHub.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        public Project()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            LastModifiedAt = DateTime.Now;
        }

        public Project(string name, string description) : this()
        {
            Name = name;
            Description = description;
        }

        // Вычисляем текстовую дату для группировки
        public string GetDateGroup()
        {
            var now = DateTime.Now;
            var daysDiff = (now.Date - CreatedAt.Date).Days;

            return daysDiff switch
            {
                0 => "Today",
                1 => "Yesterday",
                <= 3 => "3 days ago",
                <= 7 => "7 days ago",
                <= 30 => "This month",
                _ => "Older"
            };
        }
    }
}
