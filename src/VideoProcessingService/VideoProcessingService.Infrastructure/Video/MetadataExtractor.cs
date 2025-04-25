using System.Globalization;

namespace VideoProcessingService.Infrastructure.Video;

public class MetadataExtractor
{
    private MediaProcessor _processor;
    
    public MetadataExtractor(MediaProcessor processor)
    {
        _processor = processor;
    }
    
    public async Task<bool> VideoHasAudio(string filePath)
    {
        var output = await _processor.StartProcess(MediaProcessor.Program.FFprobe, Path.GetDirectoryName(filePath) ?? "/",
            $"-i \"{Path.GetFileName(filePath)}\" -show_streams -select_streams a -loglevel error");
        return !string.IsNullOrEmpty(output);
    }
    
    public async Task<double> VideoDuration(string filePath)
    {
        string durationOutput = await _processor.StartProcess(
            MediaProcessor.Program.FFprobe,
            Path.GetDirectoryName(filePath),
            $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{Path.GetFileName(filePath)}\""
        );

        if (!double.TryParse(durationOutput.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double durationSeconds))
            throw new Exception($"Cannot determine video duration, output: '{durationOutput}'");

        return durationSeconds;
    }
}