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
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
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
    }
}

