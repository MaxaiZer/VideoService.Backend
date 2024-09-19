namespace VideoProcessingService.Core.Models;

public record HlsConversionResult(string MasterPlaylistPath, IEnumerable<string> PlaylistsFilePaths,  IEnumerable<string> SegmentsFilePaths);
