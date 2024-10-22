using System.Security.Cryptography;
using System.Text;

namespace CoreService.Application.Common.Helpers;

public static class TokenHelper
{
    public static string HashToken(string token)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}