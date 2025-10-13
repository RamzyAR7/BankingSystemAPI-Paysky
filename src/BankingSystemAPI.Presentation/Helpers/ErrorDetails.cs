#region Usings
using System;
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Presentation.Helpers
{
    public class ErrorDetails
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public IDictionary<string, string[]?>? Details { get; set; }
    }
}

