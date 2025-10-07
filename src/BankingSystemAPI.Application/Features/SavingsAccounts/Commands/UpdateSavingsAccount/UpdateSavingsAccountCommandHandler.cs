#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using AutoMapper;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
#endregion


namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount
{
    /// <summary>
    /// Handles updating a savings account's editable fields with ResultExtensions patterns
    /// </summary>
    public class UpdateSavingsAccountCommandHandler : ICommandHandler<UpdateSavingsAccountCommand, SavingsAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;
        private readonly ILogger<UpdateSavingsAccountCommandHandler> _logger;

        public UpdateSavingsAccountCommandHandler(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<UpdateSavingsAccountCommandHandler> logger,
            IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
            _logger = logger;
        }

        public async Task<Result<SavingsAccountDto>> Handle(UpdateSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            // Chain authorization, validation, and update using ResultExtensions
            var authResult = await ValidateAuthorizationAsync(request.Id);
            if (authResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(authResult.ErrorItems);

            var accountResult = await LoadSavingsAccountAsync(request.Id);
            if (accountResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(accountResult.ErrorItems);

            var ownershipResult = ValidateOwnership(accountResult.Value!, request.Req.UserId);
            if (ownershipResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(ownershipResult.ErrorItems);

            var currencyResult = await ValidateCurrencyAsync(request.Req.CurrencyId);
            if (currencyResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(currencyResult.ErrorItems);

            var updateResult = await ExecuteUpdateAsync(accountResult.Value!, request.Req, currencyResult.Value!);

            // Add side effects using ResultExtensions
            updateResult.OnSuccess(() =>
                {
                    _logger.LogInformation(ApiResponseMessages.Logging.SavingsAccountUpdated, request.Id, request.Req.UserId, request.Req.CurrencyId);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning(ApiResponseMessages.Logging.SavingsAccountUpdateFailed, request.Id, request.Req.UserId, string.Join(", ", errors));
                });

            return updateResult;
        }

        private async Task<Result> ValidateAuthorizationAsync(int accountId)
        {
            try
            {
                var authResult = await _accountAuth.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);
                return authResult.IsSuccess
                    ? Result.Success()
                    : Result.Failure(authResult.ErrorItems);
            }
            catch (Exception ex)
            {
                return Result.Forbidden(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private async Task<Result<SavingsAccount>> LoadSavingsAccountAsync(int accountId)
        {
            try
            {
                var spec = new SavingsAccountByIdSpecification(accountId);
                var account = await _uow.AccountRepository.FindAsync(spec);

                return account is SavingsAccount savingsAccount
                    ? Result<SavingsAccount>.Success(savingsAccount)
                    : Result<SavingsAccount>.BadRequest(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Savings account", accountId));
            }
            catch (Exception ex)
            {
                return Result<SavingsAccount>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private Result ValidateOwnership(SavingsAccount account, string requestedUserId)
        {
            return string.Equals(requestedUserId, account.UserId, StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.BadRequest(ApiResponseMessages.Validation.AccountOwnershipRequired);
        }

        private async Task<Result<Currency>> ValidateCurrencyAsync(int currencyId)
        {
            try
            {
                var currency = await _uow.CurrencyRepository.GetByIdAsync(currencyId);

                return currency.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Currency", currencyId))
                    .Bind(c => c.IsActive
                        ? Result<Currency>.Success(c)
                        : Result<Currency>.BadRequest(ApiResponseMessages.Validation.CurrencyInactive));
            }
            catch (Exception ex)
            {
                return Result<Currency>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private async Task<Result<SavingsAccountDto>> ExecuteUpdateAsync(SavingsAccount account, SavingsAccountEditDto updateData, Currency currency)
        {
            try
            {
                // Apply updates
                account.UserId = updateData.UserId;
                account.CurrencyId = updateData.CurrencyId;

                // Convert percentage input (e.g., 30 -> 0.30) if user provided percentage rather than decimal
                decimal newInterestRate = updateData.InterestRate;
                if (newInterestRate > 1.0000m)
                {
                    newInterestRate = newInterestRate / 100m;
                }

                // Validate interest rate after conversion
                if (newInterestRate < 0.0000m || newInterestRate > 1.0000m)
                {
                    return Result<SavingsAccountDto>.BadRequest(ApiResponseMessages.Validation.InterestRateRange);
                }

                // Round to 4 decimal places to match database precision/scale
                account.InterestRate = Math.Round(newInterestRate, 4, MidpointRounding.AwayFromZero);

                // Persist changes
                await _uow.AccountRepository.UpdateAsync(account);
                await _uow.SaveAsync();

                // Set navigation property for proper mapping
                account.Currency = currency;

                var resultDto = _mapper.Map<SavingsAccountDto>(account);
                return Result<SavingsAccountDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                return Result<SavingsAccountDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }
    }
}
