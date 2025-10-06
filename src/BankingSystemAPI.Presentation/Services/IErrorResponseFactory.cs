using System.Collections.Generic;
using BankingSystemAPI.Domain.Common;

namespace BankingSystemAPI.Presentation.Services
{
    public interface IErrorResponseFactory
    {
        (int StatusCode, object Body) Create(IReadOnlyList<ResultError> errors);
    }
}
