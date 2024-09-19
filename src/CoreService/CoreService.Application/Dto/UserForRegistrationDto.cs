using System.ComponentModel.DataAnnotations;

namespace CoreService.Application.Dto
{
    public record UserForRegistrationDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string? UserName { get; init; }
        
        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; init; }
    }

}
