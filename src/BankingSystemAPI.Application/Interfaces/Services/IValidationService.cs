using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface IValidationService
    {
        Task<Result<Currency>> ValidateCurrencyAsync(int currencyId, bool mustBeActive = true);
        Task<Result<ApplicationUser>> ValidateUserAsync(string userId, bool mustBeActive = true);
        Task<Result<Bank>> ValidateBankAsync(int bankId, bool mustBeActive = true);
        Task<Result<Account>> ValidateAccountAsync(int accountId, bool mustBeActive = true);
        Task<Result> ValidateUserBankStatusAsync(ApplicationUser user);
        Task<Result> ValidateAccountOwnershipAsync(int accountId, string userId);
        Task<Result> ValidateCurrencyForTransactionAsync(int currencyId);
        Task<Result> ValidateUserRoleForAccountCreationAsync(string userId);
        Task<Result> ValidateAccountBalanceForDeletionAsync(Account account);
        Task<Result> ValidateAccountBalanceForWithdrawalAsync(Account account, decimal amount);
        string GenerateAccountNumber(string prefix);
    }
}
