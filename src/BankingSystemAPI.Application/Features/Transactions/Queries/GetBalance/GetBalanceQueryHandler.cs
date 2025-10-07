#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance
{
    public class GetBalanceQueryHandler : IQueryHandler<GetBalanceQuery, decimal>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService _accountAuth;
        private readonly ILogger<GetBalanceQueryHandler> _logger;

        public GetBalanceQueryHandler(IUnitOfWork uow, ILogger<GetBalanceQueryHandler> logger, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _accountAuth = accountAuth;
            _logger = logger;
        }

        public async Task<Result<decimal>> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
        {
            // Chain account retrieval, authorization, and balance extraction using ResultExtensions
            var accountResult = await LoadAccountAsync(request.AccountId);
            if (accountResult.IsFailure)
                return Result<decimal>.Failure(accountResult.ErrorItems);

            var authResult = await ValidateAuthorizationAsync(request.AccountId);
            if (authResult.IsFailure)
                return Result<decimal>.Failure(authResult.ErrorItems);

            var balance = accountResult.Value!.Balance;

            // Add side effects using ResultExtensions
            var result = Result<decimal>.Success(balance);
            result.OnSuccess(() => 
                {
                    _logger.LogDebug("Balance retrieved successfully for account: {AccountId}, Balance: {Balance}", 
                        request.AccountId, balance);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Failed to retrieve balance for account: {AccountId}. Errors: {Errors}",
                        request.AccountId, string.Join(", ", errors));
                });

            return result;
        }

        private async Task<Result<Domain.Entities.Account>> LoadAccountAsync(int accountId)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            return account.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Account", accountId));
        }

        private async Task<Result> ValidateAuthorizationAsync(int accountId)
        {
            try
            {
                var authResult = await _accountAuth.CanViewAccountAsync(accountId);
                return authResult.IsSuccess 
                    ? Result.Success() 
                    : Result.Failure(authResult.ErrorItems);
            }
            catch (Exception ex)
            {
                return Result.Forbidden(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }
    }
}

