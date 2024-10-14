using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace VideoProcessingService.Infrastructure.Video;

public class MediaProcessor
{
    public enum Program
    {
        FFmpeg,
        FFprobe
    }
    
    private readonly string? _ffmpegPath;
    private readonly string? _ffprobePath;
    
    public MediaProcessor(IConfiguration configuration)
    {
        _ffmpegPath = configuration["ffmpegPath"];
        _ffprobePath = configuration["ffprobePath"];
    }
    
    public async Task StartProcess(Program program, string workingDirectory, string arguments)
    {
        string programPath;

        switch (program)
        {
            case Program.FFmpeg:
                programPath = string.IsNullOrEmpty(_ffmpegPath) ? "ffmpeg" : _ffmpegPath;
                break;
            case Program.FFprobe:
                programPath = string.IsNullOrEmpty(_ffprobePath) ? "ffprobe" : _ffprobePath;
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
        process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            
        var errors = new StringBuilder();
            
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
            
      //  if (process.ExitCode != 0) //don't use errors.Length != 0, ffmpeg logs all in error
      //      throw new Exception($"FFmpeg exited with code {process.ExitCode} and error: " + errors);
    }
}