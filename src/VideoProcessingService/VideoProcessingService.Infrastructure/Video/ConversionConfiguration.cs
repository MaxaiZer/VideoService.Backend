using System.ComponentModel.DataAnnotations;

namespace VideoProcessingService.Infrastructure.Video;

public class Resolution
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Bitrate { get; init; }
}

public class ConversionConfiguration
{
    public static string Section => "Conversion";
    
    [Required]
    public List<Resolution> Resolutions { get; init; }
    
    public string? FFmpegPath { get; set; }
    
    public string? FFprobePath { get; set; }
}