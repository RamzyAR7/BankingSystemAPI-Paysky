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
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Application.Specifications.AccountSpecification;

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

        public async Task<IEnumerable<SavingsAccountDto>> GetAccountsAsync(int pageNumber, int pageSize, string? orderBy = null, string? orderDirection = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;

            var orderProp = string.IsNullOrWhiteSpace(orderBy) ? "CreatedDate" : orderBy;
            var orderDir = string.IsNullOrWhiteSpace(orderDirection) ? "DESC" : orderDirection;

            var spec = new PagedSpecification<Account>(a => a is SavingsAccount, skip, pageSize, orderProp, orderDir, a => a.Currency);
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

            var userSpec = new UserByIdSpecification(reqDto.UserId);
            var user = await _unitOfWork.UserRepository.FindAsync(userSpec);
            if (user == null)
                throw new NotFoundException($"User with ID '{reqDto.UserId}' not found.");
            if (!user.IsActive)
                throw new BadRequestException("Cannot create account for inactive user.");

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

            var spec = new SavingsAccountByIdSpecification(accountId);
            var account = await _unitOfWork.AccountRepository.FindAsync(spec);
            if (account is not SavingsAccount savingsAccount)
                throw new AccountNotFoundException($"Savings Account with ID '{accountId}' not found.");

            var currency = await _unitOfWork.CurrencyRepository.GetByIdAsync(reqDto.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{reqDto.CurrencyId}' not found.");

            savingsAccount.UserId = reqDto.UserId;
            savingsAccount.CurrencyId = reqDto.CurrencyId;
            savingsAccount.InterestRate = reqDto.InterestRate;

            await _unitOfWork.AccountRepository.UpdateAsync(savingsAccount);
            await _unitOfWork.SaveAsync();

            savingsAccount.Currency = currency;
            return _mapper.Map<SavingsAccountDto>(savingsAccount);
        }

        // Implement ISavingsAccountService interest log methods
        public async Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetAllInterestLogsAsync(int pageNumber, int pageSize, string? orderBy = null, string? orderDirection = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;

            // default ordering if none specified
            var orderProp = string.IsNullOrWhiteSpace(orderBy) ? "Timestamp" : orderBy;
            var orderDir = string.IsNullOrWhiteSpace(orderDirection) ? "DESC" : orderDirection;

            var spec = new PagedSpecification<InterestLog>(skip, pageSize, orderProp, orderDir, l => l.SavingsAccount);
            var (items, total) = await _unitOfWork.InterestLogRepository.GetPagedAsync(spec);

            var dtos = _mapper.Map<IEnumerable<InterestLogDto>>(items);
            return (dtos, total);
        }

        public async Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetInterestLogsByAccountIdAsync(int accountId, int pageNumber, int pageSize, string? orderBy = null, string? orderDirection = null)
        {
            if (accountId <= 0) throw new BadRequestException("Invalid account id.");
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;

            var orderProp = string.IsNullOrWhiteSpace(orderBy) ? "Timestamp" : orderBy;
            var orderDir = string.IsNullOrWhiteSpace(orderDirection) ? "DESC" : orderDirection;

            var spec = new PagedSpecification<InterestLog>(l => l.SavingsAccountId == accountId, skip, pageSize, orderProp, orderDir, l => l.SavingsAccount);
            var (items, total) = await _unitOfWork.InterestLogRepository.GetPagedAsync(spec);

            var dtos = _mapper.Map<IEnumerable<InterestLogDto>>(items);
            return (dtos, total);
        }
    }
}
