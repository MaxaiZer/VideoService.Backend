namespace VideoProcessingService.IntegrationTests.Tools;

public static class HlsParser
{
    
    public static string ExtractFirstPlaylistUrl(string playlistContent)
    {
        using StringReader reader = new(playlistContent);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.EndsWith(".m3u8"))
            {
                return line;
            }
        }

        throw new Exception("Can't find segment in content: " + playlistContent);
    }  
    
    public static string ExtractFirstSegmentUrl(string playlistContent)
    {
        using StringReader reader = new(playlistContent);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.EndsWith(".ts"))
            {
                return line;
            }
        }

        throw new Exception("Can't find segment in content: " + playlistContent);
    }
}