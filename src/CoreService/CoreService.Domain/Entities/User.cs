namespace Domain.Entities
{
    public class User
    {
        public string Id { get; private set;  }
        public string Name { get; private set; }
        
        public DateTimeOffset CreatedAt { get; private set;}

        private User() { } // for ORM
        
        public User(string id, string name)
        {
            Id = id;
            Name = name;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
