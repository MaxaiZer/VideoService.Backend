using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using CoreService.Infrastructure.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace CoreService.UnitTests.InfrastructureTests;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var jwt = new JwtConfiguration
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessLifetime = 900,
            RefreshLifetime = 604800,
            Secret = "super_secret_test_key_for_unit_tests"
        };
      
        var optionsMock = new Mock<IOptions<JwtConfiguration>>();
        optionsMock.Setup(x => x.Value).Returns(jwt);

        // Initialize the JwtService with the mocked IConfiguration
        _jwtService = new JwtService(optionsMock.Object);
    }

    [Fact]
    public void CreateAccessToken_ShouldReturnValidToken()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testUser"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        // Act
        var token = _jwtService.CreateAccessToken(claims);

        // Assert
        token.Should().NotBeNull();

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.ReadJwtToken(token);
        
        securityToken.Issuer.Should().Be("TestIssuer");
        securityToken.Audiences.Should().ContainSingle("TestAudience");
        securityToken.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void CreateRefreshToken_ShouldReturnValidTokenAndExpiryTime()
    {
        // Act
        var refreshTokenResult = _jwtService.CreateRefreshToken();

        // Assert
        refreshTokenResult.RefreshToken.Should().NotBeNullOrEmpty();
        refreshTokenResult.ExpiryTime.Should().NotBe(DateTimeOffset.Now);
        refreshTokenResult.ExpiryTime.Should().BeAfter(DateTimeOffset.Now);
    }

    [Fact]
    public void GetPrincipalFromToken_ShouldReturnValidPrincipal_WhenTokenIsValid()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SECRET", "supersecretkeysupersecretkeysupersecretkey");
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testUser"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var token = _jwtService.CreateAccessToken(claims);

        // Act
        var principal = _jwtService.GetPrincipalFromToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal.Identity.Name.Should().Be("testUser");
    }

    [Fact]
    public void GetPrincipalFromToken_ShouldThrowSecurityTokenException_WhenTokenIsInvalid()
    {
        // Arrange
        var invalidToken = "invalidToken";

        // Act & Assert
        _jwtService.Invoking(j => j.GetPrincipalFromToken(invalidToken))
            .Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void GenerateTokenOptions_ShouldReturnJwtSecurityToken()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testUser")
        };
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("supersecretkeysupersecretkeysupersecretkey")),
            SecurityAlgorithms.HmacSha256);

        // Act
        var tokenOptions = _jwtService.GetType()
            .GetMethod("GenerateTokenOptions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(_jwtService, [signingCredentials, claims]) as JwtSecurityToken;

        // Assert
        tokenOptions.Should().NotBeNull();
        tokenOptions.Issuer.Should().Be("TestIssuer");
        tokenOptions.Audiences.Should().ContainSingle("TestAudience");
    }
}