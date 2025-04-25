namespace VideoProcessingService.Infrastructure.Video.Builders;

public interface IHlsArgsBuilder
{
    string Build(string inputFilePath, string masterPlaylistName, bool videoHasAudio);
}