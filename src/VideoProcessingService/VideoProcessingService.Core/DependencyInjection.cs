using Microsoft.Extensions.DependencyInjection;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IVideoProcessingService, Services.VideoProcessingService>();
        return services;
    }
}