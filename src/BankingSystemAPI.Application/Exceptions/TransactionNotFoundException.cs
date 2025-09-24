using System;

namespace BankingSystemAPI.Application.Exceptions
{
    public class TransactionNotFoundException : NotFoundException
    {
        public TransactionNotFoundException(string message) : base(message)
        {
        }
    }
}
