using CoreService.Application.Dto;
using CoreService.Application.Interfaces;
using CoreService.Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.Helpers;

namespace CoreService.UnitTests.ApplicationTests
{
    public class VideoServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IFileStorage> _mockFileStorage;
        private readonly Mock<ILoggerManager> _mockLogger;
        private readonly Mock<IEventBus> _mockBus;
        private readonly VideoService _videoService;

        public VideoServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockFileStorage = new Mock<IFileStorage>();
            _mockLogger = new Mock<ILoggerManager>();
            _mockBus = new Mock<IEventBus>();   
            
            _videoService = new VideoService(
                _mockUnitOfWork.Object,
                _mockFileStorage.Object,
                _mockLogger.Object,
                _mockBus.Object
            );
        }

        [Fact]
        public async Task AddVideo_WhenValidDataIsProvided_ShouldCallDependenciesAndAddVideo()
        {
            var videoUploadDto = new VideoUploadDto
            {
                Name = "Test Video",
                UserId = Guid.NewGuid().ToString(),
                Description = "Test Description",
                UploadedVideoId = "test.mp4"
            };

            _mockUnitOfWork.Setup(uow => uow.Videos.Create(It.IsAny<Video>()));
            _mockUnitOfWork.Setup(uow => uow.VideoProcessingRequests.Create(It.IsAny<VideoProcessingRequest>()));
            _mockUnitOfWork.Setup(uow => uow.Save());

            await _videoService.AddVideo(videoUploadDto);

            _mockUnitOfWork.Verify(uow => uow.Videos.Create(It.IsAny<Video>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Save(), Times.Once);
        }

        [Fact]
        public async Task AddVideo_WhenUserIdIsMissing_ShouldThrowAndLogException()
        {
            var videoUploadDto = new VideoUploadDto
            {
                Name = "Test Video",
                Description = "Test Description",
                UploadedVideoId = "test.mp4"
            };

            await Assert.ThrowsAsync<Exception>(() => _videoService.AddVideo(videoUploadDto));

            _mockUnitOfWork.Verify(uow => uow.Videos.Create(It.IsAny<Video>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.Save(), Times.Never);
            _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GeneratePresignedUploadLink_WhenCalled_ShouldReturnUrlAndFileName()
        {
            var expectedUrl = "http://example.com";
            _mockFileStorage.Setup(fs => fs.GeneratePresignedPutUrl(It.IsAny<string>())).ReturnsAsync(expectedUrl);

            var result = await _videoService.GeneratePresignedUploadLink();

            result.Url.Should().Be(expectedUrl);
            result.FileName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetSubFile_WhenFileExists_ShouldReturnStream()
        {
            var videoId = Guid.NewGuid();
            var fileName = "playlist.m3u8";
            var expectedName = StorageFileNamingHelper.GetNameForVideoSubFile(videoId.ToString(), fileName);
            var expectedStream = new MemoryStream();
            _mockFileStorage.Setup(fs => fs.GetFileAsync(expectedName)).ReturnsAsync(expectedStream);

            var result = await _videoService.GetSubFile(videoId, fileName);

            result.Should().BeSameAs(expectedStream);
        }

        [Fact]
        public async Task GetSubFile_WhenExceptionOccurs_ShouldLogErrorAndThrow()
        {
            var videoId = Guid.NewGuid();
            var fileName = "playlist.m3u8";
            var expectedName = StorageFileNamingHelper.GetNameForVideoSubFile(videoId.ToString(), fileName);
            
            var exceptionMessage = "File not found";
            _mockFileStorage.Setup(fs => fs.GetFileAsync(expectedName)).ThrowsAsync(new Exception(exceptionMessage));

            var exception = await Assert.ThrowsAsync<Exception>(() => _videoService.GetSubFile(videoId, fileName));

            exception.Message.Should().Be(exceptionMessage);
            _mockLogger.Verify(logger => logger.LogError(It.IsAny<string>()), Times.Once);
        }
    }
}
