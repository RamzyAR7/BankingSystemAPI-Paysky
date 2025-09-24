using System;

namespace BankingSystemAPI.Application.Exceptions
{
    public class AccountNotFoundException : NotFoundException
    {
        public AccountNotFoundException(string message) : base(message)
        {
        }
    }
}
