using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.FileStorage;

public class MinioConfiguration
{
    [Required]
    public static string Section => "MinIO";
    [Required]
    public string Endpoint { get; set; }
    [Required]
    public string PublicHost { get; set; }
    [Required]
    public string AccessKey { get; set; }
    [Required]
    public string SecretKey { get; set; }
    [Required]
    public string BucketName { get; set; }
    [Required] 
    public string TmpFolder { get; set; }
}