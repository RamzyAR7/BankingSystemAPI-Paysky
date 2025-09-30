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
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw
{
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

                    await _uow.ReloadTrackedEntitiesAsync();
                }
            }
        }

        public async Task<Result<TransactionResDto>> Handle(WithdrawCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            var spec = new AccountByIdSpecification(req.AccountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result<TransactionResDto>.Failure(new[] { "Account not found." });
            if (!account.IsActive) return Result<TransactionResDto>.Failure(new[] { "Account is inactive." });

            // Explicit validations: ensure account's user is active and user's bank is active (if applicable)
            if (account.User == null || !account.User.IsActive)
                return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction for inactive user." });

            if (account.User.BankId.HasValue)
            {
                var bank = await _uow.BankRepository.GetByIdAsync(account.User.BankId.Value);
                if (bank != null && !bank.IsActive)
                    return Result<TransactionResDto>.Failure(new[] { "Cannot perform transaction: user's bank is inactive." });
            }

            // Authorization
            if (_accountAuth is not null)
                await _accountAuth.CanModifyAccountAsync(req.AccountId, AccountModificationOperation.Withdraw);

            // Use tracked account before checks that depend on balances for concurrency correctness
            var trackedAccount = await _uow.AccountRepository.GetByIdAsync(account.Id);

            if (trackedAccount is SavingsAccount && trackedAccount.Balance < req.Amount) return Result<TransactionResDto>.Failure(new[] { "Insufficient funds." });
            if (trackedAccount is CheckingAccount chk && trackedAccount.Balance - req.Amount < -chk.OverdraftLimit) return Result<TransactionResDto>.Failure(new[] { "Insufficient funds." });

            Transaction trx = null;

            await ExecuteWithRetryAsync(async () =>
            {
                trx = new Transaction { Timestamp = DateTime.UtcNow, TransactionType = TransactionType.Withdraw };
                var at = new AccountTransaction { AccountId = trackedAccount.Id, Transaction = trx, TransactionCurrency = trackedAccount.Currency?.Code ?? string.Empty, Amount = Math.Round(req.Amount, 2), Role = TransactionRole.Source, Fees = 0m };
                trx.AccountTransactions = new List<AccountTransaction> { at };

                await _uow.TransactionRepository.AddAsync(trx);
                trackedAccount.Balance = Math.Round(trackedAccount.Balance - req.Amount, 2);
                await _uow.AccountRepository.UpdateAsync(trackedAccount);
                await _uow.SaveAsync();
            });

            var dto = _mapper.Map<TransactionResDto>(trx);
            return Result<TransactionResDto>.Success(dto);
        }
    }
}
