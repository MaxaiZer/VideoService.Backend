using Microsoft.IdentityModel.SecurityTokenService;

namespace CoreService.Application.Common.Exceptions
{
    public class RefreshTokenException : BadRequestException
    {
        public RefreshTokenException()
            : base($"Invalid refresh token request. Access or refresh token is invalid.")
        {
        }
    }
}
