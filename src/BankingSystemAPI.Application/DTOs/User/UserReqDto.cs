using System;

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
        public string Email { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Full display name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// National identification number (Egyptian national ID - 14 digits).
        /// </summary>
        public string NationalId { get; set; }

        /// <summary>
        /// Contact phone number (exactly 11 digits).
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
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
        public string Password { get; set; }

        /// <summary>
        /// Password confirmation that must match Password.
        /// </summary>
        public string PasswordConfirm { get; set; }
    }
}
