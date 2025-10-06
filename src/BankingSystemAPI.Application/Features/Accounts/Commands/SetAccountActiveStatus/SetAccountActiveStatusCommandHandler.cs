#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Commands.SetAccountActiveStatus
{
    public class SetAccountActiveStatusCommandHandler : ICommandHandler<SetAccountActiveStatusCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService _accountAuth;

        public SetAccountActiveStatusCommandHandler(IUnitOfWork uow, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result> Handle(SetAccountActiveStatusCommand request, CancellationToken cancellationToken)
        {
            var authResult = await _accountAuth.CanModifyAccountAsync(request.Id, AccountModificationOperation.Edit);
            if (authResult.IsFailure)
                return Result.Failure(authResult.ErrorItems);

            var spec = new AccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result.Failure(new[] { string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Account", request.Id) });
            account.IsActive = request.IsActive;
            await _uow.AccountRepository.UpdateAsync(account);
            await _uow.SaveAsync();

            return Result.Success();
        }
    }
}

