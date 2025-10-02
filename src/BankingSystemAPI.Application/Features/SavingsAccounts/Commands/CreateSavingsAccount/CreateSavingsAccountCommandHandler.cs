using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount
{
    /// <summary>
    /// Savings account creation handler with validation for backward compatibility with tests
    /// </summary>
    public class CreateSavingsAccountCommandHandler : ICommandHandler<CreateSavingsAccountCommand, SavingsAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public CreateSavingsAccountCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<SavingsAccountDto>> Handle(CreateSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;

            // Authorization
            if (_accountAuth != null)
            {
                await _accountAuth.CanCreateAccountForUserAsync(req.UserId);
            }

            // Validate currency (backward compatibility)
            var currency = await _uow.CurrencyRepository.GetByIdAsync(req.CurrencyId);
            if (currency == null) 
                return Result<SavingsAccountDto>.Failure(new[] { "Currency not found." });
            if (!currency.IsActive) 
                return Result<SavingsAccountDto>.Failure(new[] { "Cannot create account with inactive currency." });

            // Validate user (backward compatibility)
            var user = await _uow.UserRepository.FindAsync(new UserByIdSpecification(req.UserId));
            if (user == null) 
                return Result<SavingsAccountDto>.Failure(new[] { "User not found." });
            if (!user.IsActive) 
                return Result<SavingsAccountDto>.Failure(new[] { "Cannot create account for inactive user." });

            // Create and map entity
            var entity = _mapper.Map<SavingsAccount>(req);
            entity.AccountNumber = $"SAV-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            entity.CreatedDate = DateTime.UtcNow;

            // Persist
            await _uow.AccountRepository.AddAsync(entity);
            await _uow.SaveAsync();

            // Set navigation property for proper DTO mapping
            entity.Currency = currency;

            return Result<SavingsAccountDto>.Success(_mapper.Map<SavingsAccountDto>(entity));
        }
    }
}
