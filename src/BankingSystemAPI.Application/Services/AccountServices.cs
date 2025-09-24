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

namespace BankingSystemAPI.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBankAuthorizationHelper? _bankAuth;

        public AccountService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IBankAuthorizationHelper? bankAuth = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _bankAuth = bankAuth;
        }

        public async Task<AccountDto> GetAccountByIdAsync(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid account id.");

            // include InterestLogs so Savings will have its logs loaded
            var account = await _unitOfWork.AccountRepository.FindAsync(a => a.Id == id, new[] { "InterestLogs", "User", "Currency" });
            if (account == null)
                throw new NotFoundException($"Account with ID '{id}' not found.");

            await _bankAuth.EnsureCanAccessAccountAsync(id);

            return _mapper.Map<AccountDto>(account);
        }

        public async Task<AccountDto> GetAccountByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new BadRequestException("Account number is required.");

            var account = await _unitOfWork.AccountRepository.FindAsync(a => a.AccountNumber == accountNumber, new[] { "InterestLogs", "User", "Currency" });
            if (account == null)
                throw new NotFoundException($"Account with number '{accountNumber}' not found.");

            await _bankAuth.EnsureCanAccessAccountAsync(account.Id);

            return _mapper.Map<AccountDto>(account);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BadRequestException("User id is required.");

            // include InterestLogs for savings accounts
            var accounts = await _unitOfWork.AccountRepository.FindAllAsync(a => a.UserId == userId, new[] { "InterestLogs", "Currency" });

            var filtered = await _bankAuth.FilterAccountsAsync(accounts);
            accounts = filtered.ToList();        

            return _mapper.Map<IEnumerable<AccountDto>>(accounts);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByNationalIdAsync(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
                throw new BadRequestException("National id is required.");

            var accounts = await _unitOfWork.AccountRepository.FindAllAsync(a => a.User.NationalId == nationalId, new[] { "User", "InterestLogs", "Currency" });

            var filtered = await _bankAuth.FilterAccountsAsync(accounts);
            accounts = filtered.ToList();
            
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

            await _bankAuth.EnsureCanModifyAccountAsync(id, AccountModificationOperation.Delete);

            await _unitOfWork.AccountRepository.DeleteAsync(account);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAccountsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
                throw new BadRequestException("At least one account id must be provided.");

            var distinctIds = ids.Distinct().ToList();
            var accountsToDelete = await _unitOfWork.AccountRepository.FindAllAsync(a => distinctIds.Contains(a.Id), (string[])null);
            if (accountsToDelete.Count() != distinctIds.Count())
                throw new NotFoundException("One or more specified accounts could not be found.");
            if (accountsToDelete.Any(a => a.Balance > 0))
                throw new BadRequestException("Cannot delete accounts that have a positive balance.");

            foreach (var acc in accountsToDelete)
            {
                await _bankAuth.EnsureCanModifyAccountAsync(acc.Id, AccountModificationOperation.Delete);
            }
            
            await _unitOfWork.AccountRepository.DeleteRangeAsync(accountsToDelete);
            await _unitOfWork.SaveAsync();
        }

        public async Task SetAccountActiveStatusAsync(int accountId, bool isActive)
        {
            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
            if (account == null) throw new NotFoundException($"Account with ID '{accountId}' not found.");


            await _bankAuth.EnsureCanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            account.IsActive = isActive;
            await _unitOfWork.AccountRepository.UpdateAsync(account);
            await _unitOfWork.SaveAsync();
        }
    }
}
