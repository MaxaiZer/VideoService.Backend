using VideoProcessingService.Core.Models;

namespace VideoProcessingService.Core.Interfaces;

public interface IVideoProcessingRequestRepository
{
    Task<VideoProcessingRequest?> GetById(string id);
    Task<bool> UpdateStatus(string id, VideoProcessingRequest.ProcessingStatus newStatus);
}