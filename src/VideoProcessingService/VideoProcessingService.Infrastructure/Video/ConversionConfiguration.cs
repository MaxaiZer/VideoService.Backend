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

    public List<Resolution> Resolutions { get; set; } = [];
    
    [Required]
    public int SegmentDurationInSeconds { get; set; }
    
    [Required]
    public bool AddLetterbox { get; set; }
    
    public string? FFmpegPath { get; set; }
    
    public string? FFprobePath { get; set; }
}