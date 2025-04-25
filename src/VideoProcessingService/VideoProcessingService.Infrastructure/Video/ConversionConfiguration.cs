using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
    public string ResolutionsJson { get; set; }
    
    public List<Resolution>? Resolutions =>
        string.IsNullOrWhiteSpace(ResolutionsJson)
            ? new()
            : JsonSerializer.Deserialize<List<Resolution>>(ResolutionsJson);
    
    [Required]
    public int SegmentDurationInSeconds { get; set; }
    
    [Required]
    public bool AddLetterbox { get; set; }
    
    public string? FFmpegPath { get; set; }
    
    public string? FFprobePath { get; set; }
}