namespace VideoProcessingService.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IVideoProcessingRequestRepository VideoProcessingRequests { get; }
    IVideoRepository Videos { get; }
    
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
}