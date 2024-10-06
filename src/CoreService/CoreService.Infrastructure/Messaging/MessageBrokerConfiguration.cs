using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.Messaging;

public class MessageBrokerConfiguration
{
    [Required]
    public static string Section => "MessageBroker";
    [Required]
    public string Host { get; init; }
    [Required]
    public string Username { get; init; }
    [Required]
    public string Password { get; init; }
    [Required]
    public string VideoProcessingExchangeName { get; init; }
}