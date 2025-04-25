using System.Text;

namespace VideoProcessingService.Infrastructure.Video.Builders;

public class HlsArgsBuilder: IHlsArgsBuilder
{
    private readonly ConversionConfiguration _config;

    public HlsArgsBuilder(ConversionConfiguration config)
    {
        _config = config;
    }

    public string Build(string inputFilePath, string masterPlaylistName, bool videoHasAudio)
    {
        var args = new StringBuilder();
        args.Append($"-i {inputFilePath} ");

        args.Append("-map 0:v ");
        if (videoHasAudio)
            args.Append("-map 0:a ");

        args.Append("-c:v copy ");
        if (videoHasAudio)
            args.Append("-c:a aac ");

        args.Append(
            $@"-f hls -hls_time {_config.SegmentDurationInSeconds} -hls_playlist_type vod -hls_segment_filename ""segment_%03d.ts"" ");
        args.Append($"\"{masterPlaylistName}\"");

        return args.ToString();
    }
}