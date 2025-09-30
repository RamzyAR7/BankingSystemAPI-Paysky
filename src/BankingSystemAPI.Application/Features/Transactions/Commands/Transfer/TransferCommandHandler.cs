using BankingSystemAPI.Application.Common;
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
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Transfer
{
    /// <summary>
    /// Handles transfer transactions between accounts. Performs currency conversion when needed,
    /// computes fees and applies balance updates to both accounts within a unit-of-work transaction.
    /// AccountTransaction entries include the TransactionCurrency and fee amounts.
    /// </summary>
    public class TransferCommandHandler : ICommandHandler<TransferCommand, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService? _transactionAuth;
        private readonly IAccountAuthorizationService? _accountAuth;
        private readonly ITransactionHelperService _helper;
        private const int MaxRetryCount = 3;
        private const decimal SameCurrencyFeeRate = 0.005m;
        private const decimal DifferentCurrencyFeeRate = 0.01m;

        public TransferCommandHandler(IUnitOfWork uow, IMapper mapper, ITransactionHelperService helper, ITransactionAuthorizationService? transactionAuth = null, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
            _accountAuth = accountAuth;
            _helper = helper;
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

                    await _uow.ReloadTrackedEntitiesAsync();
                }
            }
        }

        /// <summary>
        /// Handles the transfer command.
        /// </summary>
        public async Task<Result<TransactionResDto>> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Validate amount
            if (req.Amount <= 0m)
                return Result<TransactionResDto>.Failure(new[] { "Invalid amount." });

            // Prevent transfers to the same account
            if (req.SourceAccountId == req.TargetAccountId)
                return Result<TransactionResDto>.Failure(new[] { "Source and target accounts must differ." });

            var specSrc = new AccountByIdSpecification(req.SourceAccountId);
            var src = await _uow.AccountRepository.FindAsync(specSrc);
            if (src == null) return Result<TransactionResDto>.Failure(new[] { "Source account not found." });
            var specTgt = new AccountByIdSpecification(req.TargetAccountId);
            var tgt = await _uow.AccountRepository.FindAsync(specTgt);
            if (tgt == null) return Result<TransactionResDto>.Failure(new[] { "Target account not found." });

            if (!src.IsActive || !tgt.IsActive) return Result<TransactionResDto>.Failure(new[] { "Source or target account is inactive." });

            // Explicit validations: ensure source and target users are active and their banks (if any) are active
            if (src.User == null || !src.User.IsActive) return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction for inactive source user." });
            if (tgt.User == null || !tgt.User.IsActive) return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction for inactive target user." });

            if (src.User.BankId.HasValue)
            {
                var sBank = await _uow.BankRepository.GetByIdAsync(src.User.BankId.Value);
                if (sBank != null && !sBank.IsActive)
                    return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction: source user's bank is inactive." });
            }

            if (tgt.User.BankId.HasValue)
            {
                var tBank = await _uow.BankRepository.GetByIdAsync(tgt.User.BankId.Value);
                if (tBank != null && !tBank.IsActive)
                    return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction: target user's bank is inactive." });
            }

            // Use tracked instances for checks and updates
            var trackedSrc = await _uow.AccountRepository.GetByIdAsync(src.Id);
            var trackedTgt = await _uow.AccountRepository.GetByIdAsync(tgt.Id);

            if (trackedSrc == null || trackedTgt == null)
                return Result<TransactionResDto>.Failure(new[] { "Source or target account not found." });

            // compute available balance considering overdraft for checking
            decimal available = trackedSrc.Balance;
            if (trackedSrc is CheckingAccount sc) available += sc.OverdraftLimit;
            if (available < req.Amount) return Result<TransactionResDto>.Failure(new[] { "Insufficient funds." });

            if (_transactionAuth != null)
            {
                var task = _transactionAuth.CanInitiateTransferAsync(req.SourceAccountId, req.TargetAccountId);
                if (task != null) await task;
            }

            // Account-level authorization for source (withdraw)
            if (_accountAuth is not null)
            {
                await _accountAuth.CanModifyAccountAsync(req.SourceAccountId, AccountModificationOperation.Withdraw);
            }

            Transaction trx = null;

            await ExecuteWithRetryAsync(async () =>
            {
                // determine currencies and conversion
                var sourceCurrencyId = trackedSrc.CurrencyId;
                var targetCurrencyId = trackedTgt.CurrencyId;

                decimal targetAmount = req.Amount;
                bool differentCurrency = sourceCurrencyId != targetCurrencyId;
                if (differentCurrency)
                {
                    targetAmount = await _helper.ConvertAsync(sourceCurrencyId, targetCurrencyId, req.Amount);
                }

                var feeRate = differentCurrency ? DifferentCurrencyFeeRate : SameCurrencyFeeRate;
                var feeOnSource = Math.Round(req.Amount * feeRate, 2);

                targetAmount = Math.Round(targetAmount, 2);

                var srcAccTrx = new AccountTransaction
                {
                    AccountId = trackedSrc.Id,
                    TransactionCurrency = trackedSrc.Currency?.Code ?? string.Empty,
                    Amount = Math.Round(req.Amount, 2),
                    Role = TransactionRole.Source,
                    Fees = feeOnSource
                };

                var tgtAccTrx = new AccountTransaction
                {
                    AccountId = trackedTgt.Id,
                    TransactionCurrency = trackedTgt.Currency?.Code ?? string.Empty,
                    Amount = targetAmount,
                    Role = TransactionRole.Target,
                    Fees = 0m
                };

                var trxLocal = new Transaction { Timestamp = DateTime.UtcNow, TransactionType = TransactionType.Transfer, AccountTransactions = new List<AccountTransaction> { srcAccTrx, tgtAccTrx } };

                await _uow.BeginTransactionAsync();
                try
                {
                    // apply balances
                    trackedSrc.Balance = Math.Round(trackedSrc.Balance - req.Amount - feeOnSource, 2);
                    trackedTgt.Balance = Math.Round(trackedTgt.Balance + targetAmount, 2);

                    await _uow.AccountRepository.UpdateAsync(trackedSrc);
                    await _uow.AccountRepository.UpdateAsync(trackedTgt);

                    await _uow.TransactionRepository.AddAsync(trxLocal);

                    // When using in-memory no-op transaction, CommitAsync will call SaveChanges.
                    await _uow.CommitAsync();
                }
                catch
                {
                    await _uow.RollbackAsync();
                    throw;
                }

                trx = trxLocal;
            });

            var dto = _mapper.Map<TransactionResDto>(trx);
            return Result<TransactionResDto>.Success(dto);
        }
    }
}
