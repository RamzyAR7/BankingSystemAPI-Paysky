using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount
{
    public class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService? _accountAuth;

        public DeleteAccountCommandHandler(IUnitOfWork uow, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result<bool>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(request.Id, AccountModificationOperation.Delete);

            var spec = new AccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result<bool>.Failure(new[] { "Account not found." });

            if (account.Balance > 0) return Result<bool>.Failure(new[] { "Cannot delete an account with a positive balance." });

            await _uow.AccountRepository.DeleteAsync(account);
            await _uow.SaveAsync();

            return Result<bool>.Success(true);
        }
    }
}
