using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Constant
{
    /// <summary>
    /// Strongly-typed error category for Result pattern
    /// </summary>
    public enum ErrorType
    {
        Validation,
        Conflict,
        Unauthorized,
        Forbidden,
        NotFound,
        BusinessRule,
        Unknown
    }
}
