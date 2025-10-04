using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount
{
    /// <summary>
    /// Savings account creation handler with ResultExtensions for functional validation
    /// </summary>
    public class CreateSavingsAccountCommandHandler : ICommandHandler<CreateSavingsAccountCommand, SavingsAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;
        private readonly ILogger<CreateSavingsAccountCommandHandler> _logger;

        public CreateSavingsAccountCommandHandler(
            IUnitOfWork uow, 
            IMapper mapper, 
            ILogger<CreateSavingsAccountCommandHandler> logger,
            IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
            _logger = logger;
        }

        public async Task<Result<SavingsAccountDto>> Handle(CreateSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Chain authorization, validation, and creation using ResultExtensions
            var authResult = await ValidateAuthorizationAsync(req.UserId);
            if (authResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(authResult.Errors);

            var currencyResult = await ValidateCurrencyAsync(req.CurrencyId);
            if (currencyResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(currencyResult.Errors);

            var userResult = await ValidateUserAsync(req.UserId);
            if (userResult.IsFailure)
                return Result<SavingsAccountDto>.Failure(userResult.Errors);

            var createResult = await CreateSavingsAccountAsync(req, currencyResult.Value!);
            
            // Add side effects using ResultExtensions
            createResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Savings account created successfully for user: {UserId}, Currency: {CurrencyCode}", 
                        req.UserId, currencyResult.Value!.Code);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Savings account creation failed for user: {UserId}. Errors: {Errors}",
                        req.UserId, string.Join(", ", errors));
                });

            return createResult;
        }

        private async Task<Result> ValidateAuthorizationAsync(string userId)
        {
            try
            {
                var authResult = await _accountAuth.CanCreateAccountForUserAsync(userId);
                return authResult.IsSuccess 
                    ? Result.Success() 
                    : Result.Failure(authResult.Errors);
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
            }
        }

        private async Task<Result<Currency>> ValidateCurrencyAsync(int currencyId)
        {
            var currency = await _uow.CurrencyRepository.GetByIdAsync(currencyId);
            
            return currency.ToResult($"Currency with ID '{currencyId}' not found.")
                .Bind(c => c.IsActive 
                    ? Result<Currency>.Success(c) 
                    : Result<Currency>.BadRequest("Cannot create account with inactive currency."));
        }

        private async Task<Result<ApplicationUser>> ValidateUserAsync(string userId)
        {
            var user = await _uow.UserRepository.FindAsync(new UserByIdSpecification(userId));
            
            return user.ToResult($"User with ID '{userId}' not found.")
                .Bind(u => u.IsActive 
                    ? Result<ApplicationUser>.Success(u) 
                    : Result<ApplicationUser>.BadRequest("Cannot create account for inactive user."));
        }

        private async Task<Result<SavingsAccountDto>> CreateSavingsAccountAsync(SavingsAccountReqDto req, Currency currency)
        {
            try
            {
                // Create and map entity
                var entity = _mapper.Map<SavingsAccount>(req);
                entity.AccountNumber = $"SAV-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                entity.CreatedDate = DateTime.UtcNow;

                // Persist
                await _uow.AccountRepository.AddAsync(entity);
                await _uow.SaveAsync();

                // Set navigation property for proper DTO mapping
                entity.Currency = currency;

                var resultDto = _mapper.Map<SavingsAccountDto>(entity);
                return Result<SavingsAccountDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                return Result<SavingsAccountDto>.BadRequest($"Failed to create savings account: {ex.Message}");
            }
        }
    }
}
