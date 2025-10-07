#region Usings
using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;
#endregion


namespace BankingSystemAPI.Infrastructure.UnitOfWork
{
    /// <summary>
    /// High-performance Unit of Work implementation with optimized transaction handling
    /// </summary>
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        // Repositories
        public IUserRepository UserRepository { get; }
        public IRoleRepository RoleRepository { get; }
        public IAccountRepository AccountRepository { get; }
        public ITransactionRepository TransactionRepository { get; }
        public IAccountTransactionRepository AccountTransactionRepository { get; }
        public IInterestLogRepository InterestLogRepository { get; }
        public ICurrencyRepository CurrencyRepository { get; }
        public IBankRepository BankRepository { get; }

        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // for testing with InMemory provider which does not support transactions
        private bool _noOpTransaction;

        // prevent multiple disposals
        private bool _disposed;

        // simple instance flag to track whether a transaction is active for this UnitOfWork
        private bool _transactionActiveFlag;
        private bool TransactionActive
        {
            get => _transactionActiveFlag;
            set => _transactionActiveFlag = value;
        }

        public UnitOfWork(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IAccountTransactionRepository accountTransactionRepository,
            IInterestLogRepository interestLogRepository,
            ICurrencyRepository currencyRepository,
            IBankRepository bankRepository,
            ApplicationDbContext context)
        {
            UserRepository = userRepository;
            RoleRepository = roleRepository;
            AccountRepository = accountRepository;
            TransactionRepository = transactionRepository;
            AccountTransactionRepository = accountTransactionRepository;
            InterestLogRepository = interestLogRepository;
            CurrencyRepository = currencyRepository;
            BankRepository = bankRepository;
            _context = context;
        }

        #region Transaction Management

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null || _noOpTransaction)
                throw new InvalidOperationException("Transaction already in progress.");

            // Check if using InMemory provider
            var provider = _context.Database.ProviderName ?? string.Empty;
            if (provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                _noOpTransaction = true;
                TransactionActive = true;
                return;
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            TransactionActive = true;
        }

        // Parameterless wrapper to satisfy IUnitOfWork
        public Task BeginTransactionAsync() => BeginTransactionAsync(CancellationToken.None);

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_noOpTransaction)
                {
                    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _noOpTransaction = false;
                    TransactionActive = false;
                    return;
                }

                if (_transaction == null)
                    throw new InvalidOperationException("No transaction in progress.");

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await CleanupTransactionAsync().ConfigureAwait(false);
            }
        }

        // Parameterless wrapper to satisfy IUnitOfWork
        public Task CommitAsync() => CommitAsync(CancellationToken.None);

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_noOpTransaction)
                {
                    // Detach modified entities
                    var entries = _context.ChangeTracker.Entries().ToList();
                    foreach (var entry in entries)
                    {
                        if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                            entry.State = EntityState.Detached;
                    }
                    _noOpTransaction = false;
                    TransactionActive = false;
                    return;
                }

                if (_transaction == null)
                    throw new InvalidOperationException("No transaction in progress.");

                await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await CleanupTransactionAsync().ConfigureAwait(false);
            }
        }

        // Parameterless wrapper to satisfy IUnitOfWork
        public Task RollbackAsync() => RollbackAsync(CancellationToken.None);

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            // If in transaction mode, defer the save
            if (TransactionActive)
            {
                // Changes will be saved during CommitAsync
                return;
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Parameterless wrapper to satisfy IUnitOfWork
        public Task SaveAsync() => SaveAsync(CancellationToken.None);

        public async Task ReloadTrackedEntitiesAsync(CancellationToken cancellationToken = default)
        {
            var tasks = _context.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Detached)
                .Select(async entry =>
                {
                    try
                    {
                        await entry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Throw a meaningful exception including entity type and state, preserve original exception
                        var entityType = entry.Entity?.GetType().FullName ?? "Unknown";
                        var state = entry.State.ToString();
                        throw new InvalidOperationException($"Failed to reload tracked entity '{entityType}' with state '{state}'.", ex);
                    }
                });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // Parameterless wrapper to satisfy IUnitOfWork
        public Task ReloadTrackedEntitiesAsync() => ReloadTrackedEntitiesAsync(CancellationToken.None);

        #endregion

        #region Entity Detachment

        public void DetachEntity<T>(T entity) where T : class
        {
            if (entity == null) return;

            var entry = _context.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }

        #endregion

        #region Cleanup

        private async Task CleanupTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync().ConfigureAwait(false);
                _transaction = null;
            }
            TransactionActive = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Do not dispose injected DbContext here. DI container controls its lifecycle.
                _transaction?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
