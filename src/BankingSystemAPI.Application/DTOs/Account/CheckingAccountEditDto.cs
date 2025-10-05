#region Usings
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO used to edit checking account properties (does not allow editing balance).
    /// </summary>
    public class CheckingAccountEditDto
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public string UserId { get; set; }

        public int CurrencyId { get; set; }
        public decimal OverdraftLimit { get; set; }
    }
}

