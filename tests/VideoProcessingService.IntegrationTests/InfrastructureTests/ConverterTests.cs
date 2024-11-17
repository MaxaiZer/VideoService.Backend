using FluentAssertions;
using Microsoft.Extensions.Options;
using VideoProcessingService.Core.Models;
using VideoProcessingService.Infrastructure.Video;
using VideoProcessingService.IntegrationTests.Tools;

namespace VideoProcessingService.IntegrationTests.InfrastructureTests;

public class ConverterTests: IClassFixture<FFmpegFixture>
{
    private readonly string _ffmpegPath;

    public ConverterTests(FFmpegFixture ffmpeg)
    {
        _ffmpegPath = ffmpeg.FFmpegPath;
    }

    [Fact]
    public async Task HlsConversion_WhenValidFile_ShouldBeSuccessful()
    {
        var config = new ConversionConfiguration
        {
            Resolutions = new List<Resolution>
            {
                new() { Width = 1280, Height = 720, Bitrate = "5000k" },
                new() { Width = 854, Height = 480, Bitrate = "3000k" },
                new() { Width= 640, Height = 360, Bitrate = "1500k" }
            },
            FFmpegPath = _ffmpegPath
        };
        var mockOptions = Options.Create(config);
  
        string tmpDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDirectory);

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData/rabbit320.mp4");
        var videoConverter = new VideoConverter(mockOptions);

        ConversionResult result;
        try
        {
            result = await videoConverter.ConvertAsync(filePath, tmpDirectory);
            result.Should().NotBeNull();

            string masterPlaylistContent = await File.ReadAllTextAsync(result.IndexFilePath);
            string playlistContent = await File.ReadAllTextAsync(result.SubFilesPaths.First(f => f.EndsWith(".m3u8")));
        
            HlsParser.ExtractFirstPlaylistUrl(masterPlaylistContent).Should().NotBeNullOrEmpty();
            HlsParser.ExtractFirstSegmentUrl(playlistContent).Should().NotBeNullOrEmpty();
        }
        finally
        {
            Directory.Delete(tmpDirectory, recursive: true);
        }
    }
}