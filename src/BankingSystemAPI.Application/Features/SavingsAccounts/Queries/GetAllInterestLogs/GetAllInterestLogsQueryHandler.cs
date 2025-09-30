using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllInterestLogs
{
    public class GetAllInterestLogsQueryHandler : IQueryHandler<GetAllInterestLogsQuery, InterestLogsPagedDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetAllInterestLogsQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<InterestLogsPagedDto>> Handle(GetAllInterestLogsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var skip = (pageNumber - 1) * pageSize;

            if (_accountAuth is not null)
            {
                // Start with savings accounts query
                var accountQuery = _uow.AccountRepository.Table
                    .Where(a => a is SavingsAccount)
                    .Include(a => a.User)
                    .AsQueryable();

                // Let the authorization service filter the query
                accountQuery = await _accountAuth.FilterAccountsQueryAsync(accountQuery);

                // Materialize allowed account ids
                var accountIds = await accountQuery.Select(a => a.Id).ToListAsync(cancellationToken);

                if (accountIds == null || accountIds.Count == 0)
                {
                    var emptyDto = new InterestLogsPagedDto { Logs = Enumerable.Empty<InterestLogDto>(), TotalCount = 0 };
                    return Result<InterestLogsPagedDto>.Success(emptyDto);
                }

                var spec = new InterestLogsPagedSpecification(accountIds, skip, pageSize);
                var (items, total) = await _uow.InterestLogRepository.GetPagedAsync(spec);
                var dtoItems = items.Select(i => _mapper.Map<InterestLogDto>(i)).ToList();
                var dto = new InterestLogsPagedDto { Logs = dtoItems, TotalCount = total };
                return Result<InterestLogsPagedDto>.Success(dto);
            }

            var fallbackSpec = new InterestLogsPagedSpecification(skip, pageSize);
            var (fallbackItems, fallbackTotal) = await _uow.InterestLogRepository.GetPagedAsync(fallbackSpec);
            var fallbackDtoItems = fallbackItems.Select(i => _mapper.Map<InterestLogDto>(i)).ToList();
            var fallbackDto = new InterestLogsPagedDto { Logs = fallbackDtoItems, TotalCount = fallbackTotal };
            return Result<InterestLogsPagedDto>.Success(fallbackDto);
        }
    }
}
