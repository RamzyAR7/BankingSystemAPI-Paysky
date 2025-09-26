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
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Authorization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService? _accountAuth;

        public AccountService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAccountAuthorizationService? accountAuth = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<AccountDto> GetAccountByIdAsync(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid account id.");

            // include InterestLogs (for savings), User and Currency using include builder to avoid expression-on-derived-type issues
            var account = await _unitOfWork.AccountRepository.FindWithIncludeBuilderAsync(a => a.Id == id, q => q.Include("InterestLogs").Include(a => a.User).Include(a => a.Currency));
            if (account == null)
                throw new NotFoundException($"Account with ID '{id}' not found.");

            if (_accountAuth != null)
                await _accountAuth.CanViewAccountAsync(id);

            return _mapper.Map<AccountDto>(account);
        }

        public async Task<AccountDto> GetAccountByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new BadRequestException("Account number is required.");

            var account = await _unitOfWork.AccountRepository.FindWithIncludeBuilderAsync(a => a.AccountNumber == accountNumber, q => q.Include("InterestLogs").Include(a => a.User).Include(a => a.Currency));
            if (account == null)
                throw new NotFoundException($"Account with number '{accountNumber}' not found.");

            if (_accountAuth != null)
                await _accountAuth.CanViewAccountAsync(account.Id);

            return _mapper.Map<AccountDto>(account);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BadRequestException("User id is required.");

            // Get repository-level query (includes handled in infra)
            var query = _unitOfWork.AccountRepository.QueryByUserId(userId);

            IEnumerable<Account> accounts;
            if (_accountAuth != null)
            {
                var filteredQuery = await _accountAuth.FilterAccountsQueryAsync(query);
                var result = await _unitOfWork.AccountRepository.GetFilteredAccountsAsync(filteredQuery, 1, int.MaxValue);
                accounts = result.Accounts;
            }
            else
            {
                var result = await _unitOfWork.AccountRepository.GetFilteredAccountsAsync(query, 1, int.MaxValue);
                accounts = result.Accounts;
            }

            return _mapper.Map<IEnumerable<AccountDto>>(accounts);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByNationalIdAsync(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
                throw new BadRequestException("National id is required.");

            var query = _unitOfWork.AccountRepository.QueryByNationalId(nationalId);

            IEnumerable<Account> accounts;
            if (_accountAuth != null)
            {
                var filteredQuery = await _accountAuth.FilterAccountsQueryAsync(query);
                var result = await _unitOfWork.AccountRepository.GetFilteredAccountsAsync(filteredQuery, 1, int.MaxValue);
                accounts = result.Accounts;
            }
            else
            {
                var result = await _unitOfWork.AccountRepository.GetFilteredAccountsAsync(query, 1, int.MaxValue);
                accounts = result.Accounts;
            }

            return _mapper.Map<IEnumerable<AccountDto>>(accounts);
        }

        public async Task DeleteAccountAsync(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid account id.");

            var account = await _unitOfWork.AccountRepository.GetByIdAsync(id);
            if (account == null)
                throw new NotFoundException($"Account with ID '{id}' not found.");
            if (account.Balance > 0)
                throw new BadRequestException("Cannot delete an account with a positive balance.");

            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(id, AccountModificationOperation.Delete);

            await _unitOfWork.AccountRepository.DeleteAsync(account);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAccountsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
                throw new BadRequestException("At least one account id must be provided.");

            var distinctIds = ids.Distinct().ToList();
            var accountsToDelete = await _unitOfWork.AccountRepository.FindAllWithIncludesAsync(a => distinctIds.Contains(a.Id), (Expression<Func<Account, object>>[])null);
            if (accountsToDelete.Count() != distinctIds.Count())
                throw new NotFoundException("One or more specified accounts could not be found.");
            if (accountsToDelete.Any(a => a.Balance > 0))
                throw new BadRequestException("Cannot delete accounts that have a positive balance.");

            foreach (var acc in accountsToDelete)
            {
                if (_accountAuth != null)
                    await _accountAuth.CanModifyAccountAsync(acc.Id, AccountModificationOperation.Delete);
            }

            await _unitOfWork.AccountRepository.DeleteRangeAsync(accountsToDelete);
            await _unitOfWork.SaveAsync();
        }


        public async Task SetAccountActiveStatusAsync(int accountId, bool isActive)
        {
            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
            if (account == null) throw new NotFoundException($"Account with ID '{accountId}' not found.");

            if (_accountAuth != null)
                await _accountAuth.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            account.IsActive = isActive;
            await _unitOfWork.AccountRepository.UpdateAsync(account);
            await _unitOfWork.SaveAsync();
        }
    }
}
