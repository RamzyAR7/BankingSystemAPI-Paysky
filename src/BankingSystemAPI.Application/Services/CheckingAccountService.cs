using AutoMapper;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using Microsoft.EntityFrameworkCore;


namespace BankingSystemAPI.Application.Services
{
    public class CheckingAccountService : IAccountTypeService<CheckingAccount, CheckingAccountReqDto, CheckingAccountEditDto, CheckingAccountDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public CheckingAccountService(IUnitOfWork unitOfWork, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<IEnumerable<CheckingAccountDto>> GetAccountsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;
            var spec = new Specification<Account>(a => a is CheckingAccount)
                .ApplyPaging(skip, pageSize)
                .AddInclude(a => a.Currency);
            var accounts = await _unitOfWork.AccountRepository.ListAsync(spec);
            var dtosAll = _mapper.Map<IEnumerable<CheckingAccountDto>>(accounts.OfType<CheckingAccount>());
            return dtosAll;
        }

        public async Task<CheckingAccountDto> CreateAccountAsync(CheckingAccountReqDto reqDto)
        {
            // Authorization: ensure acting user can create account for target user
            if (_accountAuth != null)
                await _accountAuth.CanCreateAccountForUserAsync(reqDto.UserId);

            // Validate currency exists
            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");
            if (!currency.IsActive)
                throw new BadRequestException("Cannot create account with inactive currency.");

            // Ensure target user exists via repository
            var user = await _unitOfWork.UserRepository.FindWithIncludesAsync(u => u.Id == reqDto.UserId, new Expression<Func<ApplicationUser, object>>[] { u => u.Accounts, u => u.Bank }, true);
            if (user == null)
                throw new NotFoundException($"User with ID '{reqDto.UserId}' not found.");
            if (!user.IsActive)
                throw new BadRequestException("Cannot create account for inactive user.");

            // Ensure user has a role assigned
            var targetRole = await _unitOfWork.RoleRepository.GetRoleByUserIdAsync(reqDto.UserId);
            if (targetRole == null || string.IsNullOrWhiteSpace(targetRole.Name))
                throw new BadRequestException("Cannot create account for a user that has no role assigned. Assign a role first.");

            var newAccount = _mapper.Map<CheckingAccount>(reqDto);
            newAccount.AccountNumber = $"CHK-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            newAccount.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.AccountRepository.AddAsync(newAccount);
            await _unitOfWork.SaveAsync();

            // Attach Currency navigation so response contains CurrencyCode
            newAccount.Currency = currency;

            return _mapper.Map<CheckingAccountDto>(newAccount);
        }

        public async Task<CheckingAccountDto> UpdateAccountAsync(int accountId, CheckingAccountEditDto reqDto)
        {
            // Authorization: ensure acting user can access the account to update
            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            var spec = new Specification<Account>(a => a.Id == accountId && a is CheckingAccount);
            var account = await _unitOfWork.AccountRepository.GetAsync(spec);
            if (account is not CheckingAccount checkingAccount)
                throw new AccountNotFoundException($"Checking Account with ID '{accountId}' not found.");

            // Validate currency exists
            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");

            // Map only allowed edit fields (no balance change)
            checkingAccount.UserId = reqDto.UserId;
            checkingAccount.CurrencyId = reqDto.CurrencyId;
            checkingAccount.OverdraftLimit = reqDto.OverdraftLimit;

            await _unitOfWork.AccountRepository.UpdateAsync(checkingAccount);
            await _unitOfWork.SaveAsync();

            checkingAccount.Currency = currency;

            return _mapper.Map<CheckingAccountDto>(checkingAccount);
        }
    }
}