using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
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
        private readonly ILogger _logger;
        private string _postgresConnectionString;

        public EnvironmentFixture()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Integration.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddNLog();
            });
            _logger = loggerFactory.CreateLogger<EnvironmentFixture>();

            CreatePostgresContainer(configuration);
            CreateMinioContainer(configuration);
        }

        private void CreatePostgresContainer(IConfiguration configuration)
        {
            var connectionString = configuration["DB_CONNECTION_STRING"];
            _postgresConnectionString = connectionString ?? throw new Exception("Connection string is null");
            
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
                .WithLogger(_logger)
                .Build()
            );
        }

        private void CreateMinioContainer(IConfiguration configuration)
        {
            var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            var scriptSourcePath = Path.Combine(solutionDir, "minio-setup.sh");

            if (!File.Exists(scriptSourcePath))
                throw new Exception("Minio setup script doesn't exist");
            
            var accessKey = configuration.GetSection("MinIO")["AccessKey"] ?? throw new Exception("AccessKey is null");
            var secretKey = configuration.GetSection("MinIO")["SecretKey"] ?? throw new Exception("SecretKey is null");
            var bucketName = configuration.GetSection("MinIO")["BucketName"] ?? throw new Exception("BucketName is null");
            var tmpFolder = configuration.GetSection("MinIO")["TmpFolder"] ?? throw new Exception("TmpFolder is null");
         
            _containers.Add(new ContainerBuilder()
                .WithImage("minio/minio:latest")
                .WithEntrypoint("/bin/sh", "/usr/local/bin/minio-setup.sh")
                .WithCommand("server", "--console-address", ":9001", "/data")
                .WithEnvironment("MINIO_ROOT_USER", accessKey)
                .WithEnvironment("MINIO_ROOT_PASSWORD", secretKey)
                .WithEnvironment("BUCKET_NAME", bucketName)
                .WithEnvironment("TMP_FOLDER", tmpFolder)
                .WithEnvironment("PUBLIC_FOLDER", "videos")
                .WithPortBinding(9000)
                .WithPortBinding(9001)
                .WithResourceMapping(scriptSourcePath, "/usr/local/bin")
                .WithCleanUp(true)
                .WithLogger(_logger)
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
                var (stdout, stderr) = await container.GetLogsAsync();

                _logger.LogInformation(stdout);
                _logger.LogError(stderr);
                
                await container.StopAsync();
                await container.DisposeAsync();
            }
        }
    }
}