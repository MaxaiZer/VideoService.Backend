namespace Shared.Helpers;

public static class StorageFileNamingHelper
{
    public static string GetNameForVideoMasterPlaylist(string videoId)
        => $"{videoId}_playlist";

    public static string GetNameForVideoSubFile(string videoId, string fileName)
        => $"{videoId}_{fileName}";    
}