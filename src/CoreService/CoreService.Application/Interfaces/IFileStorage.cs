namespace CoreService.Application.Interfaces
{
    public interface IFileStorage
    {
        public Task<string> GeneratePutUrlForTempFile(string fileName);
    }
}
