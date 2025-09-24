using System;

namespace BankingSystemAPI.Application.Exceptions
{
    public class InvalidAccountOperationException : Exception
    {
        public InvalidAccountOperationException(string message) : base(message)
        {
        }
    }
}
