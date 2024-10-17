namespace VideoProcessingService.UnitTests.Tools
{
    public class TempFileFixture : IDisposable
    {
        public string FilePath { get; private set; }

        private bool _disposed;

        public TempFileFixture(string path)
        {
            FilePath = path;
            File.Create(FilePath).Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (!disposing) return;
            
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            _disposed = true;
        }
    }
}
