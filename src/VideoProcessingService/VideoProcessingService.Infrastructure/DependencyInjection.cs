using VideoProcessingService.Infrastructure.FileStorage;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using NLog;
using Shared.Messages;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Infrastructure.Logging;
using VideoProcessingService.Infrastructure.Messaging.Configuration;
using VideoProcessingService.Infrastructure.Messaging.Consumers;
using VideoProcessingService.Infrastructure.Video;

namespace VideoProcessingService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureMinio(configuration)
            .ConfigureMassTransit(configuration)
            .ConfigureLoggerService(configuration);

        services.AddOptions<ConversionConfiguration>()
            .Bind(configuration.GetSection(ConversionConfiguration.Section))
            .ValidateDataAnnotations();
        
        services.AddScoped<IVideoConverter, VideoConverter>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static IServiceCollection ConfigureMassTransit(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MessageBrokerConfiguration>()
            .Bind(configuration.GetSection(MessageBrokerConfiguration.Section))
            .ValidateDataAnnotations();

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(includeNamespace: false));
            
            x.AddConsumer<VideoProcessingRequestConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitConfig = context.GetRequiredService<IOptions<MessageBrokerConfiguration>>().Value;
                cfg.Host($"rabbitmq://{rabbitConfig.Host}", h =>
                {
                    h.Username(rabbitConfig.Username);
                    h.Password(rabbitConfig.Password);
                });
                
                cfg.Message<VideoReadyForProcessing>(topology =>
                {
                   topology.SetEntityName(rabbitConfig.VideoProcessingExchangeName);
                });

                cfg.PrefetchCount = 1;
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IServiceCollection ConfigureMinio(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MinioConfiguration>()
            .Bind(configuration.GetSection(MinioConfiguration.Section))
            .ValidateDataAnnotations();

        var serviceProvider = services.BuildServiceProvider();
        var minioConfig = serviceProvider.GetRequiredService<IOptions<MinioConfiguration>>().Value;

        var minio = new MinioClient().WithEndpoint(minioConfig.Endpoint)
            .WithCredentials(minioConfig.AccessKey, minioConfig.SecretKey)
            .Build();

        services.AddSingleton(minio);
        return services;
    }

    private static IServiceCollection ConfigureLoggerService(this IServiceCollection services,
        IConfiguration configuration)
    {
        var nlogConfigPath = configuration["Logging:NLog:ConfigPath"];
        if (string.IsNullOrEmpty(nlogConfigPath))
            throw new InvalidOperationException("NLog configuration path is missing in configuration.");

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), nlogConfigPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"NLog configuration file not found at path: {fullPath}");
        
        LogManager.Setup().LoadConfigurationFromFile(Path.Combine(Directory.GetCurrentDirectory(), nlogConfigPath));
        services.AddSingleton<ILoggerManager, LoggerManager>();
        return services;
    }
}