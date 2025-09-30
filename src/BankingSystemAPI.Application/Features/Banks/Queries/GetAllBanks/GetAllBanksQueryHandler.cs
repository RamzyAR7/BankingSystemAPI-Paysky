using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetAllBanks
{
    public class GetAllBanksQueryHandler
            : IQueryHandler<GetAllBanksQuery, List<BankSimpleResDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllBanksQueryHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<List<BankSimpleResDto>>> Handle(GetAllBanksQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var skip = (pageNumber - 1) * pageSize;
            var spec = new PagedSpecification<Bank>(skip, pageSize, request.OrderBy, request.OrderDirection);

            var banks = await _uow.BankRepository.ListAsync(spec);
            var mapped = _mapper.Map<List<BankSimpleResDto>>(banks);

            return Result<List<BankSimpleResDto>>.Success(mapped);
        }
    }
}
