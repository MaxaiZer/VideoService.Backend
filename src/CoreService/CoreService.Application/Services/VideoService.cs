using Domain.Entities;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Services;
using Shared.Helpers;
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

        public async Task AddVideo(VideoUploadDto videoUploadDto)
        {
            if (string.IsNullOrEmpty(videoUploadDto.UserId))
            {
                var message = $"{nameof(AddVideo)}: UserId was not populated is videoUploadDto";
                _logger.LogError(message);
                throw new Exception(message);
            }

            try
            {
                var request = new VideoProcessingRequest(Guid.NewGuid().ToString(), videoUploadDto.UploadedVideoId, 
                    VideoProcessingRequest.ProcessingStatus.Appending);
                var video = new Video(videoUploadDto.UploadedVideoId, videoUploadDto.Name, videoUploadDto.UserId, videoUploadDto.Description);
                
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
            return await _unitOfWork.Videos.FindViewableAsync(parameters.SearchQuery, parameters.PageNumber,
                parameters.PageSize, cancellationToken);
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
