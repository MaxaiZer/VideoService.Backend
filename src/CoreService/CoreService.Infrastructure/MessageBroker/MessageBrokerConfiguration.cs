using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.MessageBroker;

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