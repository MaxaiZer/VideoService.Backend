using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace VideoProcessingService.Infrastructure.Video;

public class MediaProcessor
{
    public enum Program
    {
        FFmpeg,
        FFprobe
    }
    
    private readonly string? _ffmpegCustomPath;
    private readonly string? _ffprobeCustomPath;
    
    public MediaProcessor(IOptions<ConversionConfiguration> conversionConfig)
    {
        _ffmpegCustomPath = conversionConfig.Value.FFmpegPath;
        _ffprobeCustomPath = conversionConfig.Value.FFprobePath;
    }
    
    public async Task<string> StartProcess(Program program, string workingDirectory, string arguments)
    {
        string programPath;

        switch (program)
        {
            case Program.FFmpeg:
                programPath = string.IsNullOrEmpty(_ffmpegCustomPath) ? "ffmpeg" : _ffmpegCustomPath;
                break;
            case Program.FFprobe:
                programPath = string.IsNullOrEmpty(_ffprobeCustomPath) ? "ffprobe" : _ffprobeCustomPath;
                break;
            default:
                throw new NotImplementedException();
        }
        
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

        var output = new StringBuilder();
        var errors = new StringBuilder();
        
        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                output.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                Console.WriteLine(args.Data);
                errors.AppendLine(args.Data);
            }
        };
            
        process.Start();
            
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
            
        await process.WaitForExitAsync();
        process.WaitForExit(); //WaitForExitAsync doesn't wait for redirected output to complete!
            
        if (process.ExitCode != 0)
        {
            throw new Exception($"Process failed with exit code {process.ExitCode}. Error: {errors}");
        }
        return output.ToString();
    }
}