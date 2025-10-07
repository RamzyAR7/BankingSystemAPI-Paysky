#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount
{
    /// <summary>
    /// Checking account creation handler with validation for backward compatibility with tests
    /// </summary>
    public class CreateCheckingAccountCommandHandler : ICommandHandler<CreateCheckingAccountCommand, CheckingAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;

        public CreateCheckingAccountCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<CheckingAccountDto>> Handle(CreateCheckingAccountCommand request, CancellationToken cancellationToken)
        {
            var reqDto = request.Req;

            // Authorization
            var authResult = await _accountAuth.CanCreateAccountForUserAsync(reqDto.UserId);
            if (authResult.IsFailure)
                return Result<CheckingAccountDto>.Failure(authResult.ErrorItems);

            // Validate currency using extensions
            var currencyResult = await ValidateCurrencyAsync(reqDto.CurrencyId);
            if (!currencyResult) // Using implicit bool operator!
                return Result<CheckingAccountDto>.Failure(currencyResult.ErrorItems);

            // Validate user using extensions
            var userResult = await ValidateUserAsync(reqDto.UserId);
            if (!userResult) // Using implicit bool operator!
                return Result<CheckingAccountDto>.Failure(userResult.ErrorItems);

            // Validate user role using extensions
            var roleValidationResult = await ValidateUserRoleAsync(reqDto.UserId);
            if (!roleValidationResult) // Using implicit bool operator!
                return Result<CheckingAccountDto>.Failure(roleValidationResult.ErrorItems);

            // Chain successful validations and create account
            var accountResult = currencyResult
                .Bind(currency => userResult
                    .Bind(user => CreateAccountEntity(reqDto, currency)));

            return await accountResult.MapAsync(async entity => await PersistAndMapAsync(entity));
        }

        private async Task<Result<Currency>> ValidateCurrencyAsync(int currencyId)
        {
            var currency = await _uow.CurrencyRepository.GetByIdAsync(currencyId);
            return currency.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Currency", currencyId))
                .Bind(c => !c.IsActive
                    ? Result<Currency>.BadRequest(ApiResponseMessages.Validation.CurrencyInactive)
                    : Result<Currency>.Success(c));
        }

        private async Task<Result<ApplicationUser>> ValidateUserAsync(string userId)
        {
            var user = await _uow.UserRepository.FindAsync(new UserByIdSpecification(userId));
            return user.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "User", userId))
                .Bind(u => !u.IsActive
                    ? Result<ApplicationUser>.BadRequest(ApiResponseMessages.Validation.AccountNotFound)
                    : Result<ApplicationUser>.Success(u));
        }

        private async Task<Result> ValidateUserRoleAsync(string userId)
        {
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(userId);
            var roleName = targetRole?.Name;
            return roleName.ToResult(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "Role"))
                .Bind<string>(_ => Result.Success());
        }

        private Result<CheckingAccount> CreateAccountEntity(CheckingAccountReqDto reqDto, Currency currency)
        {
            var entity = _mapper.Map<CheckingAccount>(reqDto);
            entity.AccountNumber = $"CHK-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            entity.CreatedDate = DateTime.UtcNow;
            entity.Currency = currency;

            return Result<CheckingAccount>.Success(entity);
        }

        private async Task<CheckingAccountDto> PersistAndMapAsync(CheckingAccount entity)
        {
            await _uow.AccountRepository.AddAsync(entity);
            await _uow.SaveAsync();
            return _mapper.Map<CheckingAccountDto>(entity);
        }
    }
}

