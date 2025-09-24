using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.User
{
    /// <summary>
    /// DTO to create or update a user.
    /// </summary>
    public class UserReqDto
    {
        /// <summary>
        /// Email address of user.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        /// <summary>
        /// Full display name.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string FullName { get; set; }

        /// <summary>
        /// National identification number (Egyptian national ID - 14 digits).
        /// </summary>
        [Required]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "National ID must contain only digits and be 14 characters long.")]
        public string NationalId { get; set; }

        /// <summary>
        /// Contact phone number (exactly 11 digits).
        /// </summary>
        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Phone number must be exactly 11 digits.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must contain only digits and be 11 characters long.")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [Required]
        public DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// User role.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Bank identifier.
        /// </summary>
        public int? BankId { get; set; }

        /// <summary>
        /// Plain text password. Must be provided when creating a user.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        /// <summary>
        /// Password confirmation that must match Password.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string PasswordConfirm { get; set; }
    }
}
