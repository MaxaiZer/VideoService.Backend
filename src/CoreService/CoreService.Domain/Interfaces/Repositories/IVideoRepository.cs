using Domain.Entities;

namespace Domain.Interfaces.Repositories
{
    public interface IVideoRepository: IRepositoryBase<Video>
    {
        Task<List<Video>> FindAsync(string searchQuery, int pageNumber, int pageSize, 
            CancellationToken cancellationToken = default);
    }
}
