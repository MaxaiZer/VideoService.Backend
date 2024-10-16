namespace CoreService.IntegrationTests.Tools;

public record TestUserData(string Id, string Name, string Password, string RefreshToken, 
    DateTimeOffset RefreshTokenExpiryTime, string AccessToken);