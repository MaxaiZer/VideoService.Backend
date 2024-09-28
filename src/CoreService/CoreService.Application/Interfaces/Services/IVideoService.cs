using CoreService.Application.Dto;
using Domain.Entities;

namespace CoreService.Application.Interfaces.Services
{
    public interface IVideoService
    {
        public Task AddVideo(VideoUploadDto videoUploadDto);
        
        public Task<Video?> GetVideoMetadata(string id, CancellationToken cancellationToken = default);
        
        public Task<GeneratedUploadUrlDto> GetUploadUrl();

        public Task<Stream?> GetMasterPlaylist(Guid videoId);

        public Task<Stream?> GetSubFile(Guid videoId, string fileName);
    }
}
