using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId
{
    public class GetAccountsByUserIdQueryHandler : IQueryHandler<GetAccountsByUserIdQuery, List<AccountDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;
        private readonly IUserAuthorizationService? _userAuth;

        public GetAccountsByUserIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null, IUserAuthorizationService? userAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
            _userAuth = userAuth;
        }

        public async Task<Result<List<AccountDto>>> Handle(GetAccountsByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<List<AccountDto>>.Failure(new[] { "UserId is required." });
            }

            // Explicit user-level authorization: if a user authorization service is available, validate access to the target user
            if (_userAuth is not null)
            {
                await _userAuth.CanViewUserAsync(request.UserId);
            }

            if (_accountAuth is not null)
            {
                var accountQuery = _uow.AccountRepository.QueryByUserId(request.UserId).AsQueryable();
                accountQuery = await _accountAuth.FilterAccountsQueryAsync(accountQuery);

                // Fetch all matching accounts via repository paging helper
                var (accounts, total) = await _uow.AccountRepository.GetFilteredAccountsAsync(accountQuery, 1, int.MaxValue);
                var mapped = accounts.Select(a => _mapper.Map<AccountDto>(a)).ToList();
                return Result<List<AccountDto>>.Success(mapped);
            }

            // Fallback: original behavior
            var spec = new AccountsByUserIdSpecification(request.UserId);
            var accountsFallback = await _uow.AccountRepository.ListAsync(spec);

            var mappedFallback = accountsFallback.Select(a => _mapper.Map<AccountDto>(a)).ToList();
            return Result<List<AccountDto>>.Success(mappedFallback);
        }
    }
}
