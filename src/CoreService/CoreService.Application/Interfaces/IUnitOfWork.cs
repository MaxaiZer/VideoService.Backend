using Domain.Interfaces.Repositories;

namespace CoreService.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IVideoRepository Videos { get; }
        IVideoProcessingRequestRepository VideoProcessingRequests { get; }

        void BeginTransaction();

        void CommitTransaction();

        void RollbackTransaction();
        void Save();
    }
}
