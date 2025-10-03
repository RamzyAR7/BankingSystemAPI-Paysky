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
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Deposit
{
    /// <summary>
    /// Handles deposit transactions. Creates a Transaction entity with an AccountTransaction
    /// (TransactionCurrency is set from the account), persists the transaction and updates
    /// the account balance within a retry wrapper to handle concurrency conflicts.
    /// </summary>
    public class DepositCommandHandler : ICommandHandler<DepositCommand, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<DepositCommandHandler> _logger;
        private const int MaxRetryCount = 3;
        private readonly IAccountAuthorizationService? _accountAuth;

        public DepositCommandHandler(IUnitOfWork uow, IMapper mapper, ILogger<DepositCommandHandler> logger, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
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

        /// <summary>
        /// Handles the deposit command using ResultExtensions for cleaner validation flow.
        /// </summary>
        public async Task<Result<TransactionResDto>> Handle(DepositCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Validate amount using functional approach
            if (req.Amount <= 0m)
                return Result<TransactionResDto>.BadRequest("Invalid amount.");

            // Chain validations using ResultExtensions
            var accountResult = await ValidateAccountAsync(req.AccountId);
            if (accountResult.IsFailure)
                return Result<TransactionResDto>.Failure(accountResult.Errors);

            var stateResult = ValidateAccountState(accountResult.Value!);
            if (stateResult.IsFailure)
                return Result<TransactionResDto>.Failure(stateResult.Errors);

            var bankResult = await ValidateBankAsync(stateResult.Value!);
            if (bankResult.IsFailure)
                return Result<TransactionResDto>.Failure(bankResult.Errors);

            var account = bankResult.Value!;
            var executionResult = await ExecuteDepositAsync(account, req);

            // Add side effects without changing the return type
            if (executionResult.IsSuccess)
            {
                _logger.LogInformation("Deposit successful. Account: {AccountId}, Amount: {Amount}", 
                    req.AccountId, req.Amount);
            }
            else
            {
                _logger.LogError("Deposit failed. Account: {AccountId}, Amount: {Amount}, Errors: {Errors}",
                    req.AccountId, req.Amount, string.Join(", ", executionResult.Errors));
            }

            return executionResult;
        }

        private async Task<Result<Account>> ValidateAccountAsync(int accountId)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            return account.ToResult($"Account with ID '{accountId}' not found.");
        }

        private Result<Account> ValidateAccountState(Account account)
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

        private async Task<Result<TransactionResDto>> ExecuteDepositAsync(Account account, DepositReqDto req)
        {
            // Authorization
            if (_accountAuth is not null)
                await _accountAuth.CanModifyAccountAsync(req.AccountId, AccountModificationOperation.Deposit);

            Transaction trx = null;

            // Execute deposit with retry mechanism
            await ExecuteWithRetryAsync(async () =>
            {
                // Load fresh tracked instance for the operation
                var trackedAccount = await _uow.AccountRepository.GetByIdAsync(account.Id);
                if (trackedAccount == null)
                    throw new InvalidOperationException("Account not found during transaction execution.");

                // Create transaction record
                trx = new Transaction 
                { 
                    Timestamp = DateTime.UtcNow, 
                    TransactionType = TransactionType.Deposit 
                };

                var accountTransaction = new AccountTransaction 
                { 
                    AccountId = trackedAccount.Id, 
                    Transaction = trx, 
                    TransactionCurrency = trackedAccount.Currency?.Code ?? string.Empty, 
                    Amount = Math.Round(req.Amount, 2), 
                    Role = TransactionRole.Target, 
                    Fees = 0m 
                };
                
                trx.AccountTransactions = new List<AccountTransaction> { accountTransaction };

                // Use domain method for deposit (includes business logic and rounding)
                try
                {
                    trackedAccount.Deposit(req.Amount);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException($"Deposit failed: {ex.Message}");
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
