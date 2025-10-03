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
        private readonly IAccountAuthorizationService? _accountAuth;

        public WithdrawCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
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
                        throw new InvalidOperationException("Concurrent update detected. Please try again later.");

                    // THIS IS ESSENTIAL - Refreshes RowVersion and other tracked data
                    await _uow.ReloadTrackedEntitiesAsync();
                }
            }
        }

        public async Task<Result<TransactionResDto>> Handle(WithdrawCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Validate amount using functional approach
            if (req.Amount <= 0m)
                return Result<TransactionResDto>.BadRequest("Invalid amount.");

            // Chain validations using ResultExtensions
            var accountResult = await ValidateAccountAsync(req.AccountId);
            if (accountResult.IsFailure)
                return Result<TransactionResDto>.Failure(accountResult.Errors);

            var stateResult = await ValidateAccountStateAsync(accountResult.Value!);
            if (stateResult.IsFailure)
                return Result<TransactionResDto>.Failure(stateResult.Errors);

            var bankResult = await ValidateBankAsync(stateResult.Value!);
            if (bankResult.IsFailure)
                return Result<TransactionResDto>.Failure(bankResult.Errors);

            var amountResult = ValidateWithdrawalAmount(bankResult.Value!, req.Amount);
            if (amountResult.IsFailure)
                return Result<TransactionResDto>.Failure(amountResult.Errors);

            var account = amountResult.Value!;
            var executionResult = await ExecuteWithdrawalAsync(account, req);

            // Add side effects without changing the return type
            if (executionResult.IsSuccess)
            {
                // Could add success logging here
            }
            else
            {
                // Could add error logging here
            }

            return executionResult;
        }

        private async Task<Result<Account>> ValidateAccountAsync(int accountId)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            return account.ToResult($"Account with ID '{accountId}' not found.");
        }

        private async Task<Result<Account>> ValidateAccountStateAsync(Account account)
        {
            return account.CanPerformTransactions()
                ? Result<Account>.Success(account)
                : Result<Account>.BadRequest("Account or user is inactive.");
        }

        private async Task<Result<Account>> ValidateBankAsync(Account account)
        {
            if (!account.User.BankId.HasValue)
                return Result<Account>.Success(account);

            var bank = await _uow.BankRepository.GetByIdAsync(account.User.BankId.Value);
            return bank == null || bank.IsActive
                ? Result<Account>.Success(account)
                : Result<Account>.BadRequest("Cannot perform transaction: user's bank is inactive.");
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
                    
                    return Result<Account>.BadRequest(
                        $"Insufficient funds. Maximum withdrawal: {maxAllowed:C} " +
                        $"(Balance: {balance:C}, Overdraft available: {overdraftAvailable:C})");
                }
                return Result<Account>.Success(account);
            }
            
            // For other account types (Savings), use balance only - no overdraft
            var availableBalance = account.GetAvailableBalance();
            return amount > availableBalance
                ? Result<Account>.BadRequest($"Insufficient funds. Available balance: {availableBalance:C}")
                : Result<Account>.Success(account);
        }

        private async Task<Result<TransactionResDto>> ExecuteWithdrawalAsync(Account account, WithdrawReqDto req)
        {
            // Authorization
            if (_accountAuth is not null)
                await _accountAuth.CanModifyAccountAsync(req.AccountId, AccountModificationOperation.Withdraw);

            Transaction trx = null;

            await ExecuteWithRetryAsync(async () =>
            {
                // Load fresh tracked instance
                var trackedAccount = await _uow.AccountRepository.GetByIdAsync(account.Id);
                if (trackedAccount == null)
                    throw new InvalidOperationException("Account not found during transaction execution.");

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
                    TransactionCurrency = trackedAccount.Currency?.Code ?? string.Empty, 
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
                    throw new InvalidOperationException($"Withdrawal failed: {ex.Message}");
                }

                // Persist changes
                await _uow.TransactionRepository.AddAsync(trx);
                await _uow.AccountRepository.UpdateAsync(trackedAccount);
                
                // EF Core automatically:
                // - Checks RowVersion in WHERE clause
                // - Throws DbUpdateConcurrencyException if conflict
                // - Updates RowVersion on success
                await _uow.SaveAsync();
            });

            var dto = _mapper.Map<TransactionResDto>(trx);
            return Result<TransactionResDto>.Success(dto);
        }
    }
}
