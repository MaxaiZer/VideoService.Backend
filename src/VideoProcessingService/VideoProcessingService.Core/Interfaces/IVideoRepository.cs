namespace VideoProcessingService.Core.Interfaces;

public interface IVideoRepository
{
    Task<bool> MarkVideoAsProcessed(string videoId);
}