using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.FileStorage;

public class MinioConfiguration
{
    [Required]
    public static string Section => "MinIO";
    [Required]
    public string Endpoint { get; init; }
    [Required]
    public string PublicUrl { get; init; }
    [Required]
    public string AccessKey { get; init; }
    [Required]
    public string SecretKey { get; init; }
    [Required]
    public string BucketName { get; init; }
    [Required] 
    public string TmpFolder { get; init; }
}