using AutoMapper;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Services
{
    public class SavingsAccountService : ISavingsAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IBankAuthorizationHelper? _bankAuth;

        public SavingsAccountService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService, IBankAuthorizationHelper? bankAuth = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _bankAuth = bankAuth;
        }

        public async Task<IEnumerable<SavingsAccountDto>> GetAccountsAsync(int pageNumber, int pageSize)
        {
            var accounts = await _unitOfWork.AccountRepository.GetAccountsByTypeAsync<SavingsAccount>(pageNumber, pageSize);

            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterAccountsAsync(accounts.Cast<Account>());
                var filteredSavings = filtered.OfType<SavingsAccount>().ToList();
                accounts = filteredSavings;
            }

            return _mapper.Map<IEnumerable<SavingsAccountDto>>(accounts);
        }

        public async Task<SavingsAccountDto> CreateAccountAsync(SavingsAccountReqDto reqDto)
        {
            if (_bankAuth != null)
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

            var newAccount = _mapper.Map<SavingsAccount>(reqDto);
            newAccount.AccountNumber = $"SAV-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            newAccount.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.AccountRepository.AddAsync(newAccount);
            await _unitOfWork.SaveAsync();

            // Attach Currency navigation so response contains CurrencyCode
            newAccount.Currency = currency;

            return _mapper.Map<SavingsAccountDto>(newAccount);
        }

        public async Task<SavingsAccountDto> UpdateAccountAsync(int accountId, SavingsAccountEditDto reqDto)
        {
            // Authorization: ensure acting user can access the account to update
            if (_bankAuth != null)
                await _bankAuth.EnsureCanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
            if (account is not SavingsAccount savingsAccount)
                throw new AccountNotFoundException($"Savings Account with ID '{accountId}' not found.");

            // Validate currency exists
            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");

            // Map only allowed edit fields
            savingsAccount.UserId = reqDto.UserId;
            savingsAccount.CurrencyId = reqDto.CurrencyId;
            savingsAccount.InterestRate = reqDto.InterestRate;
            savingsAccount.InterestType = reqDto.InterestType;

            await _unitOfWork.AccountRepository.UpdateAsync(savingsAccount);
            await _unitOfWork.SaveAsync();

            savingsAccount.Currency = currency;

            return _mapper.Map<SavingsAccountDto>(savingsAccount);
        }

        public async Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetAllInterestLogsAsync(int pageNumber, int pageSize)
        {
            var take = Math.Max(1, pageSize);
            var skip = (Math.Max(1, pageNumber) - 1) * take;

            // Load all logs with SavingsAccount navigation so filtering can decide which to include
            var allLogs = (await _unitOfWork.InterestLogRepository.FindAllAsync(null, int.MaxValue, 0, null, "DESC", new[] { "SavingsAccount" }))
                .ToList();


            var accounts = allLogs.Select(l => l.SavingsAccount).Where(a => a != null).GroupBy(a => a.Id).Select(g => g.First()).ToList();
            if (_bankAuth != null)
            {
                var filteredAccounts = (await _bankAuth.FilterAccountsAsync(accounts)).ToList();
                var allowedIds = new HashSet<int>(filteredAccounts.Select(a => a.Id));
                allLogs = allLogs.Where(l => l.SavingsAccountId != 0 && allowedIds.Contains(l.SavingsAccountId)).ToList();
            }
            else
            {
                var allowedIds = new HashSet<int>(accounts.Select(a => a.Id));
                allLogs = allLogs.Where(l => l.SavingsAccountId != 0 && allowedIds.Contains(l.SavingsAccountId)).ToList();
            }
            
            var total = allLogs.Count;
            var page = allLogs.Skip(skip).Take(take).ToList();
            var dtos = _mapper.Map<IEnumerable<InterestLogDto>>(page);
            return (dtos, total);
        }

        public async Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetInterestLogsByAccountIdAsync(int accountId, int pageNumber, int pageSize)
        {
            if (accountId <= 0) throw new BadRequestException("Invalid account id.");

            var take = Math.Max(1, pageSize);
            var skip = (Math.Max(1, pageNumber) - 1) * take;

            // Load logs for the specific account with navigation
            var logs = (await _unitOfWork.InterestLogRepository.FindAllAsync(l => l.SavingsAccountId == accountId, int.MaxValue, 0, null, "DESC", new[] { "SavingsAccount" })).ToList();

            var account = await _unitOfWork.AccountRepository.FindAsync(a => a.Id == accountId, new[] { "User" });
            if (account == null) return (Enumerable.Empty<InterestLogDto>(), 0);
            bool allowed;
            if (_bankAuth != null)
                allowed = (await _bankAuth.FilterAccountsAsync(new[] { account })).Any();
            else
                allowed = true;

            if (!allowed) return (Enumerable.Empty<InterestLogDto>(), 0);
            
            var total = logs.Count;
            var page = logs.Skip(skip).Take(take).ToList();
            var dtos = _mapper.Map<IEnumerable<InterestLogDto>>(page);
            return (dtos, total);
        }
    }
}