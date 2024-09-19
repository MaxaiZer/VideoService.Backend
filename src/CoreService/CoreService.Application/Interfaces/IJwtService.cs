using System.Security.Claims;
using CoreService.Application.Common.Models;

namespace CoreService.Application.Interfaces;

public interface IJwtService
{
    public string CreateAccessToken(List<Claim> claims);

    public RefreshTokenResult CreateRefreshToken();

    public ClaimsPrincipal GetPrincipalFromToken(string token);
}