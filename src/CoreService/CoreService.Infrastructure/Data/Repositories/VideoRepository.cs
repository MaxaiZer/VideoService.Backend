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

        public async Task<List<ViewableVideoMetadata>> FindViewableAsync(VideoSearchParameters parameters,
            CancellationToken cancellationToken = default)
        {
            var query = context.Videos.Where(video => video.Processed);
            
            if (!string.IsNullOrEmpty(parameters.UserId))
            {
                query = query.Where(video => video.UserId == parameters.UserId);
            }
            
            if (!string.IsNullOrWhiteSpace(parameters.SearchQuery))
            {
                const float minSimilarityThreshold = 0.1f;
                query = query
                    .Where(video => EF.Functions.TrigramsSimilarity(video.Name, parameters.SearchQuery) >= minSimilarityThreshold)
                    .OrderByDescending(video => EF.Functions.TrigramsSimilarity(video.Name, parameters.SearchQuery));
            }

            return await query.Join(context.Users, 
                    video => video.UserId, 
                    user => user.Id,
                    (video, user) => new ViewableVideoMetadata(
                        video.Id,
                        user.Id,
                        user.UserName,
                        video.Name,
                        video.Description,
                        video.CreatedAt))
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}