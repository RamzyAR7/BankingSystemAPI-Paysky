using Microsoft.AspNetCore.Http;

namespace BankingSystemAPI.Presentation.Services
{
    public interface ISuccessMessageProvider
    {
        string GetSuccessMessage(string httpMethod, string controller, string action, IQueryCollection? query = null);
    }
}
