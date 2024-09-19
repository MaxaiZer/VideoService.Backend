using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Video
    {
        public string Id { get; init; }
        
        public string UserId { get; private set; }

        [MaxLength(100)]
        public string Name { get; private set; }

        [MaxLength(100)]
        public string Description { get; private set; }
        
        public bool Processed { get; private set;  }
        
        public DateTimeOffset CreatedAt { get; private set; }

        private Video()  { } // for ORM 

        public Video(string id, string name, string userId, string description, bool processed = false)
        {
            Id = id;
            Name = name;
            UserId = userId;
            Description = description;
            Processed = processed;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
