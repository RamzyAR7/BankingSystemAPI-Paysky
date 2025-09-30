using System.Collections.Generic;

namespace BankingSystemAPI.Presentation.Helpers
{
    public class ErrorDetails
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public IDictionary<string, string[]?>? Details { get; set; }
    }
}
