using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using VideoProcessingService.Core.Models;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.Video
{
    public class VideoConverter: IVideoConverter
    {
        private readonly ConversionConfiguration _config;
        private readonly ThumbnailCreator _thumbnailCreator;
        private readonly MediaProcessor _processor;
        
        private const int _segmentDurationInSeconds = 10;
        
        public VideoConverter(IOptions<ConversionConfiguration> conversionConfig)
        {
            _config = conversionConfig.Value;
            _processor = new MediaProcessor(conversionConfig);
            _thumbnailCreator = new ThumbnailCreator(conversionConfig);
        }
        
        public async Task<ConversionResult> ConvertAsync(string inputFilePath, string outputDirectory)
        {
            var masterPlaylistName = "master.m3u8";
            var masterPlaylistPath = Path.Combine(outputDirectory, masterPlaylistName);
            
            //ToDo: check if video has audio
            string arguments = BuildHlsConversionArgs(inputFilePath, masterPlaylistName, addLetterbox: true);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            await _processor.StartProcess(MediaProcessor.Program.FFmpeg, outputDirectory, arguments);
            
            stopwatch.Stop();
            Console.WriteLine($"Conversion took {stopwatch.Elapsed.TotalSeconds} seconds");

            var segments = Directory.GetFiles(outputDirectory, "*.ts").ToList();
            var playlists = Directory.GetFiles(outputDirectory, "*.m3u8").ToList();
            playlists.Remove(masterPlaylistPath);

            LogSegmentsSize(segments);

            var thumbnailResult = await _thumbnailCreator.Create(segments.First(),
                _segmentDurationInSeconds / 2);

            var subFiles = new List<string>();
            subFiles.AddRange(playlists);
            subFiles.AddRange(segments);
            
            return new ConversionResult(
                IndexFilePath: masterPlaylistPath,
                ThumbnailPath: thumbnailResult.ImagePath,
                SubFilesPaths: subFiles
                );
        }

        private string BuildHlsConversionArgs(string inputFilePath, string masterPlaylistName, bool addLetterbox)
        {
            int resolutionsCount = _config.Resolutions.Count;
            var args = new StringBuilder($@"-i {inputFilePath} -filter_complex ""[0:v]split={resolutionsCount}");

            for (int i = 0; i < resolutionsCount; i++)
                args.Append($"[v{i}]");
            args.Append(";");
            
            for (var i = 0; i < resolutionsCount; i++)
            {
                var res = _config.Resolutions[i];
                args.Append($"[v{i}]scale=");

                if (addLetterbox) //to add letterbox to vertical videos
                {
                    args.Append($"'if(gt(iw/ih,{res.Width}/{res.Height}),{res.Width},-1)':");
                    args.Append($"'if(gt(iw/ih,{res.Width}/{res.Height}),-1,{res.Height})',");
                    args.Append($"pad={res.Width}:{res.Height}:({res.Width}-iw)/2:({res.Height}-ih)/2");
                }
                else
                    args.Append($"{res.Width}:{res.Height}");

                args.Append($"[v{i}out]");
                if (i != resolutionsCount - 1)
                    args.Append(";");
            }
            args.Append(@""" ");
            
            for (var i = 0; i < resolutionsCount; i++)
            {
                var res = _config.Resolutions[i];
                args.Append($"-map [v{i}out] -map 0:a -c:v libx264 -c:a aac -b:v:{i} {res.Bitrate} ");
            }

            args.Append(
                $@"-f hls -hls_time {_segmentDurationInSeconds} -hls_playlist_type vod -hls_segment_filename ""segment_%v_%03d.ts"" ");
            
            args.Append($@"-master_pl_name ""{masterPlaylistName}"" -var_stream_map """);
            for (var i = 0; i < resolutionsCount; i++)
                args.Append($"v:{i},a:{i} ");
            
            args.Append(@""" stream_%v.m3u8");
            return args.ToString();
        }
        
        private void LogSegmentsSize(List<string> segments)
        {
            var segmentSizesByResolution = new Dictionary<string, long>();
            
            foreach (var segment in segments)
            {
                var resolution = Path.GetFileName(segment).Split('_')[1];
                var size = new FileInfo(segment).Length;
                
                if (!segmentSizesByResolution.TryAdd(resolution, size))
                {
                    segmentSizesByResolution[resolution] += size;
                }
            }

            double totalSizeMb = 0;
            foreach (var resolution in segmentSizesByResolution.Keys)
            {
                var count = segments.Count / segmentSizesByResolution.Keys.Count;
                var sizeMb = segmentSizesByResolution[resolution] / 1024.0 / 1024.0;
                totalSizeMb += sizeMb;
                Console.WriteLine($"Resolution {resolution}:  average segment size {sizeMb/count:F2} MB");
                Console.WriteLine($"Resolution {resolution}:  total size {sizeMb:F2} MB");
            }
            
            Console.WriteLine($"Total size for all resolutions: {totalSizeMb:F2} MB");
        }
    }
}
