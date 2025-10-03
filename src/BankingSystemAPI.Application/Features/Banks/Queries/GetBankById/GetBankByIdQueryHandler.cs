using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankById
{
    public class GetBankByIdQueryHandler : IQueryHandler<GetBankByIdQuery, BankResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetBankByIdQueryHandler> _logger;

        public GetBankByIdQueryHandler(IUnitOfWork uow, IMapper mapper, ILogger<GetBankByIdQueryHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<BankResDto>> Handle(GetBankByIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new BankByIdSpecification(request.id);
            var bank = await _uow.BankRepository.FindAsync(spec);

            var result = bank.ToResult($"Bank with ID '{request.id}' not found.")
                .Map(b => MapBankToDto(b));

            // Add side effects without changing return type
            if (result.IsSuccess)
            {
                _logger.LogDebug("Successfully retrieved bank with ID: {BankId}", request.id);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve bank with ID: {BankId}, Errors: {Errors}",
                    request.id, string.Join(", ", result.Errors));
            }

            return result;
        }

        private BankResDto MapBankToDto(Domain.Entities.Bank bank)
        {
            var dto = _mapper.Map<BankResDto>(bank);
            if (bank.ApplicationUsers != null)
                dto.Users = _mapper.Map<List<UserResDto>>(bank.ApplicationUsers);
            
            return dto;
        }
    }
}
