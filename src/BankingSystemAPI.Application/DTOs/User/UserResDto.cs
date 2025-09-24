using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.DTOs.Account;
using System;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.DTOs.User
{
    /// <summary>
    /// User response DTO containing profile and roles information.
    /// </summary>
    public class UserResDto
    {
        /// <summary>
        /// User identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Full name of the user.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// National identifier for the user.
        /// </summary>
        public string NationalId { get; set; }

        /// <summary>
        /// Contact phone number.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Roles assigned to the user.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Bank name the user is associated with.
        /// </summary>
        public string BankName { get; set; }

        /// <summary>
        /// Indicates if the user is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// List of accounts owned by the user.
        /// </summary>
        public IList<AccountDto> Accounts { get; set; } = new List<AccountDto>();
    }
}
