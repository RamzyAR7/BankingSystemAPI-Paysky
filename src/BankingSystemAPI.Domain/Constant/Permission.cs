using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Constant
{
    public static class Permission
    {
        public static class User
        {
            public const string Create = "Permission.User.Create";
            public const string Update = "Permission.User.Update";
            public const string Delete = "Permission.User.Delete";
            public const string ReadAll = "Permission.User.ReadAll";
            public const string ReadById = "Permission.User.ReadById";
            public const string ReadByUsername = "Permission.User.ReadByUsername";
            public const string ChangePassword = "Permission.User.ChangePassword";
            public const string DeleteRange = "Permission.User.DeleteRange";
            public const string ReadSelf = "Permission.User.ReadSelf";
        }
        public static class UserRoles
        {
            public const string Assign = "Permission.UserRoles.Assign";
        }
        public static class Role
        {
            public const string Create = "Permission.Role.Create";
            public const string Delete = "Permission.Role.Delete";
            public const string ReadAll = "Permission.Role.ReadAll";
        }
        public static class RoleClaims
        {
            public const string Assign = "Permission.RoleClaims.Assign";
            public const string ReadAll = "Permission.RoleClaims.ReadAll";
        }
        public static class Auth
        {
            public const string RevokeToken = "Permission.Auth.RevokeToken";
        }

        public static class Account
        {
            public const string ReadById = "Permission.Account.ReadById";
            public const string ReadByAccountNumber = "Permission.Account.ReadByAccountNumber";
            public const string ReadByUserId = "Permission.Account.ReadByUserId";
            public const string ReadByNationalId = "Permission.Account.ReadByNationalId";
            public const string Delete = "Permission.Account.Delete";
            public const string DeleteMany = "Permission.Account.DeleteMany";
        }
        public static class CheckingAccount
        {
            public const string Create = "Permission.Checking.Create";
            public const string Update = "Permission.Checking.Update";
            public const string ReadAll = "Permission.Checking.ReadAll";
        }
        public static class SavingsAccount
        {
            public const string Create = "Permission.Savings.Create";
            public const string Update = "Permission.Savings.Update";
            public const string ReadAll = "Permission.Savings.ReadAll";
            public const string ReadAllInterestRate = "Permission.Savings.ReadAllInterestRate";
            public const string ReadInterestRateById = "Permission.Savings.ReadInterestRateById";
        }
        public static class Currency
        {
            public const string Create = "Permission.Currency.Create";
            public const string Update = "Permission.Currency.Update";
            public const string Delete = "Permission.Currency.Delete";
            public const string ReadAll = "Permission.Currency.ReadAll";
            public const string ReadById = "Permission.Currency.ReadById";
        }
        public static class Transaction
        {
            public const string ReadBalance = "Permission.Transaction.ReadBalance";
            public const string Deposit = "Permission.Transaction.Deposit";
            public const string Withdraw = "Permission.Transaction.Withdraw";
            public const string Transfer = "Permission.Transaction.Transfer";
            public const string ReadAllHistory = "Permission.Transaction.ReadAllHistory";
            public const string ReadById = "Permission.Transaction.ReadById";
        }

        public static class Bank
        {
            public const string Create = "Permission.Bank.Create";
            public const string Update = "Permission.Bank.Update";
            public const string Delete = "Permission.Bank.Delete";
            public const string ReadAll = "Permission.Bank.ReadAll";
            public const string ReadById = "Permission.Bank.ReadById";
            public const string ReadByName = "Permission.Bank.ReadByName";
            public const string SetActive = "Permission.Bank.SetActive";
        }

    }
}
