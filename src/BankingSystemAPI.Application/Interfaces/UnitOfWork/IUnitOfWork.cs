#region Usings
using BankingSystemAPI.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        IRoleRepository RoleRepository { get; }
        IUserRepository UserRepository { get; }
        IAccountRepository AccountRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        IAccountTransactionRepository AccountTransactionRepository { get; }
        IInterestLogRepository InterestLogRepository { get; }
        ICurrencyRepository CurrencyRepository { get; }
        IBankRepository BankRepository { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task ReloadTrackedEntitiesAsync(CancellationToken cancellationToken = default);
        void DetachEntity<T>(T entity) where T : class;

        // Dispose is inherited from IDisposable
    }
}

