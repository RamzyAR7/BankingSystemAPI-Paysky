using AutoMapper;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Application.Interfaces.Identity;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Services
{
    public partial class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITransactionHelperService _helper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IBankAuthorizationHelper? _bankAuth;
        private readonly ILogger<TransactionService> _logger;
        private const int MaxRetryCount = 3;

        // Fee rates : 0.5% for same-currency transfers, 1% for cross-currency transfers
        private const decimal SameCurrencyFeeRate = 0.005m;
        private const decimal DifferentCurrencyFeeRate = 0.01m;

        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITransactionHelperService helper,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IBankAuthorizationHelper? bankAuth = null,
            ILogger<TransactionService>? logger = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _helper = helper;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _bankAuth = bankAuth;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TransactionService>.Instance;
        }

        // Backward-compatible overload used by tests and callers that pass logger as 6th argument
        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITransactionHelperService helper,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            ILogger<TransactionService> logger)
            : this(unitOfWork, mapper, helper, userManager, currentUserService, null, logger)
        {
        }

        // Execute operation with retry on concurrency conflict
        private async Task ExecuteWithRetryAsync(Func<Task> operation)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await operation();
                    break; // Success
                }
                catch (DbUpdateConcurrencyException)
                {
                    retryCount++;
                    if (retryCount >= MaxRetryCount)
                        throw new InvalidAccountOperationException("Concurrent update detected. Please try again later.");

                    // Reload tracked entities via UnitOfWork helper
                    await _unitOfWork.ReloadTrackedEntitiesAsync();
                }
                catch
                {
                    throw;
                }
            }
        }

        public async Task<decimal> GetBalanceAsync(int accountId)
        {
            if (accountId <= 0)
                throw new BadRequestException("Invalid account id.");

            if (_bankAuth != null)
                await _bankAuth.EnsureCanAccessAccountAsync(accountId);

            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new AccountNotFoundException($"Account with id {accountId} not found.");

            return account.Balance;
        }

        public async Task<TransactionResDto> DepositAsync(DepositReqDto request)
        {
            if (request.Amount <= 0)
                throw new BadRequestException("Amount must be greater than zero.");

            Transaction trx = null;

            await ExecuteWithRetryAsync(async () =>
            {
                // Authorization: ensure acting user can access account (clients may only access own accounts)
                if (_bankAuth != null)
                    await _bankAuth.EnsureCanAccessAccountAsync(request.AccountId); // deposit uses access rules

                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.AccountId);
                if (account == null)
                    throw new AccountNotFoundException($"Account with id {request.AccountId} not found.");
                if (!account.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction on inactive account.");

                // Check if user is active
                var user = await _userManager.FindByIdAsync(account.UserId);
                if (user == null || !user.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive user.");

                // ensure currency navigation is loaded so TransactionCurrency is set
                if (account.Currency == null)
                {
                    account.Currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(account.CurrencyId);
                }

                account.Deposit(request.Amount);
                await _unitOfWork.AccountRepository.UpdateAsync(account);

                trx = new Transaction
                {
                    TransactionType = TransactionType.Deposit,
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.TransactionRepository.AddAsync(trx);

                var accTrx = new AccountTransaction
                {
                    AccountId = account.Id,
                    TransactionId = trx.Id,
                    TransactionCurrency = account.Currency?.Code ?? string.Empty,
                    Amount = request.Amount,
                    // For deposit treat the account as the source of the transaction record (consistent with withdraw)
                    Role = TransactionRole.Source,
                    Fees = 0m
                };

                // attach account transaction to transaction for mapping
                trx.AccountTransactions = new List<AccountTransaction> { accTrx };

                await _unitOfWork.AccountTransactionRepository.AddAsync(accTrx);
                await _unitOfWork.SaveAsync();
            });

            return _mapper.Map<TransactionResDto>(trx);
        }

        public async Task<TransactionResDto> WithdrawAsync(WithdrawReqDto request)
        {
            if (request.Amount <= 0)
                throw new BadRequestException("Amount must be greater than zero.");

            Transaction trx = null;

            await ExecuteWithRetryAsync(async () =>
            {
                // Authorization: ensure acting user can access account (clients may only access own accounts)
                if (_bankAuth != null)
                    await _bankAuth.EnsureCanAccessAccountAsync(request.AccountId); // withdraw uses access rules

                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.AccountId);
                if (account == null)
                    throw new AccountNotFoundException($"Account with id {request.AccountId} not found.");
                if (!account.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction on inactive account.");

                // Check if user is active
                var user = await _userManager.FindByIdAsync(account.UserId);
                if (user == null || !user.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive user.");

                // ensure currency navigation is loaded so TransactionCurrency is set
                if (account.Currency == null)
                {
                    account.Currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(account.CurrencyId);
                }

                account.Withdraw(request.Amount);
                await _unitOfWork.AccountRepository.UpdateAsync(account);

                trx = new Transaction
                {
                    TransactionType = TransactionType.Withdraw,
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.TransactionRepository.AddAsync(trx);

                var accTrx = new AccountTransaction
                {
                    AccountId = account.Id,
                    TransactionId = trx.Id,
                    TransactionCurrency = account.Currency?.Code ?? string.Empty,
                    Amount = request.Amount,
                    Role = TransactionRole.Source,
                    Fees = 0m
                };

                trx.AccountTransactions = new List<AccountTransaction> { accTrx };

                await _unitOfWork.AccountTransactionRepository.AddAsync(accTrx);
                await _unitOfWork.SaveAsync();
            });

            return _mapper.Map<TransactionResDto>(trx);
        }

        public async Task<TransactionResDto> TransferAsync(TransferReqDto request)
        {
            if (request.Amount <= 0)
                throw new BadRequestException("Amount must be greater than zero.");
            if (request.SourceAccountId == request.TargetAccountId)
                throw new BadRequestException("Source and target accounts must be different.");

            Transaction trx = null;

            await ExecuteWithRetryAsync(async () =>
            {
                // Authorization: ensure acting user can initiate this transfer according to bank rules
                if (_bankAuth != null)
                    await _bankAuth.EnsureCanInitiateTransferAsync(request.SourceAccountId, request.TargetAccountId);

                var source = await _unitOfWork.AccountRepository.GetByIdAsync(request.SourceAccountId);
                var target = await _unitOfWork.AccountRepository.GetByIdAsync(request.TargetAccountId);

                if (source == null)
                    throw new AccountNotFoundException($"Source account {request.SourceAccountId} not found.");
                if (target == null)
                    throw new AccountNotFoundException($"Target account {request.TargetAccountId} not found.");
                if (!source.IsActive || !target.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction on inactive account.");

                // Check if source user is active
                var sourceUser = await _userManager.FindByIdAsync(source.UserId);
                if (sourceUser == null || !sourceUser.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive source user.");
                // Check if target user is active
                var targetUser = await _userManager.FindByIdAsync(target.UserId);
                if (targetUser == null || !targetUser.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive target user.");

                // Ensure currency navigation is loaded so we can use Currency.Code
                if (source.Currency == null)
                {
                    source.Currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(source.CurrencyId);
                }
                if (target.Currency == null)
                {
                    target.Currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(target.CurrencyId);
                }

                var sourceCode = source.Currency?.Code ?? string.Empty;
                var targetCode = target.Currency?.Code ?? string.Empty;

                decimal targetAmount = request.Amount;
                bool differentCurrency = !string.Equals(sourceCode, targetCode, StringComparison.OrdinalIgnoreCase);
                if (differentCurrency)
                {
                    // use code-based conversion helper
                    targetAmount = await _helper.ConvertAsync(sourceCode, targetCode, request.Amount);
                }

                // determine fee rate
                var feeRate = differentCurrency ? DifferentCurrencyFeeRate : SameCurrencyFeeRate;
                // fee is charged on source account currency
                var feeOnSource = Math.Round(request.Amount * feeRate, 2);

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // withdraw amount + fee from source
                    source.WithdrawForTransfer(request.Amount + feeOnSource);
                    // deposit only the converted/target amount to target account
                    target.Deposit(targetAmount);

                    await _unitOfWork.AccountRepository.UpdateAsync(source);
                    await _unitOfWork.AccountRepository.UpdateAsync(target);

                    // Create transaction with account transactions and save once to avoid duplicate inserts
                    var srcAccTrx = new AccountTransaction
                    {
                        AccountId = source.Id,
                        TransactionCurrency = source.Currency?.Code ?? string.Empty,
                        Amount = request.Amount,
                        Role = TransactionRole.Source,
                        Fees = feeOnSource
                    };

                    var tgtAccTrx = new AccountTransaction
                    {
                        AccountId = target.Id,
                        TransactionCurrency = target.Currency?.Code ?? string.Empty,
                        Amount = targetAmount,
                        Role = TransactionRole.Target,
                        Fees = 0m
                    };

                    trx = new Transaction
                    {
                        TransactionType = TransactionType.Transfer,
                        Timestamp = DateTime.UtcNow,
                        AccountTransactions = new List<AccountTransaction> { srcAccTrx, tgtAccTrx }
                    };

                    // Add transaction once; EF will insert related AccountTransactions
                    await _unitOfWork.TransactionRepository.AddAsync(trx);

                    await _unitOfWork.SaveAsync();
                    await _unitOfWork.CommitAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackAsync();
                    throw;
                }
            });

            return _mapper.Map<TransactionResDto>(trx);
        }

        public async Task<IEnumerable<TransactionResDto>> GetAllAsync(int pageNumber, int pageSize)
        {
            var skip = (Math.Max(1, pageNumber) - 1) * Math.Max(1, pageSize);
            var list = await _unitOfWork.TransactionRepository.FindAllAsync(
                null, pageSize, skip, null, "DESC", new[] { "AccountTransactions" });

            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterTransactionsAsync(list);
                list = filtered.ToList();
            }

            return _mapper.Map<IEnumerable<TransactionResDto>>(list);
        }

        public async Task<IEnumerable<TransactionResDto>> GetByAccountIdAsync(int accountId, int pageNumber, int pageSize)
        {
            if (accountId <= 0) throw new BadRequestException("Invalid account id.");

            var take = Math.Max(1, pageSize);
            var skip = (Math.Max(1, pageNumber) - 1) * take;

            // Query page of transactions that reference the account (repository will apply paging)
            var list = await _unitOfWork.TransactionRepository.FindAllAsync(
                t => t.AccountTransactions != null && t.AccountTransactions.Any(at => at.AccountId == accountId),
                take, skip, null, "DESC", new[] { "AccountTransactions" });

            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterTransactionsAsync(list);
                list = filtered.ToList();
            }

            return _mapper.Map<IEnumerable<TransactionResDto>>(list);
        }

        public async Task<TransactionResDto> GetByIdAsync(int transactionId)
        {
            var trx = await _unitOfWork.TransactionRepository.FindAsync(
                t => t.Id == transactionId, new[] { "AccountTransactions" });

            if (trx == null)
                throw new TransactionNotFoundException("Transaction not found.");

            // Authorization: ensure caller is allowed to view this transaction according to bank rules
            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterTransactionsAsync(new[] { trx });
                if (!filtered.Any())
                    throw new BankingSystemAPI.Application.Exceptions.ForbiddenException("Access to requested transaction is forbidden.");
            }

            return _mapper.Map<TransactionResDto>(trx);
        }
    }
}
