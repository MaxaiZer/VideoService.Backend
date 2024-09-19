namespace CoreService.Application.Interfaces;

public interface IApplicationUser
{
    string Id { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiryTime { get; set; }
}