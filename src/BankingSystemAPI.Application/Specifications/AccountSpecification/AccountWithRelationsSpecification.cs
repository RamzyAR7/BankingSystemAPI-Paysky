using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public static class AccountWithRelationsSpec
    {
        public static AccountsByIdsSpecification ByIds(IEnumerable<int> ids)
            => new AccountsByIdsSpecification(ids);

        public static AccountByIdSpecification ById(int id)
            => new AccountByIdSpecification(id);

        public static AccountByAccountNumberSpecification ByAccountNumber(string accountNumber)
            => new AccountByAccountNumberSpecification(accountNumber);

        public static AccountsByUserIdSpecification ByUserId(string userId)
            => new AccountsByUserIdSpecification(userId);

        public static AccountsByNationalIdSpecification ByNationalId(string nationalId)
            => new AccountsByNationalIdSpecification(nationalId);
    }
}
