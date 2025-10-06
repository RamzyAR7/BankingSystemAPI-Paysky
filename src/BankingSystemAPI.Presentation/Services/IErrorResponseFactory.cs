using System.Collections.Generic;

namespace BankingSystemAPI.Presentation.Services
{
    public interface IErrorResponseFactory
    {
        // Returns status code and response body object
        (int StatusCode, object Body) Create(IReadOnlyList<string> errors);
    }
}
