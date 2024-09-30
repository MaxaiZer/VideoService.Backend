using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CoreService.Infrastructure.Data.Context
{
    public class RepositoryContext : IdentityDbContext<ApplicationUser> 
    {
        public DbSet<Video> Videos { get; set; }
        public DbSet<VideoProcessingRequest> VideoProcessingRequests { get; set; }

        public RepositoryContext(DbContextOptions options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<VideoProcessingRequest>()
                .Property(v => v.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (VideoProcessingRequest.ProcessingStatus)
                        Enum.Parse(typeof(VideoProcessingRequest.ProcessingStatus), v)
                );
            
           builder.Entity<Video>()
                .HasIndex(v => v.Name)
                .HasFilter($"\"{nameof(Video.Processed)}\" = true")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            
            base.OnModelCreating(builder);
        }
    }
}
