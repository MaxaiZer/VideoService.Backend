namespace VideoProcessingService.Core.Models;

public record ConversionResult(
    string IndexFilePath,
    string ThumbnailPath,
    IEnumerable<string> SubFilesPaths
    );
