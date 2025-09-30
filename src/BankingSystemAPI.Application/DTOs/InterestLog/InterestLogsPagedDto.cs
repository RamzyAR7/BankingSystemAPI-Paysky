using System.Collections.Generic;

namespace BankingSystemAPI.Application.DTOs.InterestLog
{
    public class InterestLogsPagedDto
    {
        public IEnumerable<InterestLogDto> Logs { get; set; } = new List<InterestLogDto>();
        public int TotalCount { get; set; }
    }
}
