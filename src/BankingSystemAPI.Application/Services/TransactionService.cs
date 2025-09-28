using AutoMapper;
using AutoMapper.QueryableExtensions;
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
using BankingSystemAPI.Application.Interfaces.Authorization;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Services
{
    public partial class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITransactionHelperService _helper;
        private readonly UserManager<ApplicationUser>? _userManager; 
        private readonly ICurrentUserService _currentUserService;
        private readonly IAccountAuthorizationService? _accountAuth;
        private readonly ITransactionAuthorizationService? _transactionAuth;
        private readonly ILogger<TransactionService> _logger;
        private const int MaxRetryCount = 3;

        // Fee rates : 0.5% for same-currency transfers, 1% for cross-currency transfers
        private const decimal SameCurrencyFeeRate = 0.005m;
        private const decimal DifferentCurrencyFeeRate = 0.01m;

        // Primary constructor used going forward (no UserManager)
        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITransactionHelperService helper,
            ICurrentUserService currentUserService,
            IAccountAuthorizationService? accountAuth = null,
            ITransactionAuthorizationService? transactionAuth = null,
            ILogger<TransactionService>? logger = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _helper = helper;
            _userManager = null;
            _currentUserService = currentUserService;
            _accountAuth = accountAuth;
            _transactionAuth = transactionAuth;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TransactionService>.Instance;
        }

        // Backwards-compatible constructor used by tests and older callers (accepts UserManager)
        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITransactionHelperService helper,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IAccountAuthorizationService? accountAuth = null,
            ITransactionAuthorizationService? transactionAuth = null,
            ILogger<TransactionService>? logger = null)
            : this(unitOfWork, mapper, helper, currentUserService, accountAuth, transactionAuth, logger)
        {
            _userManager = userManager;
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

            if (_accountAuth != null)
                await _accountAuth.CanViewAccountAsync(accountId);

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
                if (_accountAuth != null)
                    await _accountAuth.CanModifyAccountAsync(request.AccountId, AccountModificationOperation.Deposit); // deposit uses access rules

                // Load account including User and Currency in a single query and request tracking so we can update
                var account = await _unitOfWork.AccountRepository.FindWithIncludesAsync(
                    a => a.Id == request.AccountId,
                    new Expression<Func<Account, object>>[] { a => a.User, a => a.Currency },
                    asNoTracking: false);

                if (account == null)
                {
                    account = await _unitOfWork.AccountRepository.GetByIdAsync(request.AccountId);
                }

                if (account == null)
                    throw new AccountNotFoundException($"Account with id {request.AccountId} not found.");
                if (!account.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction on inactive account.");

                // Check if user is active (use included navigation instead of additional DB call)
                if (account.User == null || !account.User.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive user.");

                // Check if user's bank is active
                if (account.User.BankId.HasValue)
                {
                    var bank = await _unitOfWork.BankRepository.GetByIdAsync(account.User.BankId.Value);
                    if (bank != null && !bank.IsActive)
                        throw new InvalidAccountOperationException("Cannot perform transaction: user's bank is inactive.");
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
                    Role = TransactionRole.Target,
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
                if (_accountAuth != null)
                    await _accountAuth.CanModifyAccountAsync(request.AccountId, AccountModificationOperation.Withdraw); // withdraw uses access rules

                var account = await _unitOfWork.AccountRepository.FindWithIncludesAsync(
                    a => a.Id == request.AccountId,
                    new Expression<Func<Account, object>>[] { a => a.User, a => a.Currency },
                    asNoTracking: false);

                if (account == null)
                {
                    account = await _unitOfWork.AccountRepository.GetByIdAsync(request.AccountId);
                }

                if (account == null)
                    throw new AccountNotFoundException($"Account with id {request.AccountId} not found.");
                if (!account.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction on inactive account.");

                // Check if user is active
                if (account.User == null || !account.User.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive user.");

                // Check if user's bank is active
                if (account.User.BankId.HasValue)
                {
                    var bank = await _unitOfWork.BankRepository.GetByIdAsync(account.User.BankId.Value);
                    if (bank != null && !bank.IsActive)
                        throw new InvalidAccountOperationException("Cannot perform transaction: user's bank is inactive.");
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
                if (_transactionAuth != null)
                    await _transactionAuth.CanInitiateTransferAsync(request.SourceAccountId, request.TargetAccountId);

                // Load both accounts including User and Currency in tracked state to avoid extra round-trips
                var source = await _unitOfWork.AccountRepository.FindWithIncludesAsync(
                    a => a.Id == request.SourceAccountId,
                    new Expression<Func<Account, object>>[] { a => a.User, a => a.Currency },
                    asNoTracking: false);

                var target = await _unitOfWork.AccountRepository.FindWithIncludesAsync(
                    a => a.Id == request.TargetAccountId,
                    new Expression<Func<Account, object>>[] { a => a.User, a => a.Currency },
                    asNoTracking: false);

                if (source == null)
                {
                    source = await _unitOfWork.AccountRepository.GetByIdAsync(request.SourceAccountId);
                }
                if (target == null)
                {
                    target = await _unitOfWork.AccountRepository.GetByIdAsync(request.TargetAccountId);
                }

                if (source == null)
                    throw new AccountNotFoundException($"Source account {request.SourceAccountId} not found.");
                if (target == null)
                    throw new AccountNotFoundException($"Target account {request.TargetAccountId} not found.");
                if (!source.IsActive || !target.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction on inactive account.");

                // Check if source user is active
                if (source.User == null || !source.User.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive source user.");
                // Check if target user is active
                if (target.User == null || !target.User.IsActive)
                    throw new InvalidAccountOperationException("Cannot perform transaction for inactive target user.");

                // Check banks for source and target users
                if (source.User.BankId.HasValue)
                {
                    var sBank = await _unitOfWork.BankRepository.GetByIdAsync(source.User.BankId.Value);
                    if (sBank != null && !sBank.IsActive)
                        throw new InvalidAccountOperationException("Cannot perform transaction: source user's bank is inactive.");
                }

                if (target.User.BankId.HasValue)
                {
                    var tBank = await _unitOfWork.BankRepository.GetByIdAsync(target.User.BankId.Value);
                    if (tBank != null && !tBank.IsActive)
                        throw new InvalidAccountOperationException("Cannot perform transaction: target user's bank is inactive.");
                }

                // Use currency IDs for conversion
                var sourceCurrencyId = source.CurrencyId;
                var targetCurrencyId = target.CurrencyId;

                decimal targetAmount = request.Amount;
                bool differentCurrency = sourceCurrencyId != targetCurrencyId;
                if (differentCurrency)
                {
                    // convert using id-based helper
                    targetAmount = await _helper.ConvertAsync(sourceCurrencyId, targetCurrencyId, request.Amount);
                }

                // determine fee rate
                var feeRate = differentCurrency ? DifferentCurrencyFeeRate : SameCurrencyFeeRate;
                // fee is charged on source account currency
                var feeOnSource = Math.Round(request.Amount * feeRate, 2);

                // Round target amount to 2 decimal places for storage/display
                targetAmount = Math.Round(targetAmount, 2);

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // withdraw amount + fee from source
                    source.WithdrawForTransfer(request.Amount + feeOnSource);
                    // deposit only the converted/target amount to target account (rounded)
                    target.Deposit(targetAmount);

                    await _unitOfWork.AccountRepository.UpdateAsync(source);
                    await _unitOfWork.AccountRepository.UpdateAsync(target);

                    // Create transaction with account transactions and save once to avoid duplicate inserts
                    var srcAccTrx = new AccountTransaction
                    {
                        AccountId = source.Id,
                        TransactionCurrency = source.Currency?.Code ?? string.Empty,
                        Amount = Math.Round(request.Amount, 2),
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
            var query = _unitOfWork.TransactionRepository.QueryWithAccountTransactions().OrderBy(t => t.Id);

            if (_transactionAuth != null)
            {
                var result = await _transactionAuth.FilterTransactionsAsync(query, pageNumber, pageSize);
                var list = result.Transactions;
                return _mapper.Map<IEnumerable<TransactionResDto>>(list);
            }

            // No authorization logic required, project to DTOs at DB level to avoid materializing entities
            var total = await query.CountAsync();
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;

            var dtos = await query
                .ProjectTo<TransactionResDto>(_mapper.ConfigurationProvider)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return dtos;
        }

        public async Task<IEnumerable<TransactionResDto>> GetByAccountIdAsync(int accountId, int pageNumber, int pageSize)
        {
            if (accountId <= 0) throw new BadRequestException("Invalid account id.");
            var query = _unitOfWork.TransactionRepository.QueryByAccountId(accountId).OrderBy(t => t.Id);

            if (_transactionAuth != null)
            {
                var result = await _transactionAuth.FilterTransactionsAsync(query, pageNumber, pageSize);
                var list = result.Transactions;
                return _mapper.Map<IEnumerable<TransactionResDto>>(list);
            }

            var total = await query.CountAsync();
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;

            var dtos = await query
                .ProjectTo<TransactionResDto>(_mapper.ConfigurationProvider)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return dtos;
        }

        public async Task<TransactionResDto> GetByIdAsync(int transactionId)
        {
            var query = _unitOfWork.TransactionRepository.QueryWithAccountTransactions().Where(t => t.Id == transactionId);

            if (_transactionAuth != null)
            {
                var result = await _transactionAuth.FilterTransactionsAsync(query, 1, 1);
                var trx = result.Transactions.FirstOrDefault();
                if (trx == null)
                    throw new ForbiddenException("Access to requested transaction is forbidden.");
                return _mapper.Map<TransactionResDto>(trx);
            }

            var dto = await query
                .ProjectTo<TransactionResDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (dto == null) throw new TransactionNotFoundException("Transaction not found.");
            return dto;
        }
    }
}
