using System.Data;
using Dapper;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.Data;

public class VideoRepository: IVideoRepository
{
    private readonly IDbConnection _connection;

    public VideoRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> MarkVideoAsProcessed(string videoId)
    {
        OpenConnectionIfClosed();
        string sql = @"UPDATE ""Videos"" SET ""Processed"" = true WHERE ""Id"" = @VideoId RETURNING *";
        return await _connection.ExecuteAsync(sql, new { VideoId = videoId }) == 1;
    }
    
    private void OpenConnectionIfClosed()
    {
        if (_connection.State == ConnectionState.Closed)
            _connection.Open();
    }
}