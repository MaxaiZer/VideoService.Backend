using System.Security.Claims;
using CoreService.Application.Dto;
using CoreService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

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
                return Unauthorized("User authentication required.");
            
            videoUploadDto.UserId = userId;
            
            await _videoService.AddVideo(videoUploadDto);
            return Ok();
        }

        /// <summary>
        /// Retrieves a videos metadata.
        /// </summary>
        /// <param name="parameters">Search parameters</param>
        /// <returns>HTTP 200 status code with video metadata if the video exists.</returns>
        /// <response code="200">Videos metadata retrieved successfully.</response>
        /// <response code="204">Videos not found.</response>
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
        
        /// <summary>
        /// Retrieves the master playlist file for the specified video.
        /// </summary>
        /// <param name="videoId">The ID of the video.</param>
        /// <returns>HTTP 200 status code with the master playlist file if found; otherwise, returns a 404 status code.</returns>
        /// <response code="200">Master playlist retrieved successfully.</response>
        /// <response code="404">Master playlist not found.</response>
        [HttpGet("{videoId}/files/master-playlist")]
        public async Task<IActionResult> GetMasterPlaylist(Guid videoId)
        {
            var playlist = await _videoService.GetMasterPlaylist(videoId);
            if (playlist == null) return NotFound();

            return File(playlist, "application/vnd.apple.mpegurl");
        }
        
        /// <summary>
        /// Retrieves a specific file, related to video.
        /// </summary>
        /// <param name="videoId">The ID of the video.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>HTTP 200 status code with the file if found; otherwise, returns a 404 status code.</returns>
        /// <response code="200">File retrieved successfully.</response>
        /// <response code="404">File not found.</response>
        [HttpGet("{videoId:guid}/files/{fileName:regex(^.*\\.(m3u8|ts|jpg)$)}")]
        public async Task<IActionResult> GetSubFile(Guid videoId, string fileName)
        {
            var file = await _videoService.GetSubFile(videoId, fileName);
            if (file == null) return NotFound();

            var contentType = GetContentType(fileName);
            return File(file, contentType);
        }

        private string GetContentType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            
            provider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
            provider.Mappings[".ts"] = "video/MP2T";
            
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream"; 
            }
    
            return contentType;
        }
    }
}
