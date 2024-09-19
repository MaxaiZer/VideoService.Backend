namespace CoreService.IntegrationTests.Tools;

internal static class Extensions
{
    public static string RemoveLastSegmentInUrl(this string str)
    {
        var segments = str.Split('/');
        if (segments.Length > 1)
        {
            return string.Join('/', segments, 0, segments.Length - 1) + "/";
        }

        return str;
    }
}