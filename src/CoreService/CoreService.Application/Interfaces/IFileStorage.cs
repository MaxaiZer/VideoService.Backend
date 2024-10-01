namespace CoreService.Application.Interfaces
{
    public interface IFileStorage
    {
        public Task<Stream> GetFileAsync(string name, bool isTemporary = false);
        
        public Task<string> GeneratePresignedPutUrl(string fileName);
    }
}
