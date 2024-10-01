namespace Shared.Helpers;

public static class StorageFileNamingHelper
{
    public static string GetNameForVideoMasterPlaylist(string videoId)
        => $"{videoId}/playlist";

    public static string GetNameForVideoSubFile(string videoId, string fileName)
        => $"{videoId}/{fileName}";    
}