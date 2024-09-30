using Domain.Interfaces.Repositories;
using Domain.Entities;
using CoreService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Data.Repositories
{
    public class VideoRepository : RepositoryBase<Video>, IVideoRepository
    {
        public VideoRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<Video>> FindAsync(string searchQuery, int pageNumber, int pageSize, 
            CancellationToken cancellationToken = default)
        {
            var minSimilarityThreshold = 0.1;
            
            return context.Set<Video>().Where(video => video.Processed == true)
                .Where(video => EF.Functions.TrigramsSimilarity(video.Name, searchQuery) >= minSimilarityThreshold)
                .OrderByDescending(video => EF.Functions.TrigramsSimilarity(video.Name, searchQuery))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
