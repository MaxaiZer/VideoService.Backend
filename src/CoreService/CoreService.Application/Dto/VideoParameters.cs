using CoreService.Application.Common.Models;

namespace CoreService.Application.Dto;

public class VideoParameters: RequestParameters
{
    public string? UserId { get; set; }
    public string? SearchQuery { get; set; }
}