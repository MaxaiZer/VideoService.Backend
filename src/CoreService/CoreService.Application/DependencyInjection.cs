using CoreService.Application.Interfaces.Services;
using CoreService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}