using System.Data;
using Dapper;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Core.Models;

namespace VideoProcessingService.Infrastructure.Data;

public class VideoProcessingRequestRepository: IVideoProcessingRequestRepository
{
    private readonly IDbConnection _connection;

    public VideoProcessingRequestRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<VideoProcessingRequest?> GetById(string id)
    {
        OpenConnectionIfClosed();
        string sql = @"SELECT * FROM ""VideoProcessingRequests"" WHERE ""Id"" = @Id";
        return await _connection.QueryFirstOrDefaultAsync<VideoProcessingRequest>(sql, new { Id = id });
    }

    public async Task<bool> UpdateStatus(string id, VideoProcessingRequest.ProcessingStatus newStatus)
    {
        OpenConnectionIfClosed();
        string sql = @"UPDATE ""VideoProcessingRequests"" SET ""Status"" = @Status WHERE ""Id"" = @Id RETURNING *";
        return await _connection.ExecuteAsync(sql, new { Id = id, Status = newStatus.ToString() }) == 1;
    }

    private void OpenConnectionIfClosed()
    {
        if (_connection.State == ConnectionState.Closed)
            _connection.Open();
    }

}