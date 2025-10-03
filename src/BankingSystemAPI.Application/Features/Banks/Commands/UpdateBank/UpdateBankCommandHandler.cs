using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank
{
    public class UpdateBankCommandHandler : ICommandHandler<UpdateBankCommand, BankResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateBankCommandHandler> _logger;

        public UpdateBankCommandHandler(IUnitOfWork uow, IMapper mapper, ILogger<UpdateBankCommandHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<BankResDto>> Handle(UpdateBankCommand request, CancellationToken cancellationToken)
        {
            // Chain validation and update using ResultExtensions
            return await ValidateInputAsync(request)
                .BindAsync(async cmd => await FindBankAsync(cmd.id))
                .BindAsync(async bank => await UpdateBankAsync(bank, request.bankDto))
                .BindAsync(async bank => await PersistChangesAsync(bank))
                .MapAsync(async bank => _mapper.Map<BankResDto>(bank))
                .OnSuccess(() => 
                {
                    _logger.LogInformation("Bank updated successfully: {BankId}, Name: {BankName}", 
                        request.id, request.bankDto.Name);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Bank update failed for ID: {BankId}. Errors: {Errors}",
                        request.id, string.Join(", ", errors));
                });
        }

        private async Task<Result<UpdateBankCommand>> ValidateInputAsync(UpdateBankCommand request)
        {
            return request.ToResult("Update command is required.")
                .Bind(cmd => cmd.bankDto.ToResult("Bank data is required."))
                .Bind(dto => string.IsNullOrWhiteSpace(dto.Name)
                    ? Result<BankEditDto>.BadRequest("Bank name is required.")
                    : Result<BankEditDto>.Success(dto))
                .Map(_ => request);
        }

        private async Task<Result<Domain.Entities.Bank>> FindBankAsync(int bankId)
        {
            var spec = new BankByIdSpecification(bankId);
            var bank = await _uow.BankRepository.FindAsync(spec);
            return bank.ToResult($"Bank with ID '{bankId}' not found.");
        }

        private async Task<Result<Domain.Entities.Bank>> UpdateBankAsync(Domain.Entities.Bank bank, BankEditDto dto)
        {
            try
            {
                bank.Name = dto.Name.Trim();
                return Result<Domain.Entities.Bank>.Success(bank);
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Bank>.BadRequest($"Failed to update bank: {ex.Message}");
            }
        }

        private async Task<Result<Domain.Entities.Bank>> PersistChangesAsync(Domain.Entities.Bank bank)
        {
            try
            {
                await _uow.BankRepository.UpdateAsync(bank);
                await _uow.SaveAsync();
                return Result<Domain.Entities.Bank>.Success(bank);
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Bank>.BadRequest($"Failed to save bank changes: {ex.Message}");
            }
        }
    }
}
