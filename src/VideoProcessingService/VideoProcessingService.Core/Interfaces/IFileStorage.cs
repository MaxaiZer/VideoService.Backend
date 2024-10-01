namespace VideoProcessingService.Core.Interfaces
{
    public interface IFileStorage
    {
        public Task PutFileAsync(string name, Stream stream);

        public Task<Stream> GetFileAsync(string name, bool isTemporary = false);
    }
}
