namespace VideoProcessingService.UnitTests.Tools
{
    public class TempFileFixture : IDisposable
    {
        public string FilePath { get; private set; }

        public TempFileFixture(string path)
        {
            FilePath = path;
            File.Create(FilePath).Close();
        }

        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}
