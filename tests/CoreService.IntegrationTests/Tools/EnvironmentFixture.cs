using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Polly;

namespace CoreService.IntegrationTests.Tools
{
    [CollectionDefinition("Environment collection")]
    public class EnvironmentCollection : ICollectionFixture<EnvironmentFixture>
    {
    }

    public class EnvironmentFixture : IAsyncLifetime
    {
        private readonly List<IContainer> _containers = [];
        private readonly string _postgresConnectionString;

        public EnvironmentFixture()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Integration.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new Exception("Connection string is null");
            _postgresConnectionString = connectionString;
            var connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            var dbName = connectionStringBuilder["Database"].ToString();
            var port = int.Parse(connectionStringBuilder["Port"].ToString());
            var user = connectionStringBuilder["User Id"].ToString();
            var password = connectionStringBuilder["Password"].ToString();

            _containers.Add(new ContainerBuilder()
                .WithImage("postgres:latest")
                .WithEnvironment("POSTGRES_DB", dbName)
                .WithEnvironment("POSTGRES_USER", user)
                .WithEnvironment("POSTGRES_PASSWORD", password)
                .WithPortBinding(port)
                .WithCleanUp(true)
                .Build()
            );

            _containers.Add(new ContainerBuilder()
                .WithImage("minio/minio:latest")
                .WithCommand("server", "/data")
                .WithEnvironment("MINIO_ROOT_USER", configuration.GetSection("MinIO")["AccessKey"])
                .WithEnvironment("MINIO_ROOT_PASSWORD", configuration.GetSection("MinIO")["SecretKey"])
                .WithPortBinding(9000)
                .WithPortBinding(9001)
                .WithCleanUp(true)
                .Build()
            );
        }

        private async Task EnsurePostgresIsReady()
        {
            await using var connection = new NpgsqlConnection(_postgresConnectionString);
            
            var retryPolicy = Policy
                .Handle<Exception>() 
                .WaitAndRetryAsync(
                    retryCount: 10, 
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(2),
                    onRetry: (exception, timeSpan, retryCount, _) =>
                    {
                        Console.WriteLine(
                            $"Waiting for PostgreSQL to be ready. Retry {retryCount} after {timeSpan}. Exception: {exception.Message}");
                    });
            
            await retryPolicy.ExecuteAsync(async () =>
            {
                await connection.OpenAsync();
            });
        }

        public async Task InitializeAsync()
        {
            foreach (var container in _containers)
                await container.StartAsync();

            await EnsurePostgresIsReady();
        }

        public async Task DisposeAsync()
        {
            foreach (var container in _containers)
            {
                await container.StopAsync();
                await container.DisposeAsync();
            }
        }
    }
}