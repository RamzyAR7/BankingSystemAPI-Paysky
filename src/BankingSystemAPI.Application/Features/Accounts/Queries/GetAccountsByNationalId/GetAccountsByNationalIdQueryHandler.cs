#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId
{
    public class GetAccountsByNationalIdQueryHandler : IQueryHandler<GetAccountsByNationalIdQuery, List<AccountDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;

        public GetAccountsByNationalIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<List<AccountDto>>> Handle(GetAccountsByNationalIdQuery request, CancellationToken cancellationToken)
        {
            // Ensure a user exists with this national id
            var targetUser = await _uow.UserRepository.FindAsync(new UserByNationalIdSpecification(request.NationalId));
            if (targetUser == null)
            {
                return Result<List<AccountDto>>.NotFound("User", request.NationalId);
            }

            var spec = new AccountsByNationalIdSpecification(request.NationalId);
            var accounts = await _uow.AccountRepository.ListAsync(spec);

            // Check authorization for each account
            foreach (var acc in accounts)
            {
                var authResult = await _accountAuth.CanViewAccountAsync(acc.Id);
                if (authResult.IsFailure)
                    return Result<List<AccountDto>>.Failure(authResult.ErrorItems);
            }

            var mapped = accounts.Select(a => _mapper.Map<AccountDto>(a)).ToList();
            return Result<List<AccountDto>>.Success(mapped);
        }
    }
}

