using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankingSystemAPI.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        // Repositories
        public IAccountRepository AccountRepository { get; }
        public ITransactionRepository TransactionRepository { get; }
        public IAccountTransactionRepository AccountTransactionRepository { get; }
        public IInterestLogRepository InterestLogRepository { get; }
        public ICurrencyRepository CurrencyRepository { get; }
        public IBankRepository BankRepository { get; }
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _noOpTransaction;

        // Ambient flag to indicate repository methods should defer SaveChanges until Commit
        public static AsyncLocal<bool> TransactionActive = new AsyncLocal<bool>();

        public UnitOfWork(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IAccountTransactionRepository accountTransactionRepository,
            IInterestLogRepository interestLogRepository,
            ICurrencyRepository currencyRepository,
            IBankRepository bankRepository,
            ApplicationDbContext context
        )
        {
            AccountRepository = accountRepository;
            TransactionRepository = transactionRepository;
            AccountTransactionRepository = accountTransactionRepository;
            InterestLogRepository = interestLogRepository;
            CurrencyRepository = currencyRepository;
            BankRepository = bankRepository;
            _context = context;
        }

        // Start a transaction
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null || _noOpTransaction)
                throw new InvalidOperationException("BeginTransactionAsync failed: A transaction is already in progress for this UnitOfWork instance.");

            // InMemory provider does not support real transactions; treat as no-op but mark TransactionActive
            var provider = _context.Database.ProviderName ?? string.Empty;
            if (provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                _noOpTransaction = true;
                TransactionActive.Value = true;
                return;
            }

            _transaction = await _context.Database.BeginTransactionAsync();
            TransactionActive.Value = true;
        }

        // Commit the transaction
        public async Task CommitAsync()
        {
            if (_noOpTransaction)
            {
                // Save deferred changes and clear transaction flag
                await _context.SaveChangesAsync();
                _noOpTransaction = false;
                TransactionActive.Value = false;
                return;
            }

            if (_transaction == null)
                throw new InvalidOperationException("CommitAsync failed: No transaction in progress for this UnitOfWork instance.");

            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();

            await _transaction.DisposeAsync();
            _transaction = null;
            TransactionActive.Value = false;
        }

        // Rollback the transaction
        public async Task RollbackAsync()
        {
            if (_noOpTransaction)
            {
                // Detach any added/modified entries to emulate rollback
                var entries = _context.ChangeTracker.Entries().ToList();
                foreach (var entry in entries)
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    {
                        entry.State = EntityState.Detached;
                    }
                }

                _noOpTransaction = false;
                TransactionActive.Value = false;
                return;
            }

            if (_transaction == null)
                throw new InvalidOperationException("RollbackAsync failed: No transaction in progress for this UnitOfWork instance.");

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
            TransactionActive.Value = false;
        }

        // Save changes without transaction
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Reload tracked entries from DbContext (used on concurrency retry)
        public async Task ReloadTrackedEntitiesAsync()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                try
                {
                    await entry.ReloadAsync();
                }
                catch
                {
                    // Ignore reload failures for detached or deleted entries
                }
            }
        }

        // Dispose DbContext and transaction if still open
        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
