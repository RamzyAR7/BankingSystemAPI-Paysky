#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Constant
{
    /// <summary>
    /// Interest payment frequency for savings accounts.
    /// </summary>
    public enum InterestType
    {
        /// <summary>
        /// Interest applied every month (value = 1).
        /// </summary>
        Monthly = 1,

        /// <summary>
        /// Interest applied every quarter (value = 2).
        /// </summary>
        Quarterly,

        /// <summary>
        /// Interest applied annually (value = 3).
        /// </summary>
        Annually,

        /// <summary>
        /// For testing purposes: interest applied every 5 minutes (value = 4).
        /// </summary>
        every5minutes
    }
}

