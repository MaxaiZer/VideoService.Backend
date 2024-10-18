using System.Security.Claims;
using CoreService.Application.Common.Models;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces.Services;
using CoreService.Infrastructure.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CoreService.Api.Controllers
{  
    [ApiController]
    [Route("api/auth")]
    public class AuthController: ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtConfiguration _jwtConfig;
        
        private const string refreshTokenCookie = "refreshToken";
        
        public AuthController(IAuthService service, IOptions<JwtConfiguration> jwtConfig)
        {
            _authService = service;
            _jwtConfig = jwtConfig.Value;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="userForRegistration">User registration details.</param>
        /// <returns>HTTP 201 status code if the registration is successful; otherwise, returns a 400 status code with validation errors.</returns>
        /// <response code="201">User registered successfully.</response>
        /// <response code="400">Registration failed due to validation errors.</response>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
        {
            var result = await _authService.RegisterUser(userForRegistration);
            if (result.Succeeded) return StatusCode(201);
            
            foreach (var error in result.Errors)
            {
                ModelState.TryAddModelError(string.Empty, error);
            }
            return BadRequest(ModelState);
        }

        /// <summary>
        /// Authenticates a user and generates access and refresh tokens.
        /// </summary>
        /// <param name="userDto">User authentication details.</param>
        /// <returns>HTTP 200 status code with tokens if authentication is successful; otherwise, returns a 401 status code if authentication fails.</returns>
        /// <response code="200">Authentication successful, access + refresh tokens returned.</response>
        /// <response code="401">Authentication failed.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDto userDto)
        {
            var (user, result) = await _authService.ValidateUser(userDto);
            if (!result)
                return Unauthorized();
            
            var tokenPair = await _authService.CreateTokens(user);
            
            HttpContext.Response.Cookies.Append(refreshTokenCookie, tokenPair.RefreshToken, GetRefreshTokenCookieOptions());
            return Ok(new AccessTokenDto(tokenPair.AccessToken));
        }
        /// <summary>
        /// Refreshes the access token using the provided refresh token.
        /// </summary>
        /// <param name="tokenDto">Current access and refresh tokens.</param>
        /// <returns>HTTP 200 status code with updated tokens if refresh is successful; otherwise, returns a 400 status code if the tokens are invalid.</returns>
        /// <response code="200">Access token refreshed successfully.</response>
        /// <response code="400">Token refresh failed due to invalid tokens.</response>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] AccessTokenDto tokenDto)
        {
            var refreshToken = HttpContext.Request.Cookies[refreshTokenCookie];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest("Refresh token is missing.");
            }
            
            var tokenPair = new TokenPair(tokenDto.AccessToken, refreshToken);
            tokenPair = await _authService.RefreshAccessToken(tokenPair);
            
            HttpContext.Response.Cookies.Append(refreshTokenCookie, tokenPair.RefreshToken, GetRefreshTokenCookieOptions());
            return Ok(new AccessTokenDto(tokenPair.AccessToken));
        }
        
        /// <summary>
        /// Revokes the refresh token using the provided access token.
        /// </summary>
        /// <returns>HTTP 200 status code if revoke is successful; otherwise, returns a 401 status code if authentication fails.</returns>
        /// <response code="200">Refresh token revoked successfully.</response>
        /// <response code="401">Authentication failed.</response>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User authentication required.");
            
            await _authService.RevokeRefreshToken(userId);
            Response.Cookies.Delete(refreshTokenCookie);
            return Ok();
        }
        
        private CookieOptions GetRefreshTokenCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddSeconds(_jwtConfig.RefreshLifetime)
            };
        }
        
        //Todo: change password with revoking tokens
    }
}
