using System.Text;

namespace VideoProcessingService.Infrastructure.Video.Builders;

public class MultiResolutionHlsArgsBuilder: IHlsArgsBuilder
{
    private readonly ConversionConfiguration _config;

    public MultiResolutionHlsArgsBuilder(ConversionConfiguration config)
    {
        _config = config;
    }

    public string Build(string inputFilePath, string masterPlaylistName, bool videoHasAudio)
    {
        int resolutionsCount = _config.Resolutions.Count;

        // Set input file and split video stream into N versions
        var args = new StringBuilder($@"-i {inputFilePath} -filter_complex ""[0:v]split={resolutionsCount}");

        // Build split outputs like [v0][v1]...[vN]
        for (int i = 0; i < resolutionsCount; i++) args.Append($"[v{i}]");
        args.Append(";");

        // For each resolution, scale and optionally pad (letterbox) the video
        for (var i = 0; i < resolutionsCount; i++)
        {
            var res = _config.Resolutions[i];
            args.Append($"[v{i}]scale=");

            if (_config.AddLetterbox) // Letterbox is needed to convert vertical videos to horizontal resolutions without stretching them
            {
                // Dynamically compute scale with padding (letterbox for vertical videos)
                args.Append($"'if(gt(iw/ih,{res.Width}/{res.Height}),{res.Width},-1)':");
                args.Append($"'if(gt(iw/ih,{res.Width}/{res.Height}),-1,{res.Height})',");
                args.Append($"pad={res.Width}:{res.Height}:({res.Width}-iw)/2:({res.Height}-ih)/2");
            }
            // Simple scale to target width/height (no padding)
            else
                args.Append($"{res.Width}:{res.Height}");

            args.Append($"[v{i}out]");
            if (i != resolutionsCount - 1) args.Append(";");
        }

        args.Append(@""" ");

        // Map each processed video stream and configure encoding + optional audio
        for (var i = 0; i < resolutionsCount; i++)
        {
            var res = _config.Resolutions[i];
            args.Append($"-map [v{i}out] -c:v libx264 -c:a aac -b:v:{i} {res.Bitrate} ");
            if (videoHasAudio)
            {
                args.Append("-map 0:a ");
            }
        }

        // HLS output settings
        args.Append(
            $@"-f hls -hls_time {_config.SegmentDurationInSeconds} -hls_playlist_type vod -hls_segment_filename ""segment_%v_%03d.ts"" ");

        args.Append($@"-master_pl_name ""{masterPlaylistName}"" -var_stream_map """);

        // Stream mapping per variant (v:X[,a:X])
        for (var i = 0; i < resolutionsCount; i++)
        {
            var audio = videoHasAudio ? ",a:" + i : "";
            args.Append($"v:{i}{audio} ");
        }

        args.Append(@""" stream_%v.m3u8");
        return args.ToString();
    }
}