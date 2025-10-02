using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BankingSystemAPI.Domain.Constant.Permission;

namespace BankingSystemAPI.Application.Features.Banks.Commands.CreateBank
{
    public class CreateBankCommandHandler : ICommandHandler<CreateBankCommand, BankResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CreateBankCommandHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<Result<BankResDto>> Handle(CreateBankCommand request, CancellationToken cancellationToken)
        {
            var dto = request.bankDto;
            var normalized = dto.Name.Trim();
            var normalizedLower = normalized.ToLowerInvariant();

            var spec = new BankByNormalizedNameSpecification(normalizedLower);
            var existing = await _uow.BankRepository.FindAsync(spec);
            if (existing != null)
                return Result<BankResDto>.Failure(new[] { "A bank with the same name already exists." });

            var entity = _mapper.Map<Domain.Entities.Bank>(dto);
            entity.Name = normalized;
            entity.CreatedAt = DateTime.UtcNow;

            await _uow.BankRepository.AddAsync(entity);
            await _uow.SaveAsync();

            return Result<BankResDto>.Success(_mapper.Map<BankResDto>(entity));
        }
    }
}
