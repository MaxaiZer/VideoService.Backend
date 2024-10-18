using CoreService.Application.Common.Models;
using CoreService.Application.Dto;
using Domain.Entities;

namespace CoreService.Application.Interfaces.Services
{
    public interface IVideoService
    {
        public Task AddVideo(VideoUploadParameters videoUpload);
        
        public Task<ViewableVideoMetadata?> GetVideoMetadata(string id, CancellationToken cancellationToken = default);
        
        public Task<List<ViewableVideoMetadata>> GetVideosMetadata(VideoParameters parameters, CancellationToken cancellationToken = default);
        
        public Task<GeneratedUploadUrlDto> GetUploadUrl();
    }
}
