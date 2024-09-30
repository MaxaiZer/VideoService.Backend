namespace Domain.Entities;

public record ViewableVideoMetadata(
    string Id, 
    string UserId,
    string UserName, 
    string VideoName, 
    string Description, 
    DateTimeOffset CreatedAt
);