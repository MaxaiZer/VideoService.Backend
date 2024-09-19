using CoreService.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Identity
{
    public class ApplicationUser: IdentityUser, IApplicationUser
    {
        public string? RefreshToken { get; set; }
        public DateTimeOffset RefreshTokenExpiryTime { get; set; }
    }
}
