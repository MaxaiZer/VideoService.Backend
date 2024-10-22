using System.Security.Claims;
using CoreService.Application.Common.Helpers;
using CoreService.Application.Interfaces;
using CoreService.Infrastructure.Data.Context;
using CoreService.Infrastructure.Identity;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CoreService.IntegrationTests.Tools;

public class DatabaseSeeder
{
    public static List<TestUserData> usersWithActiveTokens = new();
    public static TestUserData userWithExpiredToken;

    public static Video processedVideo;
    public static Video notProcessedVideo;

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

        const int usersActiveTokensCount = 7;
        for (int i = 0; i < usersActiveTokensCount; i++)
        {
            usersWithActiveTokens.Add(await AddUser(Guid.NewGuid().ToString()
                .Substring(0, 8), "qwerty", withExpiredToken: false));
        }

        userWithExpiredToken = await AddUser("pook", "qwerty", withExpiredToken: true);
        
        if (_context.Videos.Any()) return;

        processedVideo = new Video("1", "Temp", usersWithActiveTokens[0].Id, "description", true);
        notProcessedVideo = new Video("2", "Temp", usersWithActiveTokens[0].Id, "description", false);
        
        _context.Videos.Add(processedVideo);
        _context.Videos.Add(notProcessedVideo);
        await _context.SaveChangesAsync();
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
            Id: userId,
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
            RefreshToken = TokenHelper.HashToken(token.RefreshToken),
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