using CoreService.Application.Common.Models;
using CoreService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CoreService.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;

    public IdentityService(UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory)
    {
        _userManager = userManager;
        _authorizationService = authorizationService;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
    }
    
    public async Task<(Result Result, string UserId)> CreateUserAsync(string name, string password)
    {
        var appUser = new ApplicationUser { UserName = name };
        var result = await _userManager.CreateAsync(appUser, password);
        return (result.ToApplicationResult(), appUser.Id);
    }

    public async Task<Result> UpdateUserAsync(IApplicationUser user)
    {
        var res = await _userManager.UpdateAsync((ApplicationUser)user);
        return res.ToApplicationResult();
    }
    
    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);
        return result.Succeeded;
    }

    public async Task<IApplicationUser?> GetUserByNameAsync(string name)
    {
        return await _userManager.FindByNameAsync(name);
    }
    
    public async Task<IApplicationUser?> GetUserByIdAsync(string id)
    {
        return await _userManager.FindByIdAsync(id);
    }
    
    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<bool> CheckPasswordAsync(IApplicationUser user, string password)
    {
        var appUser = (ApplicationUser)user;
        return await _userManager.CheckPasswordAsync(appUser, password);
    }

    private async Task<Result> DeleteUserAsync(IApplicationUser user)
    {
        var result = await _userManager.DeleteAsync((ApplicationUser)user);

        return result.ToApplicationResult();
    }
    
    /*private async Task<List<Claim>> GetUserClaims() ToDo: clean up
{
    var claims = new List<Claim>
     {
        new(ClaimTypes.Name, _user.UserName)
     };

    var roles = await _userManager.GetRolesAsync(_user.Id);
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
    return claims;
}*/
}