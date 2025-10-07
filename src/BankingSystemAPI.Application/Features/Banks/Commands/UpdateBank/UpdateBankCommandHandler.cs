#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
#endregion


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
                .MapAsync(bank => Task.FromResult(_mapper.Map<BankResDto>(bank)))
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

        private Task<Result<UpdateBankCommand>> ValidateInputAsync(UpdateBankCommand request)
        {
            var res = request.ToResult(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Update command"))
                .Bind(cmd => cmd.bankDto.ToResult(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Bank data")))
                .Bind(dto => string.IsNullOrWhiteSpace(dto.Name)
                    ? Result<BankEditDto>.BadRequest(ApiResponseMessages.Validation.BankNameRequired)
                    : Result<BankEditDto>.Success(dto))
                .Map(_ => request);

            return Task.FromResult(res);
        }

        private async Task<Result<Bank>> FindBankAsync(int bankId)
        {
            var spec = new BankByIdSpecification(bankId);
            var bank = await _uow.BankRepository.FindAsync(spec);
            return bank.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Bank", bankId));
        }

        private Task<Result<Bank>> UpdateBankAsync(Bank bank, BankEditDto dto)
        {
            try
            {
                bank.Name = dto.Name.Trim();
                return Task.FromResult(Result<Bank>.Success(bank));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result<Bank>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message)));
            }
        }
    }
}

