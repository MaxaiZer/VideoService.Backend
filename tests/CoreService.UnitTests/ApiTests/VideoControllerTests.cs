using System.Security.Claims;
using CoreService.Api.Controllers;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CoreService.UnitTests.ApiTests
{
    public class VideoControllerTests
    {
        private readonly Mock<IVideoService> _mockVideoService;
        private readonly VideoController _controller;

        public VideoControllerTests()
        {
            _mockVideoService = new Mock<IVideoService>();
            _controller = new VideoController(_mockVideoService.Object);
        }

        [Fact]
        public async Task GetPresignedUploadUrl_WhenCalled_ShouldReturnOkResultWithUrl()
        {
            var result = new GeneratedUploadUrlDto("http://example.com","fileName" );
            _mockVideoService.Setup(service => service.GeneratePresignedUploadLink())
                .ReturnsAsync(result);

            var actionResult = await _controller.GetPresignedUploadUrl();

            actionResult.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(result);
        }

        [Fact]
        public async Task Upload_WhenValidDataIsProvided_ShouldSetUserIdAndReturnOkResult()
        {
            var userId = Guid.NewGuid().ToString(); // Mocked UserId

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var videoUploadDto = new VideoUploadDto
            {
                Name = "Test Video",
                Description = "Test Description",
                UploadedVideoId = "test.mp4"
            };

            ControllerContext originalContext = _controller.ControllerContext;
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            
            var actionResult = await _controller.Upload(videoUploadDto);

            _controller.ControllerContext = originalContext;

            actionResult.Should().BeOfType<OkResult>();
            _mockVideoService.Verify(service => service.AddVideo(It.IsAny<VideoUploadDto>()), Times.Once);
            videoUploadDto.UserId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Upload_WhenUserIdIsMissing_ShouldReturnUnauthorized()
        {
            var videoUploadDto = new VideoUploadDto
            { 
                Name = "Test Video",
                Description = "Test Description",
                UploadedVideoId = "test.mp4"
            };
            
            var actionResult = await _controller.Upload(videoUploadDto);

            actionResult.Should().BeOfType<UnauthorizedObjectResult>();
            _mockVideoService.Verify(service => service.AddVideo(It.IsAny<VideoUploadDto>()), Times.Never);
        }
        
        [Fact]
        public async Task GetMasterPlaylist_WhenPlaylistExists_ShouldReturnFileResultWithCorrectContentType()
        {
            var videoId = Guid.NewGuid();
            var playlistStream = new MemoryStream();
            _mockVideoService.Setup(service => service.GetMasterPlaylist(videoId))
                .ReturnsAsync(playlistStream);

            var actionResult = await _controller.GetMasterPlaylist(videoId);
            
            actionResult.Should().BeOfType<FileStreamResult>()
                .Which.ContentType.Should().Be("application/vnd.apple.mpegurl");
        }
        
        [Fact]
        public async Task GetSubFile_WhenPlaylistExists_ShouldReturnFileResultWithCorrectContentType()
        {
            var videoId = Guid.NewGuid();
            var fileName = "playlist.m3u8";
            var playlistStream = new MemoryStream();
            _mockVideoService.Setup(service => service.GetSubFile(videoId, fileName))
                .ReturnsAsync(playlistStream);

            var actionResult = await _controller.GetSubFile(videoId, fileName);
            
            actionResult.Should().BeOfType<FileStreamResult>()
                .Which.ContentType.Should().Be("application/vnd.apple.mpegurl");
        }

        [Fact]
        public async Task GetSubFile_WhenSegmentExists_ShouldReturnFileResultWithCorrectContentType()
        {
            var videoId = Guid.NewGuid();
            var segmentName = "segment.ts";
            var segmentStream = new MemoryStream();
            _mockVideoService.Setup(service => service.GetSubFile(videoId, segmentName))
                .ReturnsAsync(segmentStream);

            var actionResult = await _controller.GetSubFile(videoId, segmentName);

            actionResult.Should().BeOfType<FileStreamResult>()
                .Which.ContentType.Should().Be("video/MP2T");
        }
    }
}