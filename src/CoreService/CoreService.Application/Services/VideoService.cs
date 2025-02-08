using CoreService.Application.Common.Models;
using Domain.Entities;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Services;
using CoreService.Application.Common.Extensions;
using Shared.Messages;

namespace CoreService.Application.Services
{

    public class VideoService : IVideoService
    {
        private readonly IEventBus _eventBus;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;
        private readonly ILoggerManager _logger;

        public VideoService(IUnitOfWork unitOfWork, IFileStorage storage, ILoggerManager logger, IEventBus eventBus)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = storage;
            _logger = logger;
            _eventBus = eventBus;
        }

        public async Task AddVideo(VideoUploadParameters videoUpload)
        {
            try
            {
                var request = new VideoProcessingRequest(Guid.NewGuid().ToString(), videoUpload.VideoFileId, 
                    VideoProcessingRequest.ProcessingStatus.Appending);
                var video = new Video(videoUpload.VideoFileId, videoUpload.Name, videoUpload.UserId, videoUpload.Description);
                
                _unitOfWork.BeginTransaction();
                
                _unitOfWork.VideoProcessingRequests.Create(request);
                _unitOfWork.Videos.Create(video);
                
                await _eventBus.PublishAsync(new VideoReadyForProcessing { RequestId = request.Id});
                
                _unitOfWork.Save();
                _unitOfWork.CommitTransaction();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the " +
                    $"{nameof(AddVideo)} service method: {ex}");
                
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<ViewableVideoMetadata?> GetVideoMetadata(string id, CancellationToken cancellationToken = default)
        {
            _logger.LogError($"Get video with id: {id}");
            return await _unitOfWork.Videos.FindViewableByIdAsync(id, cancellationToken);
        }

        public async Task<List<ViewableVideoMetadata>> GetVideosMetadata(VideoParameters parameters,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Videos.FindViewableAsync(parameters.ToDomain(), cancellationToken);
        }
        
        public async Task<GeneratedUploadUrlDto> GetUploadUrl()
        {
            string url;
            var fileName = Guid.NewGuid().ToString();

            try
            {
                url = await _fileStorage.GeneratePutUrlForTempFile(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the " +
                    $"{nameof(GetUploadUrl)} service method: {ex}");
                throw;
            }

            return new GeneratedUploadUrlDto(url, fileName);
        }
    }
}
