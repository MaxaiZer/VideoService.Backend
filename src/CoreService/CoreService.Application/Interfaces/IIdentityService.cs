using CoreService.Application.Common.Models;

namespace CoreService.Application.Interfaces;

public interface IIdentityService
{
    Task<(Result Result, string UserId)> CreateUserAsync(string name, string password);

    public Task<Result> UpdateUserAsync(IApplicationUser user);

    public Task<IApplicationUser?> GetUserByIdAsync(string id);
    
    public Task<IApplicationUser?> GetUserByNameAsync(string name);
    
    public Task<IApplicationUser?> GetUserByRefreshTokenAsync(string refreshToken);
    
    Task<bool> CheckPasswordAsync(IApplicationUser user, string password);
    
    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<Result> DeleteUserAsync(string userId);
}