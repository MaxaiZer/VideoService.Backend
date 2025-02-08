namespace Domain.Entities;

public class VideoSearchParameters
{
    public string? UserId { get; }
    public string? SearchQuery { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public VideoSearchParameters(string? userId, string? searchQuery, int pageNumber, int pageSize)
    {
        UserId = userId;
        SearchQuery = searchQuery;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}