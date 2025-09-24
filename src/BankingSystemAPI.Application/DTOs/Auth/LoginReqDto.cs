using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Auth
{
    public class LoginReqDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string? Password { get; set; }
    }
}
