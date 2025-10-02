using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using AutoMapper;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount
{
    /// <summary>
    /// Handles updating a savings account's editable fields (user, currency, interest rate).
    /// </summary>
    public class UpdateSavingsAccountCommandHandler : ICommandHandler<UpdateSavingsAccountCommand, SavingsAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public UpdateSavingsAccountCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        /// <summary>
        /// Handles the update of a savings account. Validates permissions, finds the entity,
        /// applies changes, persists them and returns the updated DTO.
        /// </summary>
        public async Task<Result<SavingsAccountDto>> Handle(UpdateSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(request.Id, AccountModificationOperation.Edit);

            var spec = new SavingsAccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account is not SavingsAccount sav) return Result<SavingsAccountDto>.Failure(new[] { "Savings account not found." });

            // Ensure provided UserId owns this account
            if (!string.Equals(request.Req.UserId, sav.UserId, StringComparison.OrdinalIgnoreCase))
                return Result<SavingsAccountDto>.Failure(new[] { "Specified user does not own this account." });

            var currency = await _uow.CurrencyRepository.GetByIdAsync(request.Req.CurrencyId);
            if (currency == null) return Result<SavingsAccountDto>.Failure(new[] { "Currency not found." });
            if (!currency.IsActive) return Result<SavingsAccountDto>.Failure(new[] { "Cannot set account to an inactive currency." });

            sav.UserId = request.Req.UserId;
            sav.CurrencyId = request.Req.CurrencyId;
            sav.InterestRate = request.Req.InterestRate;

            await _uow.AccountRepository.UpdateAsync(sav);
            await _uow.SaveAsync();

            sav.Currency = currency;
            return Result<SavingsAccountDto>.Success(_mapper.Map<SavingsAccountDto>(sav));
        }
    }
}
