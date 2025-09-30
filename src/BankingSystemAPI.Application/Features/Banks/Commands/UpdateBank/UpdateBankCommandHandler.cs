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

namespace BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank
{
    public class UpdateBankCommandHandler
            : ICommandHandler<UpdateBankCommand, BankResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public UpdateBankCommandHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<BankResDto>> Handle(UpdateBankCommand request, CancellationToken cancellationToken)
        {
            var spec = new BankByIdSpecification(request.id);
            var bank = await _uow.BankRepository.FindAsync(spec);

            if (bank == null)
                return Result<BankResDto>.Failure(new[] { "Bank not found." });

            bank.Name = request.bankDto.Name;

            await _uow.BankRepository.UpdateAsync(bank);
            await _uow.SaveAsync();

            return Result<BankResDto>.Success(_mapper.Map<BankResDto>(bank));
        }
    }
}
