using Domain.Interfaces.Repositories;
using Domain.Entities;
using CoreService.Infrastructure.Data.Context;

namespace CoreService.Infrastructure.Data.Repositories
{
    public class VideoRepository : RepositoryBase<Video>, IVideoRepository
    {
        public VideoRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
