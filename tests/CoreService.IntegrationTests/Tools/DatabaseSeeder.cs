using System.Security.Claims;
using CoreService.Application.Interfaces;
using CoreService.Infrastructure.Data.Context;
using CoreService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreService.IntegrationTests.Tools;

public class DatabaseSeeder
{
    public static List<TestUserData> existingUsersWithActiveTokens = new();
    public static TestUserData existingUserWithExpiredToken;
    public static TestUserData existingUserForTokenRevoke;

    private readonly RepositoryContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;

    public DatabaseSeeder(RepositoryContext context, UserManager<ApplicationUser> userManager, IJwtService jwtService)
    {
        _context = context;
        _userManager = userManager;
        _jwtService = jwtService;
    }
    
    public async Task SeedAsync()
    {
        if (_context.Users.Any()) return;

        existingUsersWithActiveTokens.Add(await AddUser("rook", "qwerty", withExpiredToken: false));
        existingUsersWithActiveTokens.Add(await AddUser("kook", "qwerty", withExpiredToken: false));
        existingUserWithExpiredToken = await AddUser("pook", "qwerty", withExpiredToken: true);
        existingUserForTokenRevoke = await AddUser("sook", "qwerty", withExpiredToken: false);
    }

    private async Task<TestUserData> AddUser(string name, string password, bool withExpiredToken)
    {
        var token = _jwtService.CreateRefreshToken();
        if (withExpiredToken)
        {
            token = token with { ExpiryTime = DateTimeOffset.Now.AddYears(-1) };
        }

        var userId = Guid.NewGuid().ToString();

        var testUser = new TestUserData(
            Name: name, 
            Password: password, 
            RefreshToken: token.RefreshToken,
            RefreshTokenExpiryTime: token.ExpiryTime.ToUniversalTime(), 
            AccessToken: _jwtService.CreateAccessToken([new Claim(ClaimTypes.NameIdentifier, userId)])
            );
        
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = name,
            RefreshToken = token.RefreshToken,
            RefreshTokenExpiryTime = token.ExpiryTime.ToUniversalTime()
        };
 
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errorMessage = string.Format("Failed to create user: {0}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new Exception(errorMessage);
        }

        return testUser;
    }
}