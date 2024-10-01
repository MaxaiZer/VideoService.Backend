using Shared.Helpers;
using Shared.Messages;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Core.Models;

namespace VideoProcessingService.Core.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly IFileStorage _fileStorage;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVideoConverter _videoConverter;
        private readonly ILoggerManager _logger;

        public VideoProcessingService(IFileStorage fileStorage, IUnitOfWork unitOfWork,
            IVideoConverter videoConverter, ILoggerManager logger)
        {
            _fileStorage = fileStorage;
            _unitOfWork = unitOfWork;
            _videoConverter = videoConverter;
            _logger = logger;
        }

        public async Task ProcessAndStoreVideoAsync(VideoReadyForProcessing message)
        {
            var request = await _unitOfWork.VideoProcessingRequests.GetById(message.RequestId);
            if (request == null)
                throw new Exception($"Can't find request with id: {message.RequestId}");
            
            if (request.Status is VideoProcessingRequest.ProcessingStatus.Processing or 
                VideoProcessingRequest.ProcessingStatus.Finished)
                throw new Exception($"Video is already processing or processed");

            var videoId = request.VideoId;
            await _unitOfWork.VideoProcessingRequests.UpdateStatus(request.Id, VideoProcessingRequest.ProcessingStatus.Processing);
            
            var tmpDirectory = Path.Combine(Path.GetTempPath(), videoId);
            Directory.CreateDirectory(tmpDirectory);

            var inputFilePath = Path.Combine(tmpDirectory, videoId);

            try
            {
                await using (var inputStream = await _fileStorage.GetFileAsync(videoId, isTemporary: true))
                {
                    await using var fileStream = new FileStream(inputFilePath, FileMode.Create, FileAccess.Write);
                    inputStream.Position = 0;

                    await inputStream.CopyToAsync(fileStream);
                }
                
                var res = await _videoConverter.ConvertToHlsAsync(inputFilePath, tmpDirectory);
                await StoreResultFilesAsync(videoId, res);
                
                _unitOfWork.BeginTransaction();
                
                bool requestUpdated = await _unitOfWork.VideoProcessingRequests.UpdateStatus(request.Id, VideoProcessingRequest.ProcessingStatus.Finished);
                if (!requestUpdated)
                {
                    throw new Exception("Failed to update video processing request status.");
                }

                bool videoUpdated = await _unitOfWork.Videos.MarkVideoAsProcessed(videoId);
                if (!videoUpdated)
                {
                    throw new Exception("Failed to mark video as processed.");
                }
                
                _unitOfWork.CommitTransaction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _unitOfWork.RollbackTransaction();
                throw;
            }
            finally
            {
                if (Directory.Exists(tmpDirectory))
                    Directory.Delete(tmpDirectory, recursive: true);
            }
        }

        private async Task StoreResultFilesAsync(string videoId, HlsConversionResult res)
        {
            var tasks = new List<Task>
            {
                StoreFileAsync(videoId, res.MasterPlaylistPath, isMasterPlaylist: true)
            };

            tasks.AddRange(res.PlaylistsFilePaths.Select(file => StoreFileAsync(videoId, file, isMasterPlaylist: false)));
            tasks.AddRange(res.SegmentsFilePaths.Select(file => StoreFileAsync(videoId, file, isMasterPlaylist: false)));
            await Task.WhenAll(tasks);
        }

        private async Task StoreFileAsync(string videoId, string filePath, bool isMasterPlaylist)
        {
            string fileName = Path.GetFileName(filePath);
            string storageFileName = isMasterPlaylist
                ? StorageFileNamingHelper.GetNameForVideoMasterPlaylist(videoId)
                : StorageFileNamingHelper.GetNameForVideoSubFile(videoId, fileName);

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await _fileStorage.PutFileAsync(storageFileName, fileStream);
        }
    }
}
