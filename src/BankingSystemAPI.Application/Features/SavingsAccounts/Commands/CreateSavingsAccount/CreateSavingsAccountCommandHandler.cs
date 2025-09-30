using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount
{
    /// <summary>
    /// Handler for creating a new savings account.
    /// </summary>
    /// <remarks>
    /// Validates currency, user active status and authorization; sets generated
    /// account number and creation timestamp; persists the account and returns
    /// mapped DTO with currency navigation populated.
    /// </remarks>
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

        /// <summary>
        /// Handles creation of a savings account.
        /// </summary>
        public async Task<Result<SavingsAccountDto>> Handle(CreateSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var req = request.Req;
            if (_accountAuth != null)
                await _accountAuth.CanCreateAccountForUserAsync(req.UserId);

            var currency = await _uow.CurrencyRepository.GetByIdAsync(req.CurrencyId);
            if (currency == null) return Result<SavingsAccountDto>.Failure(new[] { "Currency not found." });
            if (!currency.IsActive) return Result<SavingsAccountDto>.Failure(new[] { "Cannot create account with inactive currency." });

            var user = await _uow.UserRepository.FindAsync(new UserByIdSpecification(req.UserId));
            if (user == null) return Result<SavingsAccountDto>.Failure(new[] { "User not found." });
            if (!user.IsActive) return Result<SavingsAccountDto>.Failure(new[] { "Cannot create account for inactive user." });

            var entity = _mapper.Map<SavingsAccount>(req);
            entity.AccountNumber = $"SAV-{Guid.NewGuid().ToString().Substring(0,8).ToUpper()}";
            entity.CreatedDate = DateTime.UtcNow;

            await _uow.AccountRepository.AddAsync(entity);
            await _uow.SaveAsync();

            entity.Currency = currency;
            return Result<SavingsAccountDto>.Success(_mapper.Map<SavingsAccountDto>(entity));
        }
    }
}
