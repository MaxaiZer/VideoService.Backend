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

        public async Task<ViewableVideoMetadata?> FindViewableByIdAsync(string id,
            CancellationToken cancellationToken = default)
        {
            return await context.Videos.Where(video => video.Processed)
                .Where(video => video.Id == id)
                .Join(context.Users,
                    video => video.UserId,
                    user => user.Id,
                    (video, user) => new ViewableVideoMetadata(
                        video.Id,
                        user.Id,
                        user.UserName,
                        video.Name,
                        video.Description,
                        video.CreatedAt))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ViewableVideoMetadata>> FindViewableAsync(string? searchQuery, int pageNumber,
            int pageSize, CancellationToken cancellationToken = default)
        {
            var minSimilarityThreshold = 0.1;

            var query = context.Videos.Where(video => video.Processed);
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query
                    .Where(video => EF.Functions.TrigramsSimilarity(video.Name, searchQuery) >= minSimilarityThreshold)
                    .OrderByDescending(video => EF.Functions.TrigramsSimilarity(video.Name, searchQuery));
            }

            return await query.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Join(context.Users, 
                    video => video.UserId, 
                    user => user.Id,
                    (video, user) => new ViewableVideoMetadata(
                        video.Id,
                        user.Id,
                        user.UserName,
                        video.Name,
                        video.Description,
                        video.CreatedAt))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}