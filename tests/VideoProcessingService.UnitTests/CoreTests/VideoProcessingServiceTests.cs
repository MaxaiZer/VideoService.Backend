using VideoProcessingService.Core.Interfaces;
using FluentAssertions;
using Moq;
using Shared.Helpers;
using Shared.Messages;
using VideoProcessingService.Core.Models;
using VideoProcessingService.UnitTests.Tools;

namespace VideoProcessingService.UnitTests.CoreTests
{
    public class VideoProcessingServiceTests
    {
        private readonly Mock<IFileStorage> _mockFileStorage;
        private readonly Mock<IVideoConverter> _mockVideoConverter;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerManager> _mockLogger;
        private readonly Core.Services.VideoProcessingService _videoProcessingService;

        public VideoProcessingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockFileStorage = new Mock<IFileStorage>();
            _mockVideoConverter = new Mock<IVideoConverter>();
            _mockLogger = new Mock<ILoggerManager>();
            _videoProcessingService = new Core.Services.VideoProcessingService(
                _mockFileStorage.Object,
                _mockUnitOfWork.Object,
                _mockVideoConverter.Object,
                _mockLogger.Object
            );
        }

        private void SetupSuccessfulVideoProcessingTest(out VideoReadyForProcessing message, out VideoProcessingRequest request, out ConversionResult conversionResult)
        {
            var directory = Path.GetTempPath();
            var tempMaster = new TempFileFixture(Path.Combine(directory, "master.m3u8"));
            var tempIndex = new TempFileFixture(Path.Combine(directory, "index.m3u8"));
            var tempSegment1 = new TempFileFixture(Path.Combine(directory, "segment1.ts"));
            var tempSegment2 = new TempFileFixture(Path.Combine(directory, "segment2.ts"));
            var thumbnail = new TempFileFixture(Path.Combine(directory, "thumbnail.jpg"));

            conversionResult = new ConversionResult(
                IndexFilePath: tempMaster.FilePath,
                SubFilesPaths: [tempIndex.FilePath, tempSegment1.FilePath, tempSegment2.FilePath],
                ThumbnailPath: thumbnail.FilePath
                );

            var videoId =  Guid.NewGuid().ToString();
            var requestId = Guid.NewGuid().ToString();
            request =
                new VideoProcessingRequest(requestId, videoId, VideoProcessingRequest.ProcessingStatus.Appending);
            message = new VideoReadyForProcessing { RequestId = requestId };
            
            _mockVideoConverter.Setup(vc => vc.ConvertAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(conversionResult);
            _mockFileStorage.Setup(fs => fs.GetFileAsync(videoId, true)).ReturnsAsync(new MemoryStream());
            _mockFileStorage.Setup(fs => fs.PutFileAsync(It.IsAny<string>(), It.IsAny<Stream>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.VideoProcessingRequests.GetById(requestId)).ReturnsAsync(request);
            _mockUnitOfWork.Setup(x => x.VideoProcessingRequests.UpdateStatus(requestId, 
                It.IsAny<VideoProcessingRequest.ProcessingStatus>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Videos.MarkVideoAsProcessed(videoId)).ReturnsAsync(true);
        }

        [Fact]
        public async Task ProcessAndStoreVideoAsync_WhenCalled_ShouldStorePlaylistFileOnce()
        {
            SetupSuccessfulVideoProcessingTest(out var message, out var request, out var _);
            
            await _videoProcessingService.ProcessAndStoreVideoAsync(message);

            string targetPlaylistName = StorageFileNamingHelper.GetNameForVideoMasterPlaylist(request.VideoId);
            _mockFileStorage.Verify(fs => fs.PutFileAsync(targetPlaylistName, It.IsAny<Stream>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAndStoreVideoAsync_WhenCalled_ShouldStoreEachSegmentFile()
        {
            SetupSuccessfulVideoProcessingTest(out var message, out var request, out var conversionResult);
            
            await _videoProcessingService.ProcessAndStoreVideoAsync(message);

            foreach (var subfile in conversionResult.SubFilesPaths)
            {
                string targetFileName = StorageFileNamingHelper.GetNameForVideoSubFile(request.VideoId,
                    Path.GetFileName(subfile));
                
                _mockFileStorage.Verify(fs => fs.PutFileAsync(targetFileName, It.IsAny<Stream>()), Times.Once);
            }
        }

        [Fact]
        public async Task ProcessAndStoreVideoAsync_WhenCalled_ShouldRemoveTempDirectory()
        {
            SetupSuccessfulVideoProcessingTest(out var message, out var request, out _);

            await _videoProcessingService.ProcessAndStoreVideoAsync(message);

            Directory.Exists(Path.Combine(Path.GetTempPath(), request.VideoId)).Should().BeFalse();
        }

        [Fact]
        public async Task ProcessAndStoreVideoAsync_WhenExceptionOccurs_ShouldRemoveTempDirectory()
        {
            SetupSuccessfulVideoProcessingTest(out var message, out var request, out _);
            var exceptionMessage = "File not found";
            _mockFileStorage.Setup(fs => fs.GetFileAsync(request.VideoId, true)).ThrowsAsync(new Exception(exceptionMessage));

            await _videoProcessingService.Invoking(v => v.ProcessAndStoreVideoAsync(message))
                .Should().ThrowAsync<Exception>();
            Directory.Exists(Path.Combine(Path.GetTempPath(), request.VideoId)).Should().BeFalse();
        }

        [Fact]
        public async Task ProcessAndStoreVideoAsync_WhenExceptionOccurs_ShouldLogErrorAndThrowException()
        {
            SetupSuccessfulVideoProcessingTest(out var message, out var request, out _);
            var exceptionMessage = "File not found";
            _mockFileStorage.Setup(fs => fs.GetFileAsync(request.VideoId, true)).ThrowsAsync(new Exception(exceptionMessage));

            var exception = await Assert.ThrowsAsync<Exception>(() => _videoProcessingService.ProcessAndStoreVideoAsync(message));
            
            exception.Message.Should().Be(exceptionMessage);
            _mockLogger.Verify(logger => logger.LogError(It.IsAny<string>()), Times.Once);
        }
    }
}
