using CoreService.Application.Interfaces;
using CoreService.Infrastructure.Data.Context;
using CoreService.Infrastructure.Identity;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CoreService.IntegrationTests.Tools
{
    public class TestingWebAppFactory :  WebApplicationFactory<Program>
    {
        private static int _migrationsApplied = 0;

        public TestingWebAppFactory()
        {
           
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
          
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile("appsettings.Integration.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            });
            
            builder.ConfigureServices(ConfigureServices);
        }

        private async void ConfigureServices(IServiceCollection services)
        {
            var publishEndpointMock = new Mock<IPublishEndpoint>();
            
            publishEndpointMock
                .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            services.AddSingleton(publishEndpointMock.Object);
            
            //ToDo: why the fuck my web application factory handles migration?
            if (Interlocked.Exchange(ref _migrationsApplied, 1) != 0) return;
            
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
 
            dbContext.Database.Migrate();
           // await dbContext.Database.MigrateAsync(); throw error

            var seeder = new DatabaseSeeder(dbContext, userManager, jwtService);
            await seeder.SeedAsync();
        }
    }
}
