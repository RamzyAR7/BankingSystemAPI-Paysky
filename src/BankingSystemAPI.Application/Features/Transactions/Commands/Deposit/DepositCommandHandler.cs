using BankingSystemAPI.Domain.Common;
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
        private const int MaxRetryCount = 3;
        private readonly IAccountAuthorizationService? _accountAuth;

        public DepositCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
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

        /// <summary>
        /// Handles the deposit command.
        /// </summary>
        public async Task<Result<TransactionResDto>> Handle(DepositCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Validate amount (now using enhanced validation)
            if (req.Amount <= 0m)
                return Result<TransactionResDto>.Failure(new[] { "Invalid amount." });

            var spec = new AccountByIdSpecification(req.AccountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) 
                return Result<TransactionResDto>.Failure(new[] { "Account not found." });

            // Use enhanced validation method
            if (!account.CanPerformTransactions())
                return Result<TransactionResDto>.Failure(new[] { "Account or user is inactive." });

            // Bank validation (if applicable)
            if (account.User.BankId.HasValue)
            {
                var bank = await _uow.BankRepository.GetByIdAsync(account.User.BankId.Value);
                if (bank != null && !bank.IsActive)
                    return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction: user's bank is inactive." });
            }

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
