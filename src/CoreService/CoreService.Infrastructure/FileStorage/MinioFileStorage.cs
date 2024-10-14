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
        }

        public async Task<string> GeneratePutUrlForTempFile(string fileName)
        {
            var args = new PresignedPutObjectArgs()
                .WithObject($"{_config.TmpFolder}/" + fileName)
                .WithBucket(_config.BucketName)
                .WithExpiry(60 * 60 * 12);

            var url = await _client.PresignedPutObjectAsync(args);
            return url.Replace(_config.Endpoint, _config.PublicUrl);
        }
    }
}
