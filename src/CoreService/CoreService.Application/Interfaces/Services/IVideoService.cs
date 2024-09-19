using CoreService.Application.Dto;

namespace CoreService.Application.Interfaces.Services
{
    public interface IVideoService
    {
        public Task AddVideo(VideoUploadDto videoUploadDto);
        public Task<GeneratedUploadUrlDto> GeneratePresignedUploadLink();

        public Task<Stream?> GetMasterPlaylist(Guid videoId);

        public Task<Stream?> GetSubFile(Guid videoId, string fileName);
    }
}
