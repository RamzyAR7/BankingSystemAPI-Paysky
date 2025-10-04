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
                return Result<SavingsAccountDto>.Failure(authResult.Errors);

            var accountResult = await LoadSavingsAccountAsync(request.Id);
            if (accountResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(accountResult.Errors);

            var ownershipResult = ValidateOwnership(accountResult.Value!, request.Req.UserId);
            if (ownershipResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(ownershipResult.Errors);

            var currencyResult = await ValidateCurrencyAsync(request.Req.CurrencyId);
            if (currencyResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(currencyResult.Errors);

            var updateResult = await ExecuteUpdateAsync(accountResult.Value!, request.Req, currencyResult.Value!);
            
            // Add side effects using ResultExtensions
            updateResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Savings account updated successfully: AccountId={AccountId}, UserId={UserId}, CurrencyId={CurrencyId}", 
                        request.Id, request.Req.UserId, request.Req.CurrencyId);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Savings account update failed: AccountId={AccountId}, UserId={UserId}, Errors={Errors}",
                        request.Id, request.Req.UserId, string.Join(", ", errors));
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
                    : Result.Failure(authResult.Errors);
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
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
                    : Result<SavingsAccount>.BadRequest("Savings account not found.");
            }
            catch (Exception ex)
            {
                return Result<SavingsAccount>.BadRequest($"Failed to load savings account: {ex.Message}");
            }
        }

        private Result ValidateOwnership(SavingsAccount account, string requestedUserId)
        {
            return string.Equals(requestedUserId, account.UserId, StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.BadRequest("Specified user does not own this account.");
        }

        private async Task<Result<Currency>> ValidateCurrencyAsync(int currencyId)
        {
            try
            {
                var currency = await _uow.CurrencyRepository.GetByIdAsync(currencyId);
                
                return currency.ToResult($"Currency with ID '{currencyId}' not found.")
                    .Bind(c => c.IsActive 
                        ? Result<Currency>.Success(c) 
                        : Result<Currency>.BadRequest("Cannot set account to an inactive currency."));
            }
            catch (Exception ex)
            {
                return Result<Currency>.BadRequest($"Failed to validate currency: {ex.Message}");
            }
        }

        private async Task<Result<SavingsAccountDto>> ExecuteUpdateAsync(SavingsAccount account, SavingsAccountEditDto updateData, Currency currency)
        {
            try
            {
                // Apply updates
                account.UserId = updateData.UserId;
                account.CurrencyId = updateData.CurrencyId;
                account.InterestRate = updateData.InterestRate;

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
                return Result<SavingsAccountDto>.BadRequest($"Failed to update savings account: {ex.Message}");
            }
        }
    }
}
