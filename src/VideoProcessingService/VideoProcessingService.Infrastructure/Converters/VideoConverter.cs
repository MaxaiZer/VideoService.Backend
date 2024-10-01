using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using HlsConversionResult = VideoProcessingService.Core.Models.HlsConversionResult;
using IVideoConverter = VideoProcessingService.Core.Interfaces.IVideoConverter;

namespace VideoProcessingService.Infrastructure.Converters
{
    public class VideoConverter: IVideoConverter
    {
        private readonly string? _ffmpegPath;
        public VideoConverter(IConfiguration configuration)
        {
            _ffmpegPath = configuration["ffmpegPath"];
        }

        public async Task<HlsConversionResult> ConvertToHlsAsync(string inputFilePath, string outputDirectory)
        {
            var masterPlaylistName = "master.m3u8";
            var masterPlaylistPath = Path.Combine(outputDirectory, masterPlaylistName);
            
            //ToDo: check if video has audio
            string arguments = $@"-i {inputFilePath} -filter_complex ""[0:v]split=2[v1][v2];" + 
                               "[v1]scale='if(gt(iw/ih,1280/720),1280,-1)':'if(gt(iw/ih,1280/720),-1,720)',pad=1280:720:(1280-iw)/2:(720-ih)/2[v1out];" + 
                               @"[v2]scale='if(gt(iw/ih,854/480),854,-1)':'if(gt(iw/ih,854/480),-1,480)',pad=854:480:(854-iw)/2:(480-ih)/2[v2out]"" " + 
                               "-map [v1out] -map 0:a -c:v libx264 -c:a aac -b:v:0 5000k " + 
                               "-map [v2out] -map 0:a -c:v libx264 -c:a aac -b:v:1 3000k " + 
                               @"-f hls -hls_time 10 -hls_playlist_type vod -hls_segment_filename ""segment_%v_%03d.ts"" " +
                               $@"-master_pl_name ""{masterPlaylistName}"" -var_stream_map ""v:0,a:0 v:1,a:1"" stream_%v.m3u8";
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            await StartFFmpegProcess(outputDirectory, arguments);
            
            stopwatch.Stop();
            Console.WriteLine($"Conversion took {stopwatch.Elapsed.TotalSeconds} seconds");

            var segments = Directory.GetFiles(outputDirectory, "*.ts").ToList();
            var playlists = Directory.GetFiles(outputDirectory, "*.m3u8").ToList();
            playlists.Remove(masterPlaylistPath);

            LogSegmentsSize(segments);
            
            return new HlsConversionResult(
                MasterPlaylistPath: masterPlaylistPath,
                PlaylistsFilePaths: playlists,
                SegmentsFilePaths: segments
                );
        }
        
        private async Task StartFFmpegProcess(string workingDirectory, string arguments)
        {
            string programPath = string.IsNullOrEmpty(_ffmpegPath) ? "ffmpeg" : _ffmpegPath;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = programPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using Process process = new();
            process.StartInfo = processStartInfo;
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            
            string errors = "";
            
            process.ErrorDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                errors += args.Data;
            };
            
            process.Start();
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0) //don't use errors.Length != 0, ffmpeg logs all in error
                throw new Exception(errors);
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
