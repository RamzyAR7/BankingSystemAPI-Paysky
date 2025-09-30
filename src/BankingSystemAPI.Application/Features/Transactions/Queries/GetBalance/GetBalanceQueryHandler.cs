using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance
{
    public class GetBalanceQueryHandler : IQueryHandler<GetBalanceQuery, decimal>
    {
        private readonly IUnitOfWork _uow;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetBalanceQueryHandler(IUnitOfWork uow, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _accountAuth = accountAuth;
        }

        public async Task<Result<decimal>> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
        {
            var spec = new AccountByIdSpecification(request.AccountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result<decimal>.Failure(new[] { "Account not found." });

            if (_accountAuth is not null)
                await _accountAuth.CanViewAccountAsync(request.AccountId);

            return Result<decimal>.Success(account.Balance);
        }
    }
}
