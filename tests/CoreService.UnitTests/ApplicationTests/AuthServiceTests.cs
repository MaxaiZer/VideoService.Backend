using System.Security.Claims;
using CoreService.Application.Common.Exceptions;
using CoreService.Application.Common.Models;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces;
using CoreService.Application.Services;
using FluentAssertions;
using CoreService.Infrastructure.Identity;
using Moq;

namespace CoreService.UnitTests.ApplicationTests
{
    public class AuthServiceTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock;
        private readonly Mock<ILoggerManager> _loggerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _identityServiceMock = new Mock<IIdentityService>();
            _loggerMock = new Mock<ILoggerManager>();
            _jwtServiceMock = new Mock<IJwtService>();
            _authService = new AuthService(_loggerMock.Object, _identityServiceMock.Object, _jwtServiceMock.Object);
        }

        public ClaimsPrincipal GetValidPrincipal(string userId)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(GetClaims(userId)));
        }

        public List<Claim> GetClaims(string userId)
        {
            return new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        }

        [Fact]
        public async Task RegisterUser_ShouldReturnResult()
        {
            // Arrange
            var userForRegistration = new UserForRegistrationDto
            {
                UserName = "testUser",
                Password = "password123"
            };
            var result = Result.Success();
            _identityServiceMock
                .Setup(x => x.CreateUserAsync(userForRegistration.UserName, userForRegistration.Password))
                .ReturnsAsync((result, "userId"));

            // Act
            var registerResult = await _authService.RegisterUser(userForRegistration);

            // Assert
            registerResult.Should().Be(result);
        }

        [Fact]
        public async Task ValidateUser_ShouldReturnUserAndResult()
        {
            // Arrange
            var userForAuth = new UserForAuthenticationDto
            {
                UserName = "testUser",
                Password = "password123"
            };
            var user = new Mock<IApplicationUser>();
            _identityServiceMock
                .Setup(x => x.GetUserByNameAsync(userForAuth.UserName))
                .ReturnsAsync(user.Object);
            _identityServiceMock
                .Setup(x => x.CheckPasswordAsync(user.Object, userForAuth.Password))
                .ReturnsAsync(true);

            // Act
            var (resultUser, isValid) = await _authService.ValidateUser(userForAuth);

            // Assert
            resultUser.Should().Be(user.Object);
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateUser_ShouldLogWarning_WhenAuthenticationFails()
        {
            // Arrange
            var userForAuth = new UserForAuthenticationDto
            {
                UserName = "testUser",
                Password = "password123"
            };
            _identityServiceMock
                .Setup(x => x.GetUserByNameAsync(userForAuth.UserName))
                .ReturnsAsync((IApplicationUser)null);

            // Act
            await _authService.ValidateUser(userForAuth);

            // Assert
            _loggerMock.Verify(
                x => x.LogWarn($"{nameof(AuthService.ValidateUser)}: Authentication failed. Wrong username or password."),
                Times.Once);
        }

        [Fact]
        public async Task CreateTokens_ShouldReturnTokenDto()
        {
            // Arrange
            var user = new ApplicationUser();
            var accessToken = "accessToken";
            var refreshToken = new RefreshTokenResult("refreshToken", DateTimeOffset.Now.AddDays(7));
            _jwtServiceMock.Setup(x => x.CreateAccessToken(It.IsAny<List<Claim>>())).Returns(accessToken);
            _jwtServiceMock.Setup(x => x.CreateRefreshToken()).Returns(refreshToken);
            _identityServiceMock.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(Result.Success());

            // Act
            var tokenDto = await _authService.CreateTokens(user);

            // Assert
            tokenDto.AccessToken.Should().Be(accessToken);
            tokenDto.RefreshToken.Should().Be("refreshToken");
            user.RefreshTokenExpiryTime.Should().Be(refreshToken.ExpiryTime);
            user.RefreshToken.Should().Be("refreshToken");
        }
        
        [Fact]
        public async Task CreateTokens_WhenErrorInIdentityService_ShouldThrowException()
        {
            // Arrange
            var user = new ApplicationUser();
            var claims = new List<Claim>();
            var accessToken = "accessToken";
            var refreshToken = new RefreshTokenResult("refreshToken", DateTimeOffset.Now.AddDays(7));
            _jwtServiceMock.Setup(x => x.CreateAccessToken(claims)).Returns(accessToken);
            _jwtServiceMock.Setup(x => x.CreateRefreshToken()).Returns(refreshToken);
            _identityServiceMock.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(Result.Failure(["error!"]));

            // Act & Assert
            await _authService.Invoking(a => a.CreateTokens(user)).Should().ThrowAsync<CreateTokenException>();
        }

        [Fact]
        public async Task RefreshToken_WhenInvalidUser_ShouldThrowRefreshTokenBadRequest()
        {
            // Arrange
            var tokenDto = new AccessTokenDto("accessToken");
            _jwtServiceMock
                .Setup(x => x.GetPrincipalFromToken(tokenDto.AccessToken))
                .Returns(GetValidPrincipal(userId: Guid.NewGuid().ToString()));
            _identityServiceMock
                .Setup(x => x.GetUserByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((IApplicationUser?)null);
            var tokenPair = new TokenPair(tokenDto.AccessToken, "refreshToken");
            
            // Act & Assert
            await Assert.ThrowsAsync<RefreshTokenException>(() => _authService.RefreshAccessToken(tokenPair));
        }
        
        [Fact]
        public async Task RefreshToken_WhenInvalidToken_ShouldThrowRefreshTokenBadRequest()
        {
            // Arrange
            var tokenDto = new AccessTokenDto("accessToken");
            _jwtServiceMock
                .Setup(x => x.GetPrincipalFromToken(tokenDto.AccessToken))
                .Throws(new Exception());
            _identityServiceMock
                .Setup(x => x.GetUserByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((IApplicationUser?)null);
            var tokenPair = new TokenPair(tokenDto.AccessToken, "refreshToken");

            // Act & Assert
            await Assert.ThrowsAsync<RefreshTokenException>(() => _authService.RefreshAccessToken(tokenPair));
        }

        [Fact]
        public async Task RefreshToken_WhenValid_ShouldReturnNewTokenDto()
        {
            // Arrange
            var refreshToken = "refreshToken";
            var tokenDto = new AccessTokenDto("accessToken");
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), RefreshToken = refreshToken, RefreshTokenExpiryTime = DateTimeOffset.Now.AddDays(1) };
            var newAccessToken = "newAccessToken";

            _jwtServiceMock
                .Setup(x => x.GetPrincipalFromToken(tokenDto.AccessToken))
                .Returns(GetValidPrincipal(user.Id));
            _identityServiceMock
                .Setup(x => x.GetUserByIdAsync(user.Id))
                .ReturnsAsync(user);
            _jwtServiceMock.Setup(x => x.CreateAccessToken(It.IsAny<List<Claim>>())).Returns(newAccessToken);
            _identityServiceMock.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(Result.Success);

            // Act
            var newTokenDto = await _authService.RefreshAccessToken(new TokenPair(tokenDto.AccessToken, refreshToken));

            // Assert
            newTokenDto.AccessToken.Should().Be(newAccessToken);
            newTokenDto.RefreshToken.Should().Be("refreshToken");
            user.RefreshToken.Should().Be("refreshToken");
        }
    }
}
