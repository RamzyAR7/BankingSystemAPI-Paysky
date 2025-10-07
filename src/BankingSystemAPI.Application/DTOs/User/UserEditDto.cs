#region Usings
using System;
#endregion


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
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Full display name.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// National identification number (Egyptian national ID - 14 digits).
        /// </summary>
        public string NationalId { get; set; } = string.Empty;

        /// <summary>
        /// Contact phone number (exactly 11 digits).
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Date of birth.
        /// </summary>
        public DateOnly DateOfBirth { get; set; }
    }
}

