using Domain.Entities;

namespace Domain.Interfaces.Repositories
{
    public interface IVideoRepository: IRepositoryBase<Video>
    {
        Task<ViewableVideoMetadata?> FindViewableByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<List<ViewableVideoMetadata>> FindViewableAsync(string searchQuery, int pageNumber, int pageSize, 
            CancellationToken cancellationToken = default);
    }
}
