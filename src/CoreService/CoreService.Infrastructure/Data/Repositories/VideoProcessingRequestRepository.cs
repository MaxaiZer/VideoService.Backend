using Domain.Interfaces.Repositories;
using Domain.Entities;
using CoreService.Infrastructure.Data.Context;

namespace CoreService.Infrastructure.Data.Repositories
{
    public class VideoProcessingRequestRepository : RepositoryBase<VideoProcessingRequest>, IVideoProcessingRequestRepository
    {
        public VideoProcessingRequestRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
