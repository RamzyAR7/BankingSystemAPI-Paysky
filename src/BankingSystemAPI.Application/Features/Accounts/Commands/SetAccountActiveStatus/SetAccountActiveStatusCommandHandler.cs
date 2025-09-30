using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.SetAccountActiveStatus
{
    public class SetAccountActiveStatusCommandHandler : ICommandHandler<SetAccountActiveStatusCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService? _accountAuth;

        public SetAccountActiveStatusCommandHandler(IUnitOfWork uow, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result<bool>> Handle(SetAccountActiveStatusCommand request, CancellationToken cancellationToken)
        {
            if (_accountAuth is not null)
                await _accountAuth.CanModifyAccountAsync(request.Id, AccountModificationOperation.Edit);

            var spec = new AccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result<bool>.Failure(new[] { $"Account with ID '{request.Id}' not found." });
            account.IsActive = request.IsActive;
            await _uow.AccountRepository.UpdateAsync(account);
            await _uow.SaveAsync();

            return Result<bool>.Success(true);
        }
    }
}
