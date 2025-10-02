using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetInterestLogsByAccountId
{
    public class GetInterestLogsByAccountIdQueryHandler : IQueryHandler<GetInterestLogsByAccountIdQuery, InterestLogsPagedDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetInterestLogsByAccountIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<InterestLogsPagedDto>> Handle(GetInterestLogsByAccountIdQuery request, CancellationToken cancellationToken)
        {
            if (_accountAuth is not null)
            {
                await _accountAuth.CanViewAccountAsync(request.AccountId);
            }

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var skip = (pageNumber - 1) * pageSize;

            var spec = new InterestLogsByAccountPagedSpecification(request.AccountId, skip, pageSize);
            var (items, total) = await _uow.InterestLogRepository.GetPagedAsync(spec);
            var dtoItems = items.Select(i => _mapper.Map<InterestLogDto>(i)).ToList();
            var dto = new InterestLogsPagedDto { Logs = dtoItems, TotalCount = total };
            return Result<InterestLogsPagedDto>.Success(dto);
        }
    }
}
