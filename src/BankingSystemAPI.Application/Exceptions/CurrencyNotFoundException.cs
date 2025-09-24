using System;

namespace BankingSystemAPI.Application.Exceptions
{
    public class CurrencyNotFoundException : NotFoundException
    {
        public CurrencyNotFoundException(string message) : base(message)
        {
        }
    }
}
