namespace CoreService.Application.Dto
{
    public class VideoUploadDto
    {
        public string Name { get; init; }
        public string? UserId { get; set; }
        public string Description { get; init; }
        public string UploadedVideoId { get; init; }
    }
}
