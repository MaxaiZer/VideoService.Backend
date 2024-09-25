using System.Text;
using CoreService.Application.Interfaces;
using CoreService.Infrastructure.Data.Context;
using CoreService.Infrastructure.Data.Repositories;
using CoreService.Infrastructure.FileStorage;
using CoreService.Infrastructure.Identity;
using CoreService.Infrastructure.Jwt;
using CoreService.Infrastructure.Logging;
using CoreService.Infrastructure.MessageBroker;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Minio;
using NLog;
using Shared.Messages;

namespace CoreService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IFileStorage, MinioFileStorage>();
        
        services
            .ConfigureMassTransit(configuration)
            .ConfigureMinio(configuration)
            .ConfigureLoggerService(configuration)
            .ConfigurePostgres(configuration)
            .ConfigureIdentity()
            .ConfigureJwt(configuration);

        return services;
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
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitConfig = context.GetRequiredService<IOptions<MessageBrokerConfiguration>>().Value;
                cfg.Host($"rabbitmq://{rabbitConfig.Host}", h =>
                {
                    h.Username(rabbitConfig.Username);
                    h.Password(rabbitConfig.Password);
                });
              
              cfg.Message<VideoReadyForProcessing>(e =>
              {
                  e.SetEntityName(rabbitConfig.VideoProcessingExchangeName); 
              });

              cfg.Publish<VideoReadyForProcessing>(e =>
              {
                  e.ExchangeType = "fanout"; 
              });
              
              cfg.ConfigureEndpoints(context);
            });
        });

        services.AddTransient<IEventBus, EventBus>();
        return services;
    }
    
    private static IServiceCollection ConfigureMinio(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MinioConfiguration>()
            .Bind(configuration.GetSection(MinioConfiguration.Section))
            .ValidateDataAnnotations();
        
        var serviceProvider = services.BuildServiceProvider();
        var minioConfig = serviceProvider.GetRequiredService<IOptions<MinioConfiguration>>().Value;

        var minio = new MinioClient()
            .WithEndpoint(minioConfig.Endpoint)
            .WithCredentials(minioConfig.AccessKey, minioConfig.SecretKey)
            .Build();

        services.AddSingleton(minio);
        return services;
    }

    private static IServiceCollection ConfigureLoggerService(this IServiceCollection services, IConfiguration configuration)
    {
        var nlogConfigPath = configuration["Logging:NLog:ConfigPath"];
        if (string.IsNullOrEmpty(nlogConfigPath))
            throw new ArgumentNullException(nameof(nlogConfigPath), "NLog configuration path is missing in configuration.");
        
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), nlogConfigPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"NLog configuration file not found at path: {fullPath}");
        
        LogManager.Setup().LoadConfigurationFromFile(Path.Combine(Directory.GetCurrentDirectory(), nlogConfigPath));
        services.AddSingleton<ILoggerManager, LoggerManager>();
        return services;
    }

    private static IServiceCollection ConfigurePostgres(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "PostgreSQL connection string is missing.");
        
        services.AddDbContext<RepositoryContext>(opts =>
            opts.UseNpgsql(connectionString));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    private static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                /*
                 options.Password.RequireDigit = true;
                  options.Password.RequireLowercase = true;
                  options.Password.RequireUppercase = true;
                  options.Password.RequireNonAlphanumeric = false;
                  options.Password.RequiredLength = 6;
                  */
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            })
            .AddEntityFrameworkStores<RepositoryContext>();
        
        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }

    private static IServiceCollection ConfigureJwt(this IServiceCollection services, IConfiguration
        configuration)
    {
        services.AddOptions<JwtConfiguration>()
            .Bind(configuration.GetSection(JwtConfiguration.Section))
            .ValidateDataAnnotations();
        
        var serviceProvider = services.BuildServiceProvider();
        var jwtConfig = serviceProvider.GetRequiredService<IOptions<JwtConfiguration>>().Value;

        services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.ValidIssuer,
                    ValidAudience = jwtConfig.ValidAudience,
                    IssuerSigningKey = new
                        SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey))
                };
            });
        
        services.AddScoped<IJwtService, JwtService>();
        return services;
    }
}