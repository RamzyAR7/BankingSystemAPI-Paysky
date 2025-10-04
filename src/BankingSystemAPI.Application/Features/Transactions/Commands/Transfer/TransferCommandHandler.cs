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
                return Result<TransactionResDto>.Failure(basicValidationResult.Errors);

            var accountsResult = await LoadAccountsAsync(req);
            if (!accountsResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(accountsResult.Errors);

            var accountValidationResult = ValidateAccounts(accountsResult.Value!);
            if (!accountValidationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(accountValidationResult.Errors);

            var bankValidationResult = await ValidateBanksAsync(accountsResult.Value!);
            if (!bankValidationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(bankValidationResult.Errors);

            var authorizationResult = await ValidateAuthorizationAsync(accountsResult.Value!.Source, accountsResult.Value.Target, req);
            if (!authorizationResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(authorizationResult.Errors);

            // Validate balance with accurate fee calculation
            var balanceResult = await ValidateBalanceWithFeesAsync(accountsResult.Value!.Source, accountsResult.Value.Target, req.Amount);
            if (!balanceResult) // Using implicit bool operator!
                return Result<TransactionResDto>.Failure(balanceResult.Errors);

            var transferResult = await ExecuteTransferAsync(accountsResult.Value!, req);

            // Add side effects using ResultExtensions
            transferResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Transfer successful. Source: {SourceId}, Target: {TargetId}, Amount: {Amount}",
                        req.SourceAccountId, req.TargetAccountId, req.Amount);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Transfer failed. Source: {SourceId}, Target: {TargetId}, Amount: {Amount}, Errors: {Errors}",
                        req.SourceAccountId, req.TargetAccountId, req.Amount, string.Join(", ", errors));
                });

            return transferResult;
        }

        private Result ValidateBasicInput(TransferReqDto req)
        {
            var validations = new[]
            {
                req.Amount <= 0m ? Result.ValidationFailed("Transfer amount must be greater than zero.") : Result.Success(),
                req.SourceAccountId == req.TargetAccountId ? Result.ValidationFailed("Source and target accounts must be different.") : Result.Success()
            };

            return ResultExtensions.ValidateAll(validations);
        }

        private async Task<Result<AccountPair>> LoadAccountsAsync(TransferReqDto req)
        {
            try
            {
                // For transfer operations, we need tracking enabled and Currency navigation loaded
                var accountIds = new[] { req.SourceAccountId, req.TargetAccountId };
                var spec = new BankingSystemAPI.Application.Specifications.AccountSpecification.AccountsByIdsWithCurrencySpecification(accountIds);
                
                var accounts = await _uow.AccountRepository.ListAsync(spec);
                
                var accountsList = accounts.ToList();
                var src = accountsList.FirstOrDefault(a => a.Id == req.SourceAccountId);
                var tgt = accountsList.FirstOrDefault(a => a.Id == req.TargetAccountId);

                if (src == null)
                    return Result<AccountPair>.NotFound("Account", req.SourceAccountId);

                if (tgt == null)
                    return Result<AccountPair>.NotFound("Account", req.TargetAccountId);

                return Result<AccountPair>.Success(new AccountPair { Source = src, Target = tgt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load accounts for transfer: Source={SourceId}, Target={TargetId}", req.SourceAccountId, req.TargetAccountId);
                return Result<AccountPair>.BadRequest($"Failed to load accounts: {ex.Message}");
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
                    ? Result.Conflict("Cannot perform transaction: source account holder is inactive.") // Maps to 409
                    : Result.Success(),
                    
                accounts.Target.User?.IsActive != true 
                    ? Result.Conflict("Cannot perform transaction: target account holder is inactive.") // Maps to 409
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
                            bankValidations.Add(Result.Conflict("Cannot perform transaction: source user's bank is inactive."));
                        if (bankId == accounts.Target.User.BankId)
                            bankValidations.Add(Result.Conflict("Cannot perform transaction: target user's bank is inactive."));
                    }
                    else if (!banks.ContainsKey(bankId))
                    {
                        bankValidations.Add(Result.BadRequest($"Bank with ID {bankId} not found."));
                    }
                    else
                    {
                        bankValidations.Add(Result.Success());
                    }
                }

                return ResultExtensions.ValidateAll(bankValidations.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate banks for transfer");
                return Result.BadRequest("Failed to validate bank status.");
            }
        }

        private async Task<Result> ValidateAuthorizationAsync(Account source, Account target, TransferReqDto req)
        {
            try
            {
                var authResult = await _transactionAuth.CanInitiateTransferAsync(req.SourceAccountId, req.TargetAccountId);
                if (!authResult) // Using implicit bool operator!
                    return Result.Forbidden(string.Join("; ", authResult.Errors)); // Maps to 403

                var accountAuthResult = await _accountAuth.CanModifyAccountAsync(req.SourceAccountId, AccountModificationOperation.Withdraw);
                if (!accountAuthResult) // Using implicit bool operator!
                    return Result.Forbidden(string.Join("; ", accountAuthResult.Errors)); // Maps to 403

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
            }
        }

        private async Task<Result> ValidateBalanceWithFeesAsync(Account source, Account target, decimal amount)
        {
            // Calculate accurate fees based on currency difference
            var sourceCurrencyId = source.CurrencyId;
            var targetCurrencyId = target.CurrencyId;
            bool differentCurrency = sourceCurrencyId != targetCurrencyId;
            
            var feeRate = differentCurrency ? DifferentCurrencyFeeRate : SameCurrencyFeeRate;
            var fees = Math.Round(amount * feeRate, 2);
            var totalRequired = amount + fees;
            
            decimal available = source.Balance;
            if (source is CheckingAccount sc) available += sc.OverdraftLimit;
            
            return available >= totalRequired
                ? Result.Success()
                : Result.InsufficientFunds(totalRequired, available); // Maps to 409 Conflict
        }

        private async Task<Result<TransactionResDto>> ExecuteTransferAsync(AccountPair accounts, TransferReqDto req)
        {
            await _uow.BeginTransactionAsync();
            
            try
            {
                var trx = await ExecuteTransferLogicAsync(req, accounts.Source, accounts.Target);
                
                // EF Core automatically:
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
                return Result<TransactionResDto>.BadRequest($"Transfer execution failed: {ex.Message}");
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

            // Update balances using the same logic as the old implementation:
            // - Withdraw amount + fee from source (like WithdrawForTransfer)  
            // - Deposit only the converted/target amount to target account
            src.Balance = Math.Round(src.Balance - req.Amount - feeOnSource, 2);
            tgt.Balance = Math.Round(tgt.Balance + targetAmount, 2);

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
