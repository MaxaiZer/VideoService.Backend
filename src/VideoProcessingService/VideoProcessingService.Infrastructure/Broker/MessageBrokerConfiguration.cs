using System.ComponentModel.DataAnnotations;

namespace VideoProcessingService.Infrastructure.Broker;

public class MessageBrokerConfiguration
{
    [Required]
    public static string Section => "MessageBroker";
    [Required]
    public string Host { get; set; }
    [Required]
    public string Username { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public string VideoProcessingExchangeName { get; set; }
}