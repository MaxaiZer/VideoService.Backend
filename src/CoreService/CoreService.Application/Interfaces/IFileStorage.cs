namespace CoreService.Application.Interfaces
{
    public interface IFileStorage
    {
        public Task PutFileAsync(string name, Stream stream);

        public Task<Stream> GetFileAsync(string name);

        public Task<Stream> GetFileAsync(string name, long offset, long length);

        public Task<string> GeneratePresignedPutUrl(string fileName);
    }
}
