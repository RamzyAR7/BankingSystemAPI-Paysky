#region Usings
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.DTOs.InterestLog
{
    public class InterestLogsPagedDto
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public IEnumerable<InterestLogDto> Logs { get; set; } = new List<InterestLogDto>();
        public int TotalCount { get; set; }
    }
}

