using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BankingSystemAPI.Application.Features.Currencies.Queries.GetAllCurrencies
{
    public class GetAllCurrenciesQueryHandler : IQueryHandler<GetAllCurrenciesQuery, List<CurrencyDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllCurrenciesQueryHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<List<CurrencyDto>>> Handle(GetAllCurrenciesQuery request, CancellationToken cancellationToken)
        {
            var spec = new AllCurrenciesSpecification();
            var list = await _uow.CurrencyRepository.ListAsync(spec);
            // Map each item individually to avoid issues with mocked IMapper mapping collections
            var mapped = list.Select(c => _mapper.Map<CurrencyDto>(c)).ToList();
            return Result<List<CurrencyDto>>.Success(mapped);
        }
    }
}
