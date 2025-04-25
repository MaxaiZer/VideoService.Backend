using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Core.Models;
using VideoProcessingService.Infrastructure.Video;
using VideoProcessingService.IntegrationTests.Tools;

namespace VideoProcessingService.IntegrationTests.InfrastructureTests;

public class ConverterTests: IClassFixture<FFmpegFixture>
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public ConverterTests(FFmpegFixture ffmpeg)
    {
        _ffmpegPath = ffmpeg.FFmpegPath;
        _ffprobePath = ffmpeg.FFprobePath;
    }

    [Fact]
    public async Task HlsMultiResolutionConversion_WhenValidFile_ShouldBeSuccessful()
    {
        var config = new ConversionConfiguration
        {
            ResolutionsJson="[{\"Width\":1280,\"Height\":720,\"Bitrate\":\"5000k\"}," +
                            "{\"Width\":854,\"Height\":480,\"Bitrate\":\"3000k\"}," +
                            "{\"Width\":640,\"Height\":360,\"Bitrate\":\"1500k\"}]\n",
            SegmentDurationInSeconds = 10,
            AddLetterbox = true,
            FFmpegPath = _ffmpegPath,
            FFprobePath = _ffprobePath
        };
        var mockOptions = Options.Create(config);
  
        string tmpDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDirectory);

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData/rabbit320.mp4");
        var videoConverter = new VideoConverter(new Mock<ILoggerManager>().Object, mockOptions);

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
    
    [Fact]
    public async Task HlsConversion_WhenValidFile_ShouldBeSuccessful()
    {
        var config = new ConversionConfiguration
        {
            FFmpegPath = _ffmpegPath,
            FFprobePath = _ffprobePath,
            SegmentDurationInSeconds = 10
        };
        var mockOptions = Options.Create(config);
  
        string tmpDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDirectory);

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData/rabbit320.mp4");
        var videoConverter = new VideoConverter(new Mock<ILoggerManager>().Object, mockOptions);

        ConversionResult result;
        try
        {
            result = await videoConverter.ConvertAsync(filePath, tmpDirectory);
            result.Should().NotBeNull();

            //without resolutions master playlist is the only playlist
            string masterPlaylistContent = await File.ReadAllTextAsync(result.IndexFilePath);
            HlsParser.ExtractFirstSegmentUrl(masterPlaylistContent).Should().NotBeNullOrEmpty();
        }
        finally
        {
            Directory.Delete(tmpDirectory, recursive: true);
        }
    }
}