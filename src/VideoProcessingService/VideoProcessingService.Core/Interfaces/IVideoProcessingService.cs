using Shared.Messages;

namespace VideoProcessingService.Core.Interfaces
{
    public interface IVideoProcessingService
    {
        public Task ProcessAndStoreVideoAsync(VideoReadyForProcessing request);
    }
}
