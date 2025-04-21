using Domain.Interfaces.Repositories;

namespace CoreService.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IVideoRepository Videos { get; }
        IVideoProcessingRequestRepository VideoProcessingRequests { get; }

        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
