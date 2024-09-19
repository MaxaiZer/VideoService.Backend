namespace Domain.Entities;

public class VideoProcessingRequest
{
    public enum ProcessingStatus
    {
        Appending,   
        Processing, 
        Finished   
    }
    
    public string Id { get; private set; }
    public string VideoId { get; private set; }
    public ProcessingStatus Status { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }

    private VideoProcessingRequest() { } // for ORM

    public VideoProcessingRequest(string id, string videoId, ProcessingStatus status)
    {
        Id = id;
        VideoId = videoId;
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}