using CoreService.Application.Dto;
using Microsoft.IdentityModel.SecurityTokenService;

namespace CoreService.Application.Common.Exceptions
{
    public class RefreshTokenBadRequest : BadRequestException
    {
        public RefreshTokenBadRequest()
            : base($"Invalid client request. The {nameof(TokenDto)} has some invalid values.")
        {
        }
    }
}
