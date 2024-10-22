using CoreService.Application.Common.Models;
using CoreService.Application.Dto;

namespace CoreService.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result> RegisterUser(UserForRegistrationDto userForRegistration);

        Task<(IApplicationUser?, bool)> ValidateUser(UserForAuthenticationDto userForAuth);

        public Task<TokenPair> CreateTokens(IApplicationUser user, bool updateRefreshExpiryTime);
        
        Task<TokenPair> RefreshAccessToken(string refreshToken);
        
        Task RevokeRefreshToken(string userId);
    }
}
