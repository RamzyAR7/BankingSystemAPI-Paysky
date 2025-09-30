using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Queries.GetAllCheckingAccounts
{
    public class GetAllCheckingAccountsQueryHandler : IQueryHandler<GetAllCheckingAccountsQuery, List<CheckingAccountDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetAllCheckingAccountsQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<List<CheckingAccountDto>>> Handle(GetAllCheckingAccountsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var skip = (pageNumber - 1) * pageSize;

            if (_accountAuth is not null)
            {
                var query = _uow.AccountRepository.Table
                    .Where(a => a is CheckingAccount)
                    .Include(a => a.Currency)
                    .AsQueryable();

                var (items, total) = await _accountAuth.FilterAccountsAsync(query, pageNumber, pageSize);
                var mapped = items.OfType<CheckingAccount>().Select(a => _mapper.Map<CheckingAccountDto>(a)).ToList();
                return Result<List<CheckingAccountDto>>.Success(mapped);
            }

            var spec = new PagedSpecification<Account>(a => a is CheckingAccount, skip, pageSize, request.OrderBy, request.OrderDirection, a => a.Currency);
            var accounts = await _uow.AccountRepository.ListAsync(spec);
            var mappedFallback = accounts.OfType<CheckingAccount>().Select(a => _mapper.Map<CheckingAccountDto>(a)).ToList();
            return Result<List<CheckingAccountDto>>.Success(mappedFallback);
        }
    }
}
