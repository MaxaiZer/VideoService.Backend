using CoreService.Application.Interfaces;
using MassTransit;
using Shared.Messages;

namespace CoreService.Infrastructure.MessageBroker;

public class EventBus : IEventBus
{
    private readonly IPublishEndpoint _endpoint;

    public EventBus(IPublishEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class {
        
        if (message is VideoReadyForProcessing processing)
            Console.WriteLine("Message sended! RequestId: " + processing.RequestId);
       
        return _endpoint.Publish(message, cancellationToken);
    }
}