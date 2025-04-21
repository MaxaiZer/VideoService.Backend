using CoreService.Application.Common.Models;
using CoreService.Application.Interfaces;
using CoreService.Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace CoreService.UnitTests.ApplicationTests
{
    public class VideoServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IFileStorage> _mockFileStorage;
        private readonly Mock<ILoggerManager> _mockLogger;
        private readonly VideoService _videoService;

        public VideoServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockFileStorage = new Mock<IFileStorage>();
            _mockLogger = new Mock<ILoggerManager>();
            Mock<IEventBus> mockBus = new();   
            
            _videoService = new VideoService(
                _mockUnitOfWork.Object,
                _mockFileStorage.Object,
                _mockLogger.Object,
                mockBus.Object
            );
        }

        [Fact]
        public async Task AddVideo_WhenValidDataIsProvided_ShouldCallDependenciesAndAddVideo()
        {
            var videoUploadDto = new VideoUploadParameters(Name: "Test Video",
                UserId: Guid.NewGuid().ToString(),
                Description: "Test Description",
                VideoFileId: "test.mp4");

            _mockUnitOfWork.Setup(uow => uow.Videos.Create(It.IsAny<Video>()));
            _mockUnitOfWork.Setup(uow => uow.VideoProcessingRequests.Create(It.IsAny<VideoProcessingRequest>()));

            await _videoService.AddVideo(videoUploadDto);

            _mockUnitOfWork.Verify(uow => uow.Videos.Create(It.IsAny<Video>()), Times.Once);
        }

        [Fact]
        public async Task GetUploadUrl_WhenCalled_ShouldReturnUrlAndFileName()
        {
            var expectedUrl = "http://example.com";
            _mockFileStorage.Setup(fs => fs.GeneratePutUrlForTempFile(It.IsAny<string>())).ReturnsAsync(expectedUrl);

            var result = await _videoService.GetUploadUrl();

            result.Url.Should().Be(expectedUrl);
            result.FileId.Should().NotBeNullOrEmpty();
        }
    }
}
