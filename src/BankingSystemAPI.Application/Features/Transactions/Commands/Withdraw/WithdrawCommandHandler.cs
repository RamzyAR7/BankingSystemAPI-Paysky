#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Domain.Entities;
using AutoMapper;
using System.Collections.Generic;
using System;
using BankingSystemAPI.Domain.Constant;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Exceptions;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw
{
    /// <summary>
    /// Withdraw command handler with validation for backward compatibility with tests
    /// </summary>
    public class WithdrawCommandHandler : ICommandHandler<WithdrawCommand, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private const int MaxRetryCount = 3;
        private readonly IAccountAuthorizationService _accountAuth;

        public WithdrawCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        private async Task ExecuteWithRetryAsync(Func<Task> operation)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await operation();
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    retryCount++;
                    if (retryCount >= MaxRetryCount)
                        throw new InvalidOperationException(ApiResponseMessages.Infrastructure.ConcurrencyConflict);

                    await _uow.ReloadTrackedEntitiesAsync();
                }
            }
        }

        public async Task<Result<TransactionResDto>> Handle(WithdrawCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            if (req.Amount <= 0m)
                return Result<TransactionResDto>.BadRequest(ApiResponseMessages.Validation.TransferAmountGreaterThanZero);

            // Chain validations using ResultExtensions
            var accountResult = await ValidateAccountAsync(req.AccountId);
            if (accountResult.IsFailure)
                return Result<TransactionResDto>.Failure(accountResult.ErrorItems);

            var stateResult = await ValidateAccountStateAsync(accountResult.Value!);
            if (stateResult.IsFailure)
                return Result<TransactionResDto>.Failure(stateResult.ErrorItems);

            var bankResult = await ValidateBankAsync(stateResult.Value!);
            if (bankResult.IsFailure)
                return Result<TransactionResDto>.Failure(bankResult.ErrorItems);

            var amountResult = ValidateWithdrawalAmount(bankResult.Value!, req.Amount);
            if (amountResult.IsFailure)
                return Result<TransactionResDto>.Failure(amountResult.ErrorItems);

            var account = amountResult.Value!;
            var executionResult = await ExecuteWithdrawalAsync(account, req);

            return executionResult;
        }

        private async Task<Result<Account>> ValidateAccountAsync(int accountId)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            return account.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Account", accountId));
        }

        private async Task<Result<Account>> ValidateAccountStateAsync(Account account)
        {
            return account.CanPerformTransactions()
                ? Result<Account>.Success(account)
                : Result<Account>.BadRequest(ApiResponseMessages.Validation.AccountNotFound);
        }

        private async Task<Result<Account>> ValidateBankAsync(Account account)
        {
            if (!account.User.BankId.HasValue)
                return Result<Account>.Success(account);

            var bank = await _uow.BankRepository.GetByIdAsync(account.User.BankId.Value);
            return bank == null || bank.IsActive
                ? Result<Account>.Success(account)
                : Result<Account>.BadRequest(ApiResponseMessages.Validation.CurrencyInactive);
        }

        private Result<Account> ValidateWithdrawalAmount(Account account, decimal amount)
        {
            // Special handling for checking accounts with overdraft facility
            if (account is CheckingAccount checkingAccount)
            {
                if (!checkingAccount.CanWithdraw(amount))
                {
                    var maxAllowed = checkingAccount.GetMaxWithdrawalAmount();
                    var balance = checkingAccount.Balance;
                    var overdraftAvailable = checkingAccount.GetAvailableOverdraftCredit();
                    
                    // Format balance with minus sign for negative values instead of parentheses
                    var balanceFormatted = balance >= 0 ? $"${balance:F2}" : $"-${Math.Abs(balance):F2}";
                    
                    return Result<Account>.BadRequest(
                        string.Format(ApiResponseMessages.BankingErrors.InsufficientFundsFormat, maxAllowed.ToString("C"), balanceFormatted));
                }
                return Result<Account>.Success(account);
            }
            
            // For other account types (Savings), use balance only - no overdraft
            var availableBalance = account.GetAvailableBalance();
            return amount > availableBalance
                ? Result<Account>.BadRequest(string.Format(ApiResponseMessages.BankingErrors.InsufficientFundsFormat, amount.ToString("C"), availableBalance.ToString("C")))
                : Result<Account>.Success(account);
        }

        private async Task<Result<TransactionResDto>> ExecuteWithdrawalAsync(Account account, WithdrawReqDto req)
        {
            // Authorization
            var authResult = await _accountAuth.CanModifyAccountAsync(req.AccountId, AccountModificationOperation.Withdraw);
            if (authResult.IsFailure)
                return Result<TransactionResDto>.Failure(authResult.ErrorItems);

            Transaction? trx = null;

            try
            {
                await ExecuteWithRetryAsync(async () =>
                {
                    // Load fresh tracked instance with navigation properties using specification
                    var spec = new AccountByIdSpecification(account.Id);
                    var trackedAccount = await _uow.AccountRepository.FindAsync(spec);
                    if (trackedAccount == null)
                        throw new InvalidOperationException(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Account", account.Id));

                    // Create transaction record
                    trx = new Transaction 
                    { 
                        Timestamp = DateTime.UtcNow, 
                        TransactionType = TransactionType.Withdraw 
                    };

                    var accountTransaction = new AccountTransaction 
                    { 
                        AccountId = trackedAccount.Id, 
                        Transaction = trx, 
                        TransactionCurrency = trackedAccount.Currency?.Code, // Remove ?? string.Empty to allow null
                        Amount = Math.Round(req.Amount, 2), 
                        Role = TransactionRole.Source, 
                        Fees = 0m 
                    };
                    
                    trx.AccountTransactions = new List<AccountTransaction> { accountTransaction };

                    // Use domain method for withdrawal (includes business logic and account-specific rules)
                    try
                    {
                        trackedAccount.Withdraw(req.Amount);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Wrap domain error in BusinessRuleException with standardized message
                        throw new BusinessRuleException(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                    }

                    // Persist changes
                    await _uow.TransactionRepository.AddAsync(trx);
                    await _uow.AccountRepository.UpdateAsync(trackedAccount);
                    
                    // - Checks RowVersion in WHERE clause
                    // - Throws DbUpdateConcurrencyException if conflict
                    // - Updates RowVersion on success
                    await _uow.SaveAsync();
                });

                var dto = _mapper.Map<TransactionResDto>(trx!);
                return Result<TransactionResDto>.Success(dto);
            }
            catch (BusinessRuleException ex)
            {
                // Return proper error result using infrastructure template
                return Result<TransactionResDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains(ApiResponseMessages.Infrastructure.ConcurrencyConflict))
            {
                // Handle concurrency conflicts properly using standardized message
                return Result<TransactionResDto>.Conflict(ApiResponseMessages.Infrastructure.ConcurrencyConflict);
            }
        }
    }
}

