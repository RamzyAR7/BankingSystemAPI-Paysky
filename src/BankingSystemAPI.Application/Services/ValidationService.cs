using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Services;

namespace BankingSystemAPI.Application.Services
{
    /// <summary>
    /// Common validation service to eliminate duplication across handlers
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ValidationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Currency>> ValidateCurrencyAsync(int currencyId, bool mustBeActive = true)
        {
            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(currencyId);
            if (currency == null)
                return Result<Currency>.NotFound("Currency", currencyId);

            if (mustBeActive && !currency.IsActive)
                return Result<Currency>.BadRequest("Cannot use inactive currency.");

            return Result<Currency>.Success(currency);
        }

        public async Task<Result<ApplicationUser>> ValidateUserAsync(string userId, bool mustBeActive = true)
        {
            var user = await _unitOfWork.UserRepository.FindAsync(new UserByIdSpecification(userId));
            if (user == null)
                return Result<ApplicationUser>.NotFound("User", userId);

            if (mustBeActive && !user.IsActive)
                return Result<ApplicationUser>.BadRequest("Cannot use inactive user.");

            return Result<ApplicationUser>.Success(user);
        }

        public async Task<Result<Bank>> ValidateBankAsync(int bankId, bool mustBeActive = true)
        {
            var bank = await _unitOfWork.BankRepository.GetByIdAsync(bankId);
            if (bank == null)
                return Result<Bank>.NotFound("Bank", bankId);

            if (mustBeActive && !bank.IsActive)
                return Result<Bank>.BadRequest("Cannot use inactive bank.");

            return Result<Bank>.Success(bank);
        }

        public async Task<Result<Account>> ValidateAccountAsync(int accountId, bool mustBeActive = true)
        {
            var account = await _unitOfWork.AccountRepository.FindAsync(new AccountByIdSpecification(accountId));
            if (account == null)
                return Result<Account>.NotFound("Account", accountId);

            if (mustBeActive && !account.IsActive)
                return Result<Account>.BadRequest("Account is inactive.");

            return Result<Account>.Success(account);
        }

        public async Task<Result> ValidateUserBankStatusAsync(ApplicationUser user)
        {
            if (user.BankId == null)
                return Result.Success(); // User doesn't belong to a bank, which is valid in some cases

            var bankResult = await ValidateBankAsync(user.BankId.Value, true);
            if (bankResult.IsFailure)
                return Result.BadRequest("User's bank is inactive or not found.");

            return Result.Success();
        }

        public async Task<Result> ValidateAccountOwnershipAsync(int accountId, string userId)
        {
            var account = await _unitOfWork.AccountRepository.FindAsync(new AccountByIdSpecification(accountId));
            if (account == null)
                return Result.NotFound("Account", accountId);

            if (account.UserId != userId)
                return Result.Forbidden("Access denied: User does not own this account.");

            return Result.Success();
        }

        public async Task<Result> ValidateCurrencyForTransactionAsync(int currencyId)
        {
            var currencyResult = await ValidateCurrencyAsync(currencyId, true);
            if (currencyResult.IsFailure)
                return Result.BadRequest("Currency is inactive or not found.");

            return Result.Success();
        }

        public async Task<Result> ValidateUserRoleForAccountCreationAsync(string userId)
        {
            var userResult = await ValidateUserAsync(userId, true);
            if (userResult.IsFailure)
                return Result.BadRequest("User not found or inactive.");

            var user = userResult.Value!;
            
            // Additional role-based validation can be added here
            // For now, just check if user is active and has required properties
            if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Email))
                return Result.BadRequest("User profile is incomplete for account creation.");

            return Result.Success();
        }

        public async Task<Result> ValidateAccountBalanceForDeletionAsync(Account account)
        {
            if (account.Balance > 0)
                return Result.BadRequest("Cannot delete account with positive balance. Please withdraw all funds first.");

            if (account.Balance < 0)
                return Result.BadRequest("Cannot delete account with negative balance. Please resolve the debt first.");

            return Result.Success();
        }

        public async Task<Result> ValidateAccountBalanceForWithdrawalAsync(Account account, decimal amount)
        {
            if (amount <= 0)
                return Result.BadRequest("Withdrawal amount must be greater than zero.");

            var availableBalance = account.GetAvailableBalance();
            if (amount > availableBalance)
                return Result.BadRequest($"Insufficient funds. Available balance: {availableBalance:C}, Requested: {amount:C}");

            return Result.Success();
        }

        public string GenerateAccountNumber(string prefix)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        /// <summary>
        /// Validates multiple entities and returns a combined result
        /// </summary>
        public async Task<Result> ValidateTransferEntitiesAsync(int sourceAccountId, int targetAccountId)
        {
            var sourceResult = await ValidateAccountAsync(sourceAccountId);
            var targetResult = await ValidateAccountAsync(targetAccountId);
            
            return Result.Combine(sourceResult, targetResult);
        }

        /// <summary>
        /// Validates business rules for a transfer operation
        /// </summary>
        public Result ValidateTransferBusinessRules(Account sourceAccount, decimal amount)
        {
            if (amount <= 0)
                return Result.BadRequest("Transfer amount must be greater than zero.");

            var availableBalance = sourceAccount.GetAvailableBalance();
            if (amount > availableBalance)
                return Result.BadRequest("Insufficient funds for transfer.");

            return Result.Success();
        }
    }
}