using System.Security.Claims;
using CoreService.Application.Common.Exceptions;
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
            var (result, userId) = await _identityService.
                CreateUserAsync(userForRegistration.UserName, userForRegistration.Password);
            
            //  if (result.Succeeded) ToDO: clean up
            //     await _userManager.AddToRolesAsync(user, userForRegistration.Roles);
            
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
        
        public async Task<TokenDto> CreateTokens(IApplicationUser user)
        {
            //    var claims = await GetClaims(user);  // Get claims based on the user
            var accessToken = _jwtService.CreateAccessToken(claims: [new Claim(ClaimTypes.NameIdentifier, user.Id)]);
            var refreshTokenResult = _jwtService.CreateRefreshToken();

            user.RefreshToken = refreshTokenResult.RefreshToken;
            user.RefreshTokenExpiryTime = refreshTokenResult.ExpiryTime.ToUniversalTime();

            var res = await _identityService.UpdateUserAsync(user);
            if (res.Succeeded)
            {
                return new TokenDto(accessToken, refreshTokenResult.RefreshToken);
            }
            
            var errorMessages = string.Join(", ", res.Errors);
            var error = $"{nameof(CreateTokens)}: error update user: {errorMessages}";
            _logger.LogError(error);
                
            throw new Exception(error);
        }

        public async Task<TokenDto> RefreshAccessToken(TokenDto tokenDto)
        {
            ClaimsPrincipal principal;
            
            try
            {
                principal = _jwtService.GetPrincipalFromToken(tokenDto.AccessToken);
            }
            catch (Exception e)
            {
                throw new RefreshTokenBadRequest();
            }

            var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _identityService.GetUserByIdAsync(id);
            
            if (user == null || user.RefreshToken != tokenDto.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTimeOffset.Now)
                throw new RefreshTokenBadRequest();

            var accessToken = _jwtService.CreateAccessToken(claims: []);
            return tokenDto with { AccessToken = accessToken };
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
