using BankingSystemAPI.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.UnitOfWork
{
    public interface IUnitOfWork: IDisposable
    {
        IRoleRepository RoleRepository { get; }
        IUserRepository UserRepository { get; }
        IAccountRepository AccountRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        IAccountTransactionRepository AccountTransactionRepository { get; }
        IInterestLogRepository InterestLogRepository { get; }
        ICurrencyRepository CurrencyRepository { get; }
        IBankRepository BankRepository { get; }

        Task SaveAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task BeginTransactionAsync();
        Task ReloadTrackedEntitiesAsync();
        void DetachEntity<T>(T entity) where T : class;

        void Dispose();
    }
}
