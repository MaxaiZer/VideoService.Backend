using System.Security.Claims;
using CoreService.Application.Common.Models;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.Controllers
{
    [ApiController]
    [Route("api/videos")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService _videoService;

        public VideoController(IVideoService service)
        {
            _videoService = service;
        }
     
        /// <summary>
        /// Generates a presigned URL for uploading a video file.
        /// </summary>
        /// <returns>HTTP 200 status code with the presigned upload URL if successful; otherwise, returns a 401 status code if authentication fails.</returns>
        /// <response code="200">Presigned upload URL generated successfully.</response>
        /// <response code="401">Authentication failed.</response>
        [HttpGet("upload-url")]
        [Authorize]
        public async Task<IActionResult> GetUploadUrl()
        {
            var res = await _videoService.GetUploadUrl();
            return Ok(res);
        }

        /// <summary>
        /// Uploads a new video with the provided metadata.
        /// </summary>
        /// <param name="videoUploadDto">Details of the video to be uploaded.</param>
        /// <returns>HTTP 200 status code if the video is uploaded successfully.</returns>
        /// <response code="200">Video uploaded successfully.</response>
        /// <response code="401">Authentication failed.</response>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Upload([FromBody]VideoUploadDto videoUploadDto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Missing userId claim in the token.");
            }

            var parameters = new VideoUploadParameters(Name: videoUploadDto.Name, UserId: userId,
                Description: videoUploadDto.Description, VideoFileId: videoUploadDto.VideoFileId);
            
            await _videoService.AddVideo(parameters);
            return Ok();
        }

        /// <summary>
        /// Retrieves a videos metadata.
        /// </summary>
        /// <param name="parameters">Search parameters</param>
        /// <returns>HTTP 200 status code with video metadata if the video exists.</returns>
        /// <response code="200">Videos metadata retrieved successfully.</response>
        /// <response code="204">Videos not found.</response>
        [HttpGet]
        public async Task<IActionResult> GetMetadata([FromQuery]VideoParameters parameters, CancellationToken cancellationToken)
        {
            var videos = await _videoService.GetVideosMetadata(parameters, cancellationToken);
            if (videos.Count == 0) return NoContent();

            return Ok(videos);
        }
        
        /// <summary>
        /// Retrieves a video metadata.
        /// </summary>
        /// <param name="videoId">Id of the video.</param>
        /// <returns>HTTP 200 status code with video metadata if the video exists.</returns>
        /// <response code="200">Video metadata retrieved successfully.</response>
        /// <response code="404">Video not found.</response>
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetMetadata(string videoId, CancellationToken cancellationToken)
        {
            var video = await _videoService.GetVideoMetadata(videoId, cancellationToken);
            if (video == null) return NotFound();

            return Ok(video);
        }
    }
}
