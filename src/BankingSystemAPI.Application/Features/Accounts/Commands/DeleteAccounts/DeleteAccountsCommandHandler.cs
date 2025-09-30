using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts
{
    public class DeleteAccountsCommandHandler : ICommandHandler<DeleteAccountsCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService? _accountAuth;

        public DeleteAccountsCommandHandler(IUnitOfWork uow, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result<bool>> Handle(DeleteAccountsCommand request, CancellationToken cancellationToken)
        {
            var distinctIds = request.Ids.Distinct().ToList();
            var spec = new AccountsByIdsSpecification(distinctIds);
            var accountsToDelete = await _uow.AccountRepository.ListAsync(spec);
            if (accountsToDelete.Count() != distinctIds.Count())
                return Result<bool>.Failure(new[] { "One or more specified accounts could not be found." });
            if (accountsToDelete.Any(a => a.Balance > 0))
                return Result<bool>.Failure(new[] { "Cannot delete accounts that have a positive balance." });

            // authorization per-account
            if (_accountAuth is not null)
            {
                foreach (var acc in accountsToDelete)
                {
                    await _accountAuth.CanModifyAccountAsync(acc.Id, AccountModificationOperation.Delete);
                }
            }

            await _uow.AccountRepository.DeleteRangeAsync(accountsToDelete);
            await _uow.SaveAsync();

            return Result<bool>.Success(true);
        }
    }
}
