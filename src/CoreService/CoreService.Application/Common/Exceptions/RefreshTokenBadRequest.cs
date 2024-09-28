using Microsoft.IdentityModel.SecurityTokenService;

namespace CoreService.Application.Common.Exceptions
{
    public class RefreshTokenBadRequest : BadRequestException
    {
        public RefreshTokenBadRequest()
            : base($"Invalid refresh token request. Access or refresh token is invalid.")
        {
        }
    }
}
