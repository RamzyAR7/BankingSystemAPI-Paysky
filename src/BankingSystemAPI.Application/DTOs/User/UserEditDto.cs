using System;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.User
{
    /// <summary>
    /// DTO to update a user (does not include password fields).
    /// </summary>
    public class UserEditDto
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
    }
}
