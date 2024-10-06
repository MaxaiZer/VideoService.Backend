using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.Jwt;

public class JwtConfiguration
{
    [Required]
    public static string Section => "Jwt";

    [Required]
    public string Secret { get; init; }

    [Required]
    public string Issuer { get; init; }
    
    [Required]
    public string Audience { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "AccessLifetime must be greater than 0.")]
    public int AccessLifetime { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "RefreshLifetime must be greater than 0.")]
    public int RefreshLifetime { get; init; }
}