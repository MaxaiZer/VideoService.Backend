using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoreService.Application.Common.Models;
using CoreService.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoreService.Infrastructure.Jwt;

public class JwtService : IJwtService
{
    private readonly JwtConfiguration _config;
    
    private const string _securityAlgorithm = SecurityAlgorithms.HmacSha256;

    public JwtService(IOptions<JwtConfiguration> jwtConfiguration)
    {
        _config = jwtConfiguration.Value;
    }

    public string CreateAccessToken(List<Claim> claims)
    {
        var signingCredentials = GetSigningCredentials();
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        return accessToken;
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        
        var expiryTime = DateTimeOffset.Now.AddDays(Convert.ToDouble(_config.RefreshTokenExpirationDays));
        return new RefreshTokenResult(token, expiryTime);
    }
    
    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var f = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config.Secret));
        
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = f, //new SymmetricSecurityKey(
              //  Encoding.UTF8.GetBytes(_secretKey)),
            ValidateLifetime = true,
            ValidIssuer = _config.ValidIssuer,
            ValidAudience = _config.ValidAudience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;

        ClaimsPrincipal principal;
        try
        {
            principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException(ex.Message);
        }

        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(_securityAlgorithm,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
    
    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_config.Secret);
        var secret = new SymmetricSecurityKey(key);
        return new SigningCredentials(secret, _securityAlgorithm);
    }
    
    private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
    {
        var tokenOptions = new JwtSecurityToken(
            issuer: _config.ValidIssuer,
            audience: _config.ValidAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config.AccessTokenExpirationMinutes)),
            signingCredentials: signingCredentials);

        return tokenOptions;
    }

}