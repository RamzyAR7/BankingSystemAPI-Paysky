#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank
{
    public class DeleteBankCommandHandler : ICommandHandler<DeleteBankCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<DeleteBankCommandHandler> _logger;

        public DeleteBankCommandHandler(IUnitOfWork uow, ILogger<DeleteBankCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteBankCommand request, CancellationToken cancellationToken)
        {
            // Chain validation and deletion using ResultExtensions
            return await FindBankAsync(request.id)
                .BindAsync(async bank => await ValidateCanDeleteAsync(bank, request.id))
                .BindAsync(async bank => await DeleteBankAsync(bank))
                .BindAsync(async bank => await PersistChangesAsync())
                .OnSuccess(() => 
                {
                    _logger.LogInformation("Bank deleted successfully: {BankId}", request.id);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Bank deletion failed for ID: {BankId}. Errors: {Errors}",
                        request.id, string.Join(", ", errors));
                });
        }

        private async Task<Result<Bank>> FindBankAsync(int bankId)
        {
            var spec = new BankByIdSpecification(bankId);
            var bank = await _uow.BankRepository.FindAsync(spec);
            return bank.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Bank", bankId));
        }

        private async Task<Result<Bank>> ValidateCanDeleteAsync(Bank bank, int bankId)
        {
            var hasUsers = await _uow.UserRepository.AnyAsync(u => u.BankId == bankId);
            return hasUsers
                ? Result<Bank>.BadRequest(ApiResponseMessages.Validation.DeleteUserHasAccounts)
                : Result<Bank>.Success(bank);
        }

        private async Task<Result<Bank>> DeleteBankAsync(Bank bank)
        {
            try
            {
                await _uow.BankRepository.DeleteAsync(bank);
                return Result<Bank>.Success(bank);
            }
            catch (Exception ex)
            {
                return Result<Bank>.BadRequest($"Failed to delete bank: {ex.Message}");
            }
        }

        private async Task<Result> PersistChangesAsync()
        {
            try
            {
                await _uow.SaveAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.BadRequest($"Failed to save changes: {ex.Message}");
            }
        }
    }
}

