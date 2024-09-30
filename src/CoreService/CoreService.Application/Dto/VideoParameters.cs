using CoreService.Application.Common.Models;

namespace CoreService.Application.Dto;

public class VideoParameters: RequestParameters
{
    public string? SearchQuery { get; set; }
}