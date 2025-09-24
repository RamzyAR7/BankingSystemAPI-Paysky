using System;

namespace BankingSystemAPI.Application.Exceptions
{
    public class AccountNumberNotFoundException : NotFoundException
    {
        public AccountNumberNotFoundException(string message) : base(message)
        {
        }
    }
}
