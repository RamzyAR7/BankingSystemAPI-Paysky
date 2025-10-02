using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
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
    /// Optimized transfer command handler with reduced database queries and better performance
    /// </summary>
    public class TransferCommandHandler : ICommandHandler<TransferCommand, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService? _transactionAuth;
        private readonly IAccountAuthorizationService? _accountAuth;
        private readonly ITransactionHelperService _helper;
        private const decimal SameCurrencyFeeRate = 0.005m;
        private const decimal DifferentCurrencyFeeRate = 0.01m;

        public TransferCommandHandler(IUnitOfWork uow, IMapper mapper, ITransactionHelperService helper, 
            ITransactionAuthorizationService? transactionAuth = null, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
            _accountAuth = accountAuth;
            _helper = helper;
        }

        public async Task<Result<TransactionResDto>> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Early validation
            if (req.Amount <= 0m)
                return Result<TransactionResDto>.Failure(new[] { "Invalid amount." });

            if (req.SourceAccountId == req.TargetAccountId)
                return Result<TransactionResDto>.Failure(new[] { "Source and target accounts must differ." });

            // OPTIMIZATION: Fetch both accounts in a single optimized query
            var accountIds = new[] { req.SourceAccountId, req.TargetAccountId };
            var accounts = await _uow.AccountRepository.ListAsync(
                new BankingSystemAPI.Application.Specifications.AccountSpecification.AccountsByIdsSpecification(accountIds));
            
            var accountsList = accounts.ToList();
            var src = accountsList.FirstOrDefault(a => a.Id == req.SourceAccountId);
            var tgt = accountsList.FirstOrDefault(a => a.Id == req.TargetAccountId);

            if (src == null) return Result<TransactionResDto>.Failure(new[] { "Source account not found." });
            if (tgt == null) return Result<TransactionResDto>.Failure(new[] { "Target account not found." });

            // OPTIMIZATION: Combined validation in single check
            var validationErrors = ValidateAccountsAndUsers(src, tgt);
            if (validationErrors.Any())
                return Result<TransactionResDto>.Failure(validationErrors);

            // OPTIMIZATION: Batch bank validation if needed
            var bankValidationErrors = await ValidateBanksAsync(src, tgt);
            if (bankValidationErrors.Any())
                return Result<TransactionResDto>.Failure(bankValidationErrors);

            // Authorization checks
            if (_transactionAuth != null)
                await _transactionAuth.CanInitiateTransferAsync(req.SourceAccountId, req.TargetAccountId);

            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(req.SourceAccountId, AccountModificationOperation.Withdraw);

            // Business logic validation
            decimal available = src.Balance;
            if (src is CheckingAccount sc) available += sc.OverdraftLimit;
            if (available < req.Amount) 
                return Result<TransactionResDto>.Failure(new[] { "Insufficient funds." });

            // Execute transfer with transaction handling
            await _uow.BeginTransactionAsync();
            
            try
            {
                var trx = await ExecuteTransferLogicAsync(req, src, tgt);
                
                // EF Core automatically:
                // - Checks RowVersion in WHERE clause for both accounts
                // - Throws DbUpdateConcurrencyException if any conflict occurs
                // - Updates RowVersion on successful commits
                await _uow.CommitAsync();

                var dto = _mapper.Map<TransactionResDto>(trx);
                return Result<TransactionResDto>.Success(dto);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        #region Helper Methods

        private List<string> ValidateAccountsAndUsers(Account src, Account tgt)
        {
            var errors = new List<string>();

            if (!src.IsActive || !tgt.IsActive)
                errors.Add("Source or target account is inactive.");

            if (src.User == null || !src.User.IsActive)
                errors.Add("Cannot perform transaction for inactive source user.");

            if (tgt.User == null || !tgt.User.IsActive)
                errors.Add("Cannot perform transaction for inactive target user.");

            return errors;
        }

        private async Task<List<string>> ValidateBanksAsync(Account src, Account tgt)
        {
            var errors = new List<string>();
            var bankIds = new List<int>();

            if (src.User.BankId.HasValue) bankIds.Add(src.User.BankId.Value);
            if (tgt.User.BankId.HasValue && tgt.User.BankId != src.User.BankId) bankIds.Add(tgt.User.BankId.Value);

            if (bankIds.Any())
            {
                foreach (var bankId in bankIds.Distinct())
                {
                    var bank = await _uow.BankRepository.GetByIdAsync(bankId);
                    if (bank != null && !bank.IsActive)
                    {
                        if (bankId == src.User.BankId)
                            errors.Add("Cannot perform transaction: source user's bank is inactive.");
                        if (bankId == tgt.User.BankId)
                            errors.Add("Cannot perform transaction: target user's bank is inactive.");
                    }
                }
            }

            return errors;
        }

        private async Task<Transaction> ExecuteTransferLogicAsync(TransferReqDto req, Account src, Account tgt)
        {
            // Get fresh tracked instances for the transaction
            var trackedSrc = await _uow.AccountRepository.GetByIdAsync(src.Id);
            var trackedTgt = await _uow.AccountRepository.GetByIdAsync(tgt.Id);

            // Calculate currency conversion and fees
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

            // Create transaction with account transactions
            var transaction = new Transaction
            {
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                AccountTransactions = new List<AccountTransaction>
                {
                    new AccountTransaction
                    {
                        AccountId = trackedSrc.Id,
                        TransactionCurrency = trackedSrc.Currency?.Code ?? string.Empty,
                        Amount = Math.Round(req.Amount, 2),
                        Role = TransactionRole.Source,
                        Fees = feeOnSource
                    },
                    new AccountTransaction
                    {
                        AccountId = trackedTgt.Id,
                        TransactionCurrency = trackedTgt.Currency?.Code ?? string.Empty,
                        Amount = targetAmount,
                        Role = TransactionRole.Target,
                        Fees = 0m
                    }
                }
            };

            // Update balances
            trackedSrc.Balance = Math.Round(trackedSrc.Balance - req.Amount - feeOnSource, 2);
            trackedTgt.Balance = Math.Round(trackedTgt.Balance + targetAmount, 2);

            // Persist changes - EF Core will automatically handle RowVersion for both accounts
            await _uow.TransactionRepository.AddAsync(transaction);
            await _uow.AccountRepository.UpdateAsync(trackedSrc);
            await _uow.AccountRepository.UpdateAsync(trackedTgt);
            await _uow.SaveAsync();

            return transaction;
        }

        #endregion
    }
}
