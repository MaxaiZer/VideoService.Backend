using System.Security.Claims;
using CoreService.Application.Common.Exceptions;
using CoreService.Application.Common.Helpers;
using CoreService.Application.Common.Models;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Services;

namespace CoreService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IIdentityService _identityService;
        private readonly ILoggerManager _logger;
        private readonly IJwtService _jwtService;

        public AuthService(ILoggerManager logger, IIdentityService identityService, IJwtService jwtService)
        {
            _logger = logger;
            _identityService = identityService;
            _jwtService = jwtService;
        }

        public async Task<Result> RegisterUser(UserForRegistrationDto userForRegistration)
        {
            var (result, _) =
                await _identityService.CreateUserAsync(userForRegistration.UserName, userForRegistration.Password);

            return result;
        }

        public async Task<(IApplicationUser?, bool)> ValidateUser(UserForAuthenticationDto userForAuth)
        {
            var user = await _identityService.GetUserByNameAsync(userForAuth.UserName);

            var result = user != null &&
                         await _identityService.CheckPasswordAsync(user, userForAuth.Password);
            if (!result)
                _logger.LogWarn($"{nameof(ValidateUser)}: Authentication failed. Wrong username or password.");
            return (user, result);
        }

        public async Task<TokenPair> CreateTokens(IApplicationUser user, bool updateRefreshExpiryTime)
        {
            var accessToken = _jwtService.CreateAccessToken(claims: [new Claim(ClaimTypes.NameIdentifier, user.Id)]);
            var refreshTokenResult = _jwtService.CreateRefreshToken();

            user.RefreshToken = TokenHelper.HashToken(refreshTokenResult.RefreshToken);
            if (updateRefreshExpiryTime)
            {
                user.RefreshTokenExpiryTime = refreshTokenResult.ExpiryTime.ToUniversalTime();
            }

            var res = await _identityService.UpdateUserAsync(user);
            if (res.Succeeded)
            {
                return new TokenPair(accessToken, refreshTokenResult.RefreshToken);
            }
            
            var error = $"{nameof(CreateTokens)}: error update user: {string.Join(", ", res.Errors)}";
            _logger.LogError(error);

            throw new CreateTokenException(error);
        }

        public async Task<TokenPair> RefreshAccessToken(string refreshToken)
        {
            var hashedToken = TokenHelper.HashToken(refreshToken);
            var user = await _identityService.GetUserByRefreshTokenAsync(hashedToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTimeOffset.Now)
                throw new RefreshTokenException();

            return await CreateTokens(user, updateRefreshExpiryTime: false);
        }

        public async Task RevokeRefreshToken(string userId)
        {
            var user = await _identityService.GetUserByIdAsync(userId);
            user.RefreshToken = null;
            var res = await _identityService.UpdateUserAsync(user);

            if (!res.Succeeded)
            {
                var errorMessages = string.Join(", ", res.Errors);
                var error = $"{nameof(CreateTokens)}: error revoke token: {errorMessages}";
                _logger.LogError(error);
            }
        }
    }
}