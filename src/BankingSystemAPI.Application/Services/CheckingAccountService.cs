using AutoMapper;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using BankingSystemAPI.Domain.Constant;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Application.Interfaces.Identity;


namespace BankingSystemAPI.Application.Services
{
    public class CheckingAccountService : IAccountTypeService<CheckingAccount, CheckingAccountReqDto, CheckingAccountEditDto, CheckingAccountDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IBankAuthorizationHelper? _bankAuth;

        public CheckingAccountService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService, IBankAuthorizationHelper? bankAuth = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _bankAuth = bankAuth;
        }

        public async Task<IEnumerable<CheckingAccountDto>> GetAccountsAsync(int pageNumber, int pageSize)
        {
            // Do manual projection to DTO to avoid AutoMapper mapping entity navigation graphs (and potential cycles)
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var skip = (pageNumber - 1) * pageSize;

            Expression<Func<Account, bool>> predicate = a => a is CheckingAccount;

            var accounts = await _unitOfWork.AccountRepository.FindAllAsync(predicate, take: pageSize, skip: skip, orderBy: (Expression<Func<Account, object>>)(a => a.Id), orderByDirection: "ASC", Includes: new[] { "Currency" });

            // Apply bank-level filtering if available so callers get only allowed accounts
            var filtered = await _bankAuth.FilterAccountsAsync(accounts);
            accounts = filtered.ToList();
            

            var dtosAll = _mapper.Map<IEnumerable<CheckingAccountDto>>(accounts.OfType<CheckingAccount>());
            return dtosAll;
        }

        public async Task<CheckingAccountDto> CreateAccountAsync(CheckingAccountReqDto reqDto)
        {
            // Authorization: ensure acting user can create account for target user
            await _bankAuth.EnsureCanCreateAccountForUserAsync(reqDto.UserId);

            // Validate currency exists
            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");
            if (!currency.IsActive)
                throw new BadRequestException("Cannot create account with inactive currency.");

            // Ensure target user exists
            var user = await _userManager.FindByIdAsync(reqDto.UserId);
            if (user == null)
                throw new NotFoundException($"User with ID '{reqDto.UserId}' not found.");
            if (!user.IsActive)
                throw new BadRequestException("Cannot create account for inactive user.");

            // Cannot create account for user without any role
            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles == null || !targetRoles.Any())
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
            await _bankAuth.EnsureCanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
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