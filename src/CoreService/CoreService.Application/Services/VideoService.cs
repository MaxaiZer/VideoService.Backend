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
                
                _unitOfWork.Save();
                _unitOfWork.CommitTransaction();
                
                await _eventBus.PublishAsync(new VideoReadyForProcessing { RequestId = request.Id});
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the " +
                    $"{nameof(AddVideo)} service method: {ex}");
                
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<Video?> GetVideoMetadata(string id)
        {
            var list = await _unitOfWork.Videos.FindByConditionAsync(v => v.Id == id && v.Processed,
                trackChanges: false);
            return list.FirstOrDefault();
        }
        
        public async Task<GeneratedUploadUrlDto> GeneratePresignedUploadLink()
        {
            string url;
            var fileName = Guid.NewGuid().ToString();

            try
            {
                url = await _fileStorage.GeneratePresignedPutUrl(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the " +
                    $"{nameof(GeneratePresignedUploadLink)} service method: {ex}");
                throw;
            }

            return new GeneratedUploadUrlDto(url, fileName);
        }

        public async Task<Stream?> GetMasterPlaylist(Guid videoId)
        {
            return await GetFile(StorageFileNamingHelper.GetNameForVideoMasterPlaylist(videoId.ToString()));
        }
        
        public async Task<Stream?> GetSubFile(Guid videoId, string name)
        {
            return await GetFile(StorageFileNamingHelper.GetNameForVideoSubFile(videoId.ToString(), name));
        }

        private async Task<Stream?> GetFile(string storageFileName)
        {
            try
            {
                return await _fileStorage.GetFileAsync(storageFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong in the " +
                                 $"{nameof(GetFile)} service method: {ex}");

                throw;
            }
        }

    }
}
