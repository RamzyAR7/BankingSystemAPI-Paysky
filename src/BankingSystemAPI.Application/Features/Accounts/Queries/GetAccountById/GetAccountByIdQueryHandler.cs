using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById
{
    public class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetAccountByIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            if (_accountAuth is not null)
                await _accountAuth.CanViewAccountAsync(request.Id);

            var spec = new AccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null) return Result<AccountDto>.Failure(new[] { $"Account with ID '{request.Id}' not found." });
            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
        }
    }
}
