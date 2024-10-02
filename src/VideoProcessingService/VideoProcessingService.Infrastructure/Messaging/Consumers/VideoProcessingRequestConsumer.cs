using MassTransit;
using Shared.Messages;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.Messaging.Consumers;

public class VideoProcessingRequestConsumer: IConsumer<VideoReadyForProcessing>
{
    private readonly IVideoProcessingService _videoProcessingService;

    public VideoProcessingRequestConsumer(IVideoProcessingService videoProcessingService)
    {
        _videoProcessingService = videoProcessingService;
    }

    public async Task Consume(ConsumeContext<VideoReadyForProcessing> context)
    {
        Console.WriteLine("Message consumed! RequestId: " + context.Message.RequestId);
        await _videoProcessingService.ProcessAndStoreVideoAsync(context.Message);
    }
}