using AutoMapper;
using AutoMapper.QueryableExtensions;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Application.Interfaces.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using BankingSystemAPI.Application.Specifications;

namespace BankingSystemAPI.Application.Services
{
    public class SavingsAccountService : ISavingsAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public SavingsAccountService(IUnitOfWork unitOfWork, IMapper mapper, IAccountAuthorizationService? accountAuth = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<IEnumerable<SavingsAccountDto>> GetAccountsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;
            var spec = new Specification<Account>(a => a is SavingsAccount)
                .ApplyPaging(skip, pageSize)
                .AddInclude(a => a.Currency);
            var accounts = await _unitOfWork.AccountRepository.ListAsync(spec);
            var dtosMapped = _mapper.Map<IEnumerable<SavingsAccountDto>>(accounts.OfType<SavingsAccount>());
            return dtosMapped;
        }

        public async Task<SavingsAccountDto> CreateAccountAsync(SavingsAccountReqDto reqDto)
        {
            if (_accountAuth != null)
                await _accountAuth.CanCreateAccountForUserAsync(reqDto.UserId);

            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");
            if (!currency.IsActive)
                throw new BadRequestException("Cannot create account with inactive currency.");

            var user = await _unitOfWork.UserRepository.FindWithIncludesAsync(u => u.Id == reqDto.UserId, new Expression<Func<ApplicationUser, object>>[] { u => u.Accounts, u => u.Bank });
            if (user == null)
                throw new NotFoundException($"User with ID '{reqDto.UserId}' not found.");
            if (!user.IsActive)
                throw new BadRequestException("Cannot create account for inactive user.");

            var targetRole = await _unitOfWork.RoleRepository.GetRoleByUserIdAsync(reqDto.UserId);
            if (targetRole == null || string.IsNullOrWhiteSpace(targetRole.Name))
                throw new BadRequestException("Cannot create account for a user that has no role assigned. Assign a role first.");

            var newAccount = _mapper.Map<SavingsAccount>(reqDto);
            newAccount.AccountNumber = $"SAV-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            newAccount.CreatedDate = DateTime.UtcNow;

            await _unitOfWork.AccountRepository.AddAsync(newAccount);
            await _unitOfWork.SaveAsync();

            newAccount.Currency = currency;

            return _mapper.Map<SavingsAccountDto>(newAccount);
        }

        public async Task<SavingsAccountDto> UpdateAccountAsync(int accountId, SavingsAccountEditDto reqDto)
        {
            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            var spec = new Specification<Account>(a => a.Id == accountId && a is SavingsAccount);
            var account = await _unitOfWork.AccountRepository.GetAsync(spec);
            if (account is not SavingsAccount savingsAccount)
                throw new AccountNotFoundException($"Savings Account with ID '{accountId}' not found.");

            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");

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
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var take = Math.Max(1, pageSize);
            var skip = (Math.Max(1, pageNumber) - 1) * take;
            var orderBy = (Expression<Func<InterestLog, object>>)(l => l.Timestamp);

            // Build account query for savings accounts
            var accountQuery = _unitOfWork.AccountRepository.Table.Where(a => a is SavingsAccount).Include(a => a.User).AsQueryable();

            if (_accountAuth != null)
            {
                // Let auth filter the account query in DB and return composable query
                accountQuery = await _accountAuth.FilterAccountsQueryAsync(accountQuery);
            }

            // If no accounts match, return empty result to avoid composing a contains-subquery that may fail
            if (!await accountQuery.AnyAsync())
            {
                return (Enumerable.Empty<InterestLogDto>(), 0);
            }

            // Materialize account IDs to a simple list to avoid EF attempting to translate a complex subquery
            var accountIds = await accountQuery.Select(a => a.Id).ToListAsync();
            if (accountIds == null || accountIds.Count == 0)
            {
                return (Enumerable.Empty<InterestLogDto>(), 0);
            }

            // Compose interest log predicate using in-memory id list
            Expression<Func<InterestLog, bool>> predicate = l => accountIds.Contains(l.SavingsAccountId);

            var (items, total) = await _unitOfWork.InterestLogRepository.GetPagedAsync(predicate, take, skip, orderBy, "DESC", new[] { (Expression<Func<InterestLog, object>>)(l => l.SavingsAccount) });
            var dtos = _mapper.Map<IEnumerable<InterestLogDto>>(items);
            return (dtos, total);
        }

        public async Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetInterestLogsByAccountIdAsync(int accountId, int pageNumber, int pageSize)
        {
            if (accountId <= 0) throw new BadRequestException("Invalid account id.");
            if (_accountAuth != null)
            {
                try
                {
                    await _accountAuth.CanViewAccountAsync(accountId);
                }
                catch (ForbiddenException)
                {
                    throw new ForbiddenException($"Not authorized to view interest logs for account {accountId}.");
                }
            }

            Expression<Func<InterestLog, bool>> predicate = l => l.SavingsAccountId == accountId;
            var take = Math.Max(1, pageSize);
            var skip = (Math.Max(1, pageNumber) - 1) * take;
            var orderBy = (Expression<Func<InterestLog, object>>)(l => l.Timestamp);

            var (items, total) = await _unitOfWork.InterestLogRepository.GetPagedAsync(predicate, take, skip, orderBy, "DESC", new[] { (Expression<Func<InterestLog, object>>)(l => l.SavingsAccount) });
            var dtos = _mapper.Map<IEnumerable<InterestLogDto>>(items);
            return (dtos, total);
        }
    }
}
