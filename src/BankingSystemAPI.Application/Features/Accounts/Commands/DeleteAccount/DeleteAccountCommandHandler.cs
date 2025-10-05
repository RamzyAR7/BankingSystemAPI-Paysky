#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount
{
    public class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService _accountAuth;

        public DeleteAccountCommandHandler(IUnitOfWork uow, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            var authResult = await _accountAuth.CanModifyAccountAsync(request.Id, AccountModificationOperation.Delete);
            if (authResult.IsFailure)
                return Result.Failure(authResult.Errors);

            var spec = new AccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result.Failure(new[] { string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Account", request.Id) });

            if (account.Balance > 0) return Result.Failure(new[] { string.Format(ApiResponseMessages.Validation.CannotDeleteAccountPositiveBalanceFormat, account.AccountNumber, account.Balance) });

            await _uow.AccountRepository.DeleteAsync(account);
            await _uow.SaveAsync();

            return Result.Success();
        }
    }
}

