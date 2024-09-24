using VideoProcessingService.Infrastructure.FileStorage;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using NLog;
using Shared.Messages;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Infrastructure.Broker;
using VideoProcessingService.Infrastructure.Consumers;
using VideoProcessingService.Infrastructure.Converters;
using VideoProcessingService.Infrastructure.Data;
using VideoProcessingService.Infrastructure.Logging;

namespace VideoProcessingService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureMinio(configuration)
            .ConfigureMassTransit(configuration)
            .ConfigureFileStorage()
            .ConfigureUnitOfWork()
            .ConfigureVideoConverter()
            .ConfigureLoggerService(configuration);
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
    private static IServiceCollection ConfigureFileStorage(this IServiceCollection services) =>
        services.AddScoped<IFileStorage, MinioFileStorage>();

    private static IServiceCollection ConfigureVideoConverter(this IServiceCollection services) =>
        services.AddScoped<IVideoConverter, VideoConverter>();

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
            throw new ArgumentNullException(nameof(nlogConfigPath),
                "NLog configuration path is missing in configuration.");

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), nlogConfigPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"NLog configuration file not found at path: {fullPath}");
        
        LogManager.Setup().LoadConfigurationFromFile(Path.Combine(Directory.GetCurrentDirectory(), nlogConfigPath));
        services.AddSingleton<ILoggerManager, LoggerManager>();
        return services;
    }
    
    private static IServiceCollection ConfigureUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}