using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankByName
{
    public sealed class GetBankByNameQueryHandler
            : IQueryHandler<GetBankByNameQuery, BankResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetBankByNameQueryHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<BankResDto>> Handle(GetBankByNameQuery request, CancellationToken cancellationToken)
        {
            var spec = new BankByNameSpecification(request.name);
            var bank = await _uow.BankRepository.FindAsync(spec);

            if (bank == null)
                return Result<BankResDto>.Failure(new[] { "Bank not found." });

            var dto = _mapper.Map<BankResDto>(bank);
            if (bank.ApplicationUsers != null)
                dto.Users = _mapper.Map<List<DTOs.User.UserResDto>>(bank.ApplicationUsers);

            return Result<BankResDto>.Success(dto);
        }
    }
}
