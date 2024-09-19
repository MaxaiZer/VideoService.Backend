namespace CoreService.Application.Common.Models;

public record RefreshTokenResult(string RefreshToken, DateTimeOffset ExpiryTime);