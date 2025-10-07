#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Infrastructure.Setting
{
    public class JwtSettings
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        // Required configuration populated from appsettings; using 'required' to indicate these must be provided at startup
        public required string Key { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshSlidingDays { get; set; }
        public int RefreshAbsoluteDays { get; set; }
    }
}

