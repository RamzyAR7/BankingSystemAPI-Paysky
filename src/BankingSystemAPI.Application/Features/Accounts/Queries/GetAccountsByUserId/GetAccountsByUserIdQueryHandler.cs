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
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId
{
    public class GetAccountsByUserIdQueryHandler : IQueryHandler<GetAccountsByUserIdQuery, List<AccountDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;
        private readonly IUserAuthorizationService _userAuth;

        public GetAccountsByUserIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService accountAuth, IUserAuthorizationService userAuth)
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
                var err = new ResultError(ErrorType.Validation, ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "UserId"));
                return Result<List<AccountDto>>.Failure(err);
            }

            // Ensure target user exists - return NotFound instead of empty list when user does not exist
            var targetUser = await _uow.UserRepository.FindAsync(new UserByIdSpecification(request.UserId));
            if (targetUser == null)
            {
                return Result<List<AccountDto>>.NotFound("User", request.UserId);
            }

            // Explicit user-level authorization: validate access to the target user
            var userAuthResult = await _userAuth.CanViewUserAsync(request.UserId);
            if (userAuthResult.IsFailure)
                return Result<List<AccountDto>>.Failure(userAuthResult.ErrorItems);

            var accountQuery = _uow.AccountRepository.QueryByUserId(request.UserId).AsQueryable();
            var filterResult = await _accountAuth.FilterAccountsQueryAsync(accountQuery);

            if (filterResult.IsFailure)
                return Result<List<AccountDto>>.Failure(filterResult.ErrorItems);

            var filteredQuery = filterResult.Value!;

            // Fetch all matching accounts via repository paging helper
            var (accounts, total) = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, 1, int.MaxValue);
            var mapped = accounts.Select(a => _mapper.Map<AccountDto>(a)).ToList();
            return Result<List<AccountDto>>.Success(mapped);
        }
    }
}

