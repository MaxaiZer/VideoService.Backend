using System.Security.Claims;
using CoreService.Api.Controllers;
using CoreService.Application.Common.Models;
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
        public async Task GetUploadUrl_WhenCalled_ShouldReturnOkResultWithUrl()
        {
            var result = new GeneratedUploadUrlDto("http://example.com","fileName" );
            _mockVideoService.Setup(service => service.GetUploadUrl())
                .ReturnsAsync(result);

            var actionResult = await _controller.GetUploadUrl();

            actionResult.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(result);
        }

        [Fact]
        public async Task Upload_WhenValidDataIsProvided_ShouldSetUserIdAndReturnOkResult()
        {
            var userId = Guid.NewGuid().ToString();

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
                VideoFileId = "test.mp4"
            };

            ControllerContext originalContext = _controller.ControllerContext;
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            
            var actionResult = await _controller.Upload(videoUploadDto);

            _controller.ControllerContext = originalContext;

            actionResult.Should().BeOfType<OkResult>();
            _mockVideoService.Verify(service => service.AddVideo(It.IsAny<VideoUploadParameters>()), Times.Once);
        }

        [Fact]
        public async Task Upload_WhenUserIdIsMissing_ShouldReturnUnauthorized()
        {
            var videoUploadDto = new VideoUploadDto
            { 
                Name = "Test Video",
                Description = "Test Description",
                VideoFileId = "test.mp4"
            };
            
            var actionResult = await _controller.Upload(videoUploadDto);

            actionResult.Should().BeOfType<UnauthorizedObjectResult>();
            _mockVideoService.Verify(service => service.AddVideo(It.IsAny<VideoUploadParameters>()), Times.Never);
        }
    }
}