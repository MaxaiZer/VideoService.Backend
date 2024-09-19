namespace CoreService.Application.Dto;

public record HlsConversionResult(string MasterPlaylistPath, IEnumerable<string> PlaylistsFilePaths,  IEnumerable<string> SegmentsFilePaths);
