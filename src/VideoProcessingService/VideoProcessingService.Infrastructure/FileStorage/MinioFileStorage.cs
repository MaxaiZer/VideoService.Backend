using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using VideoProcessingService.Core.Exceptions;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.FileStorage
{
    public class MinioFileStorage: IFileStorage
    {
        private readonly IMinioClient _client;
        private readonly MinioConfiguration _config;

        public MinioFileStorage(IMinioClient client, IOptions<MinioConfiguration> minioConfiguration)
        {
            _client = client;
            _config = minioConfiguration.Value;
        }

        public async Task<Stream> GetFileAsync(string name, bool isTemporary = false)
        {
            Stream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithObject((isTemporary ? $"{_config.TmpFolder}/" : "") + name)
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

        public async Task PutFileAsync(string name, Stream stream)
        {
            var args = new PutObjectArgs()
                .WithObject(name)
                .WithBucket(_config.BucketName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);

            await _client.PutObjectAsync(args);
        }
    }
}
