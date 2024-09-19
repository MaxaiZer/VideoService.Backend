using CoreService.Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace CoreService.Infrastructure.FileStorage
{
    public class MinioFileStorage: IFileStorage
    {
        private readonly IMinioClient _client;
        private readonly MinioConfiguration _config;

        public MinioFileStorage(IMinioClient client, IOptions<MinioConfiguration> minioConfiguration)
        {
            _client = client;
            _config = minioConfiguration.Value;
            InitializeBucketAsync().GetAwaiter().GetResult(); // Initialize the bucket
        }

        private async Task InitializeBucketAsync()
        {
            bool found = false;

            try
            {
                // Check if the bucket exists
                var args = new BucketExistsArgs().WithBucket(_config.BucketName);
                var bucketExists = await _client.BucketExistsAsync(args);
                found = bucketExists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking bucket existence: {ex.Message}");
            }

            if (!found)
            {
                try
                {
                    /*
                    // Create the bucket if it does not exist
                    new MakeBucketArgs().W
                    await _client.MakeBucketAsync(_defaultBucket);

                    var config = new LifecycleConfiguration();
                    config.Rules.Add(
                        new LifecycleRule { Expiration = new Expiration { Days = 1 } }
                    );

                    await _client.SetBucketLifecycleAsync(bucketName, lifecyclePolicy);

                    Console.WriteLine($"Bucket '{_defaultBucket}' created successfully.");
                    */
                    var args = new MakeBucketArgs().WithBucket(_config.BucketName);
                    await _client.MakeBucketAsync(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating bucket: {ex.Message}");
                    throw; // Rethrow to handle the exception at a higher level if necessary
                }
            }
        }


        public async Task<Stream> GetFileAsync(string name)
        {
            Stream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithObject(name)
                .WithBucket(_config.BucketName)
                .WithCallbackStream(async (str, cancellationToken) => 
                    await str.CopyToAsync(stream, cancellationToken));

            try
            {
                await _client.GetObjectAsync(args);
                stream.Position = 0;
            }
            catch (ObjectNotFoundException ex)
            {
                throw new NotFoundException("Object name: " + name + " " + ex);
            }
            return stream;
        }

        public async Task<Stream> GetFileAsync(string name, long offset, long length)
        {
            Stream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithObject(name)
                .WithBucket(_config.BucketName)
                .WithOffsetAndLength(offset, length)
                .WithCallbackStream((str) =>
                {
                    str.CopyToAsync(stream);
                });

            try
            {
                await _client.GetObjectAsync(args);
            } catch (ObjectNotFoundException ex)
            {
                throw new NotFoundException(ex.Message);
            }
            return stream;
        }

        public async Task GetFileMetadata(string name)
        {
            var args = new StatObjectArgs()
                .WithObject(name)
                .WithBucket(_config.BucketName);

            Minio.DataModel.ObjectStat stat = await _client.StatObjectAsync(args);
        }

        public async Task PutFileAsync(string name, Stream stream)
        {
            var args = new PutObjectArgs()
                .WithObject(name)
                .WithBucket(_config.BucketName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);

            await _client.PutObjectAsync(args);
        }

        public async Task<string> GeneratePresignedPutUrl(string fileName)
        {
            var args = new PresignedPutObjectArgs()
                .WithObject(fileName)
                .WithBucket(_config.BucketName)
                .WithExpiry(60 * 60 * 12);

            var url = await _client.PresignedPutObjectAsync(args);
            return url.Replace(_config.Endpoint, _config.PublicHost);

          //return url;
        }
    }
}
