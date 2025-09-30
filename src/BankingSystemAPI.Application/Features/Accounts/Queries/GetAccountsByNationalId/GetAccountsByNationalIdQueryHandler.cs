using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId
{
    public class GetAccountsByNationalIdQueryHandler : IQueryHandler<GetAccountsByNationalIdQuery, List<AccountDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetAccountsByNationalIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<List<AccountDto>>> Handle(GetAccountsByNationalIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new AccountsByNationalIdSpecification(request.NationalId);
            var accounts = await _uow.AccountRepository.ListAsync(spec);

            if (_accountAuth is not null)
            {
                foreach (var acc in accounts)
                {
                    await _accountAuth.CanViewAccountAsync(acc.Id);
                }
            }

            var mapped = accounts.Select(a => _mapper.Map<AccountDto>(a)).ToList();
            return Result<List<AccountDto>>.Success(mapped);
        }
    }
}
