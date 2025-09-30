using AutoMapper;
using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount
{
    /// <summary>
    /// Handles updating a checking account's editable fields (user, currency, overdraft limit).
    /// </summary>
    public class UpdateCheckingAccountCommandHandler : ICommandHandler<UpdateCheckingAccountCommand, CheckingAccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public UpdateCheckingAccountCommandHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        /// <summary>
        /// Handles the update of a checking account. Validates permissions, finds the entity,
        /// applies changes, persists them and returns the updated DTO.
        /// </summary>
        public async Task<Result<CheckingAccountDto>> Handle(UpdateCheckingAccountCommand request, CancellationToken cancellationToken)
        {
            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(request.Id, AccountModificationOperation.Edit);

            var spec = new CheckingAccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account is not CheckingAccount chk) return Result<CheckingAccountDto>.Failure(new[] { "Checking account not found." });

            // Ensure the provided UserId actually owns this account — do not allow ownership reassignment here
            if (!string.Equals(request.Req.UserId, chk.UserId, StringComparison.OrdinalIgnoreCase))
                return Result<CheckingAccountDto>.Failure(new[] { "Specified user does not own this account." });

            var currency = await _uow.CurrencyRepository.GetByIdAsync(request.Req.CurrencyId);
            if (currency == null) return Result<CheckingAccountDto>.Failure(new[] { "Currency not found." });
            if (!currency.IsActive) return Result<CheckingAccountDto>.Failure(new[] { "Cannot set account to an inactive currency." });

            chk.UserId = request.Req.UserId;
            chk.CurrencyId = request.Req.CurrencyId;
            chk.OverdraftLimit = request.Req.OverdraftLimit;

            await _uow.AccountRepository.UpdateAsync(chk);
            await _uow.SaveAsync();

            chk.Currency = currency;
            return Result<CheckingAccountDto>.Success(_mapper.Map<CheckingAccountDto>(chk));
        }
    }
}
