using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.Jwt;

public class JwtConfiguration
{
    [Required]
    public static string Section => "Jwt";

    [Required]
    public string Secret { get; set; }

    [Required]
    public string ValidIssuer { get; set; }
    [Required]
    public string ValidAudience { get; set; }
    
    [Required]
    public string AccessTokenExpirationMinutes { get; set; }
    
    [Required]
    public string RefreshTokenExpirationDays { get; set; }
}