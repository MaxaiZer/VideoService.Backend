using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VideoProcessingService.IntegrationTests.Tools
{
    public class FFmpegFixture : IDisposable
    {
        public string FFmpegPath { get; private set; }
        public bool deleteFFmpegOnDispose = false;

        private readonly string _ffmpegArchivePath = Path.Combine(Path.GetTempPath(), "ffmpeg_archive");
        private readonly string _ffmpegDirPath = Path.Combine(Path.GetTempPath(), "ffmpeg");

        public FFmpegFixture()
        {
            if (!Directory.Exists(_ffmpegDirPath))
                Directory.CreateDirectory(_ffmpegDirPath);
            
            FFmpegPath = DownloadAndExtractFFmpegAsync().GetAwaiter().GetResult();
        }

        private async Task<string> DownloadAndExtractFFmpegAsync()
        {
            var executable = FindExecutable(_ffmpegDirPath);
            if (executable != null)
            {
                return executable;
            }
            
            string ffmpegUrl;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ffmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ffmpegUrl = "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ffmpegUrl = "https://evermeet.cx/ffmpeg/ffmpeg-release.zip";
            }
            else
            {
                throw new PlatformNotSupportedException("This OS platform is not supported.");
            }

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(ffmpegUrl);
                response.EnsureSuccessStatusCode();
                await using (var fs = new FileStream(_ffmpegArchivePath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(_ffmpegArchivePath, _ffmpegDirPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-xf {_ffmpegArchivePath} -C {_ffmpegDirPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }

            return FindExecutable(_ffmpegDirPath) ?? throw new Exception("Error during installing ffmpeg");
        }

        private string? FindExecutable(string directory)
        {
            string ffmpegExecutableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            string[] foundFiles = Directory.GetFiles(directory, ffmpegExecutableName, SearchOption.AllDirectories);

            if (foundFiles.Length > 0)
                return foundFiles[0];

            return null;
        }

        public void Dispose()
        {
            if (deleteFFmpegOnDispose)
            {
                File.Delete(_ffmpegArchivePath);
                Directory.Delete(_ffmpegDirPath, true);
            }
        }
    }
 }
