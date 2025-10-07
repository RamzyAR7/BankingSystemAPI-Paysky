#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using AutoMapper;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.Authorization;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Commands.Transfer
{
    /// <summary>
    /// Optimized transfer command handler with ResultExtensions for better error handling and functional patterns
    /// </summary>
    public class TransferCommandHandler : ICommandHandler<TransferCommand, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService _transactionAuth;
        private readonly IAccountAuthorizationService _accountAuth;
        private readonly ITransactionHelperService _helper;
        private readonly ILogger<TransferCommandHandler> _logger;
        private const decimal SameCurrencyFeeRate = 0.005m;     // 0.5% fee for same-currency transfers
        private const decimal DifferentCurrencyFeeRate = 0.01m;  // 1% fee for cross-currency transfers

        public TransferCommandHandler(IUnitOfWork uow, IMapper mapper, ITransactionHelperService helper, 
            ITransactionAuthorizationService transactionAuth, IAccountAuthorizationService accountAuth,
            ILogger<TransferCommandHandler>? logger = null)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
            _accountAuth = accountAuth;
            _helper = helper;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TransferCommandHandler>.Instance;
        }

        public async Task<Result<TransactionResDto>> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Sequential validation using ResultExtensions
            var basicValidationResult = ValidateBasicInput(req);
            if (!basicValidationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(basicValidationResult.ErrorItems);

            var accountsResult = await LoadAccountsAsync(req);
            if (!accountsResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(accountsResult.ErrorItems);

            var accountValidationResult = ValidateAccounts(accountsResult.Value!);
            if (!accountValidationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(accountValidationResult.ErrorItems);

            var bankValidationResult = await ValidateBanksAsync(accountsResult.Value!);
            if (!bankValidationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(bankValidationResult.ErrorItems);

            var authorizationResult = await ValidateAuthorizationAsync(accountsResult.Value!.Source, accountsResult.Value.Target, req);
            if (!authorizationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(authorizationResult.ErrorItems);

            // Validate balance with accurate fee calculation
            var balanceResult = await ValidateBalanceWithFeesAsync(accountsResult.Value!.Source, accountsResult.Value.Target, req.Amount);
            if (!balanceResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(balanceResult.ErrorItems);

            var transferResult = await ExecuteTransferAsync(accountsResult.Value!, req);

            // Add side effects using ResultExtensions
            transferResult.OnSuccess(() => 
            {
                _logger.LogInformation(ApiResponseMessages.Logging.TransactionTransferSuccess,
                    req.SourceAccountId, req.TargetAccountId, req.Amount);
            })
            .OnFailure(errors => 
            {
                // Result.OnFailure supplies IReadOnlyList<string>
                _logger.LogWarning(ApiResponseMessages.Logging.TransactionTransferFailed,
                    req.SourceAccountId, req.TargetAccountId, req.Amount, string.Join(", ", errors));
            });

            return transferResult;
        }

        private Result ValidateBasicInput(TransferReqDto req)
        {
            var validations = new[]
            {
                req.Amount <= 0m ? Result.ValidationFailed(ApiResponseMessages.Validation.TransferAmountGreaterThanZero) : Result.Success(),
                req.SourceAccountId == req.TargetAccountId ? Result.ValidationFailed(ApiResponseMessages.Validation.SourceAndTargetAccountsMustDiffer) : Result.Success()
            };

            return ResultExtensions.ValidateAll(validations);
        }

        private async Task<Result<AccountPair>> LoadAccountsAsync(TransferReqDto req)
        {
            try
            {
                // For transfer operations, we need tracking enabled and Currency navigation loaded
                var accountIds = new[] { req.SourceAccountId, req.TargetAccountId };
                var spec = new Specifications.AccountSpecification.AccountsByIdsWithCurrencySpecification(accountIds);
                
                var accounts = await _uow.AccountRepository.ListAsync(spec);
                
                var accountsList = accounts.ToList();
                var src = accountsList.FirstOrDefault(a => a.Id == req.SourceAccountId);
                var tgt = accountsList.FirstOrDefault(a => a.Id == req.TargetAccountId);

                if (src == null)
                {
                    _logger.LogError(ApiResponseMessages.Logging.TransactionLoadAccountsFailed, req.SourceAccountId, req.TargetAccountId);
                    return Result<AccountPair>.NotFound("Account", req.SourceAccountId);
                }

                if (tgt == null)
                {
                    _logger.LogError(ApiResponseMessages.Logging.TransactionLoadAccountsFailed, req.SourceAccountId, req.TargetAccountId);
                    return Result<AccountPair>.NotFound("Account", req.TargetAccountId);
                }

                return Result<AccountPair>.Success(new AccountPair { Source = src, Target = tgt });
            }
            catch (Exception e)
            {
                _logger.LogError(e, ApiResponseMessages.Logging.TransactionLoadAccountsFailed, req.SourceAccountId, req.TargetAccountId);
                return Result<AccountPair>.BadRequest(string.Format(ApiResponseMessages.Logging.TransactionValidateBanksFailed, e.Message));
            }
        }

        private Result ValidateAccounts(AccountPair accounts)
        {
            var validations = new[]
            {
                !accounts.Source.IsActive 
                    ? Result.AccountInactive(accounts.Source.Id.ToString()) // Maps to 409 Conflict
                    : Result.Success(),
                    
                !accounts.Target.IsActive 
                    ? Result.AccountInactive(accounts.Target.Id.ToString()) // Maps to 409 Conflict
                    : Result.Success(),
                    
                accounts.Source.User?.IsActive != true 
                    ? Result.Conflict(AuthorizationConstants.ErrorMessages.InactiveAccountAccess) // Maps to 409
                    : Result.Success(),
                    
                accounts.Target.User?.IsActive != true 
                    ? Result.Conflict(AuthorizationConstants.ErrorMessages.InactiveAccountAccess) // Maps to 409
                    : Result.Success()
            };

            return ResultExtensions.ValidateAll(validations);
        }

        private async Task<Result> ValidateBanksAsync(AccountPair accounts)
        {
            var bankIds = new List<int>();
            if (accounts.Source.User.BankId.HasValue) bankIds.Add(accounts.Source.User.BankId.Value);
            if (accounts.Target.User.BankId.HasValue && accounts.Target.User.BankId != accounts.Source.User.BankId) 
                bankIds.Add(accounts.Target.User.BankId.Value);

            if (!bankIds.Any())
                return Result.Success();

            try
            {
                // Batch load all required banks in a single query instead of multiple calls
                var distinctBankIds = bankIds.Distinct().ToArray();
                var banks = new Dictionary<int, Domain.Entities.Bank>();
                
                // Load all banks at once
                foreach (var bankId in distinctBankIds)
                {
                    var bank = await _uow.BankRepository.GetByIdAsync(bankId);
                    if (bank != null)
                        banks[bankId] = bank;
                }

                var bankValidations = new List<Result>();
                foreach (var bankId in distinctBankIds)
                {
                    if (banks.TryGetValue(bankId, out var bank) && !bank.IsActive)
                    {
                        if (bankId == accounts.Source.User.BankId)
                            bankValidations.Add(Result.Conflict(string.Format(ApiResponseMessages.Generic.DeactivatedFormat, "Bank")));
                        if (bankId == accounts.Target.User.BankId)
                            bankValidations.Add(Result.Conflict(string.Format(ApiResponseMessages.Generic.DeactivatedFormat, "Bank")));
                    }
                    else if (!banks.ContainsKey(bankId))
                    {
                        bankValidations.Add(Result.BadRequest(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Bank", bankId)));
                    }
                    else
                    {
                        bankValidations.Add(Result.Success());
                    }
                }

                return ResultExtensions.ValidateAll(bankValidations.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e, ApiResponseMessages.Logging.TransactionValidateBanksFailed, e.Message);
                return Result.BadRequest(ApiResponseMessages.Infrastructure.DbGenericError);
            }
        }

        private async Task<Result> ValidateAuthorizationAsync(Account source, Account target, TransferReqDto req)
        {
            try
            {
                var authResult = await _transactionAuth.CanInitiateTransferAsync(req.SourceAccountId, req.TargetAccountId);
                if (!authResult) // Using implicit bool operator!
                    return Result.Failure(authResult.ErrorItems);

                var accountAuthResult = await _accountAuth.CanModifyAccountAsync(req.SourceAccountId, AccountModificationOperation.Withdraw);
                if (!accountAuthResult) // Using implicit bool operator!
                    return Result.Failure(accountAuthResult.ErrorItems);

                return Result.Success();
            }
            catch (Exception)
            {
                return Result.Forbidden(AuthorizationConstants.ErrorMessages.SystemError);
            }
        }

    private Task<Result> ValidateBalanceWithFeesAsync(Account source, Account target, decimal amount)
        {
            // Calculate accurate fees based on currency difference
            var sourceCurrencyId = source.CurrencyId;
            var targetCurrencyId = target.CurrencyId;
            bool differentCurrency = sourceCurrencyId != targetCurrencyId;
            
            var feeRate = differentCurrency ? DifferentCurrencyFeeRate : SameCurrencyFeeRate;
            var fees = Math.Round(amount * feeRate, 2);
            var totalRequired = amount + fees;
            
            // For transfers, NEVER allow overdraft - only use actual balance
            decimal availableBalance = source.Balance;
            
            return Task.FromResult(availableBalance >= totalRequired
                ? Result.Success()
                : Result.InsufficientFunds(totalRequired, availableBalance)); // Maps to 409 Conflict
        }

        private async Task<Result<TransactionResDto>> ExecuteTransferAsync(AccountPair accounts, TransferReqDto req)
        {
            await _uow.BeginTransactionAsync();
            
            try
            {
                var trx = await ExecuteTransferLogicAsync(req, accounts.Source, accounts.Target);
                
                // - Checks RowVersion in WHERE clause for both accounts
                // - Throws DbUpdateConcurrencyException if any conflict occurs
                // - Updates RowVersion on successful commits
                await _uow.CommitAsync();

                var dto = _mapper.Map<TransactionResDto>(trx);
                return Result<TransactionResDto>.Success(dto);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return Result<TransactionResDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private async Task<Transaction> ExecuteTransferLogicAsync(TransferReqDto req, Account src, Account tgt)
        {
            // Use the already loaded and tracked accounts from LoadAccountsAsync
            // They should now have Currency navigation property loaded and be tracked
            
            // Calculate currency conversion and fees
            var sourceCurrencyId = src.CurrencyId;
            var targetCurrencyId = tgt.CurrencyId;
            
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
                        AccountId = src.Id,
                        TransactionCurrency = src.Currency?.Code, // Remove ?? string.Empty to allow null
                        Amount = Math.Round(req.Amount, 2),
                        Role = TransactionRole.Source,
                        Fees = feeOnSource
                    },
                    new AccountTransaction
                    {
                        AccountId = tgt.Id,
                        TransactionCurrency = tgt.Currency?.Code, // Remove ?? string.Empty to allow null
                        Amount = targetAmount,
                        Role = TransactionRole.Target,
                        Fees = 0m
                    }
                }
            };

            // Use domain methods for balance updates to enforce business rules
            // WithdrawForTransfer prevents overdraft usage (no negative balance allowed)
            var totalWithdrawal = req.Amount + feeOnSource;
            
            try
            {
                src.WithdrawForTransfer(totalWithdrawal);  // This enforces no overdraft for transfers
                tgt.Deposit(targetAmount);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(string.Format(ApiResponseMessages.BankingErrors.AccountInactiveFormat, src.Id));
            }

            // Persist changes - EF Core will automatically handle RowVersion for both accounts
            await _uow.TransactionRepository.AddAsync(transaction);
            await _uow.AccountRepository.UpdateAsync(src);
            await _uow.AccountRepository.UpdateAsync(tgt);
            await _uow.SaveAsync();

            return transaction;
        }

        private class AccountPair
        {
            public Account Source { get; set; } = null!;
            public Account Target { get; set; } = null!;
        }
    }
}

