using System.Diagnostics;
using Microsoft.Extensions.Options;
using VideoProcessingService.Core.Models;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Infrastructure.Video.Builders;

namespace VideoProcessingService.Infrastructure.Video
{
    public class VideoConverter: IVideoConverter
    {
        private readonly ILoggerManager _logger;
        private readonly ConversionConfiguration _config;
        private readonly ThumbnailCreator _thumbnailCreator;
        private readonly MediaProcessor _processor;
        
        public VideoConverter(ILoggerManager logger, IOptions<ConversionConfiguration> conversionConfig)
        {
            _logger = logger;
            _config = conversionConfig.Value;
            _processor = new MediaProcessor(conversionConfig);
            _thumbnailCreator = new ThumbnailCreator(conversionConfig);
        }
        
        public async Task<ConversionResult> ConvertAsync(string inputFilePath, string outputDirectory)
        {
            var masterPlaylistName = "master.m3u8";
            var masterPlaylistPath = Path.Combine(outputDirectory, masterPlaylistName);

            var extractor = new MetadataExtractor(_processor);
            var hasAudio = await extractor.VideoHasAudio(inputFilePath);
            
            IHlsArgsBuilder builder = _config.Resolutions.Count > 0
                ? new MultiResolutionHlsArgsBuilder(_config)
                : new HlsArgsBuilder(_config);
            string arguments = builder.Build(inputFilePath, masterPlaylistName, hasAudio);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            _logger.LogDebug($"ffmpeg args : {arguments}");
            await _processor.StartProcess(MediaProcessor.Program.FFmpeg, outputDirectory, arguments);
            
            stopwatch.Stop();
            _logger.LogInfo($"Conversion took {stopwatch.Elapsed.TotalSeconds} seconds");

            var segments = Directory.GetFiles(outputDirectory, "*.ts").ToList();
            var playlists = Directory.GetFiles(outputDirectory, "*.m3u8").ToList();
            playlists.Remove(masterPlaylistPath);

            if (segments.Count == 0)
                throw new Exception("No video segments generated after conversion");

            LogSegmentsSize(segments);
            
            var durationSeconds = await extractor.VideoDuration(segments.First());
            var thumbnailResult = await _thumbnailCreator.Create(segments.First(),
                durationSeconds / 2);

            var subFiles = new List<string>();
            subFiles.AddRange(playlists);
            subFiles.AddRange(segments);
            
            return new ConversionResult(
                IndexFilePath: masterPlaylistPath,
                ThumbnailPath: thumbnailResult.ImagePath,
                SubFilesPaths: subFiles
                );
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
