using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber
{
    public class GetAccountByAccountNumberQueryHandler : IQueryHandler<GetAccountByAccountNumberQuery, AccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;

        public GetAccountByAccountNumberQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<AccountDto>> Handle(GetAccountByAccountNumberQuery request, CancellationToken cancellationToken)
        {
            var spec = new AccountByAccountNumberSpecification(request.AccountNumber);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result<AccountDto>.Failure(new[] { $"Account with number '{request.AccountNumber}' not found." });

            var authResult = await _accountAuth.CanViewAccountAsync(account.Id);
            if (authResult.IsFailure)
                return Result<AccountDto>.Failure(authResult.Errors);

            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
        }
    }
}
