using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts
{
    public class GetAllSavingsAccountsQueryHandler : IQueryHandler<GetAllSavingsAccountsQuery, List<SavingsAccountDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetAllSavingsAccountsQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<List<SavingsAccountDto>>> Handle(GetAllSavingsAccountsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var skip = (pageNumber - 1) * pageSize;

            if (_accountAuth is not null)
            {
                var accountQuery = _uow.AccountRepository.Table
                    .Where(a => a is SavingsAccount)
                    .Include(a => a.Currency)
                    .AsQueryable();

                var filterResult = await _accountAuth.FilterAccountsAsync(accountQuery, pageNumber, pageSize);
                if (filterResult.IsFailure)
                    return Result<List<SavingsAccountDto>>.Failure(filterResult.Errors);

                var (accounts, total) = filterResult.Value!;
                var mapped = accounts.OfType<SavingsAccount>().Select(a => _mapper.Map<SavingsAccountDto>(a)).ToList();
                return Result<List<SavingsAccountDto>>.Success(mapped);
            }

            var spec = new PagedSpecification<Account>(a => a is SavingsAccount, skip, pageSize, request.OrderBy, request.OrderDirection, a => a.Currency);
            var accountsDefault = await _uow.AccountRepository.ListAsync(spec);
            var mappedDefault = accountsDefault.OfType<SavingsAccount>().Select(a => _mapper.Map<SavingsAccountDto>(a)).ToList();
            return Result<List<SavingsAccountDto>>.Success(mappedDefault);
        }
    }
}
