using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount
{
    /// <summary>
    /// Checking account creation handler with validation for backward compatibility with tests
    /// </summary>
    public class CreateCheckingAccountCommandHandler : ICommandHandler<CreateCheckingAccountCommand, CheckingAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public CreateCheckingAccountCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<CheckingAccountDto>> Handle(CreateCheckingAccountCommand request, CancellationToken cancellationToken)
        {
            var reqDto = request.Req;

            // Authorization
            if (_accountAuth != null)
            {
                await _accountAuth.CanCreateAccountForUserAsync(reqDto.UserId);
            }

            // Validate currency (backward compatibility)
            var currency = await _uow.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null) 
                return Result<CheckingAccountDto>.Failure(new[] { $"Currency with ID '{reqDto.CurrencyId}' not found." });
            if (!currency.IsActive) 
                return Result<CheckingAccountDto>.Failure(new[] { "Cannot create account with inactive currency." });

            // Validate user (backward compatibility)
            var user = await _uow.UserRepository.FindAsync(new UserByIdSpecification(reqDto.UserId));
            if (user == null) 
                return Result<CheckingAccountDto>.Failure(new[] { $"User with ID '{reqDto.UserId}' not found." });
            if (!user.IsActive) 
                return Result<CheckingAccountDto>.Failure(new[] { "Cannot create account for inactive user." });

            // Validate user role (backward compatibility)
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(reqDto.UserId);
            if (targetRole == null || string.IsNullOrWhiteSpace(targetRole.Name)) 
                return Result<CheckingAccountDto>.Failure(new[] { "Cannot create account for a user that has no role assigned. Assign a role first." });

            // Create and map entity
            var entity = _mapper.Map<CheckingAccount>(reqDto);
            entity.AccountNumber = $"CHK-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            entity.CreatedDate = DateTime.UtcNow;

            // Persist
            await _uow.AccountRepository.AddAsync(entity);
            await _uow.SaveAsync();

            // Set navigation property for proper DTO mapping
            entity.Currency = currency;

            return Result<CheckingAccountDto>.Success(_mapper.Map<CheckingAccountDto>(entity));
        }
    }
}
