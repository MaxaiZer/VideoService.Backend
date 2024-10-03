using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
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
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["ffmpegPath"]).Returns(_ffmpegPath);
        string tmpDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDirectory);

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData/rabbit320.mp4");
        var videoConverter = new VideoConverter(mockConfig.Object);

        ConversionResult result;
        try
        {
            result = await videoConverter.ConvertAsync(filePath, tmpDirectory);
            result.Should().NotBeNull();

            string masterPlaylistContent = File.ReadAllText(result.IndexFilePath);
            string playlistContent = File.ReadAllText(result.SubFilesPaths.First(f => f.EndsWith(".m3u8")));
        
            HlsParser.ExtractFirstPlaylistUrl(masterPlaylistContent).Should().NotBeNullOrEmpty();
            HlsParser.ExtractFirstSegmentUrl(playlistContent).Should().NotBeNullOrEmpty();
        }
        finally
        {
            Directory.Delete(tmpDirectory, recursive: true);
        }
    }
}