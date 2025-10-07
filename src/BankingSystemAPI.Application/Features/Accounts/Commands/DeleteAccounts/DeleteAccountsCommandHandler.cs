#region Usings
using AutoMapper;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Common;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts
{
    /// <summary>
    /// Simplified bulk delete handler - validation handled by enhanced FluentValidation
    /// </summary>
    public class DeleteAccountsCommandHandler : ICommandHandler<DeleteAccountsCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService _accountAuth;

        public DeleteAccountsCommandHandler(IUnitOfWork uow, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result> Handle(DeleteAccountsCommand request, CancellationToken cancellationToken)
        {
            var distinctIds = request.Ids.Distinct().ToList();
            
            if (!distinctIds.Any())
                return Result.ValidationFailed(ApiResponseMessages.Validation.AtLeastOneAccountIdRequired);

            // Get accounts that exist
            var deleteSpec = AccountsByIdsSpecification.WithTracking(distinctIds);
            var accountsToDelete = await _uow.AccountRepository.ListAsync(deleteSpec);
            
            // Check if all requested accounts exist
            var foundIds = accountsToDelete.Select(a => a.Id).ToList();
            var missingIds = distinctIds.Except(foundIds).ToList();
            
            if (missingIds.Any())
            {
                return Result.ValidationFailed(string.Format(ApiResponseMessages.Validation.AccountsNotFoundFormat, string.Join(", ", missingIds)));
            }

            // Authorization - Check each account
            foreach (var account in accountsToDelete)
            {
                var authResult = await _accountAuth.CanModifyAccountAsync(account.Id, AccountModificationOperation.Delete);
                    if (authResult.IsFailure)
                        return Result.Failure(authResult.ErrorItems);
            }

            // Validate accounts can be deleted (no positive balance)
            foreach (var account in accountsToDelete)
            {
                if (account.Balance > 0)
                {
                    return Result.ValidationFailed(string.Format(ApiResponseMessages.Validation.CannotDeleteAccountPositiveBalanceFormat, account.AccountNumber, account.Balance));
                }
            }

            // Use bulk delete for better performance
            await _uow.AccountRepository.DeleteRangeAsync(accountsToDelete);
            await _uow.SaveAsync();

            return Result.Success();
        }
    }
}

