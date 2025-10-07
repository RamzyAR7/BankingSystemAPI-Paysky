#region Usings
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.DTOs.Auth
{
    public class AuthResultDto
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public bool Succeeded { get; set; }
        public List<IdentityError> Errors { get; set; } = new();
        public AuthResDto? AuthData { get; set; }
        public string? Message { get; set; }
    }
}

