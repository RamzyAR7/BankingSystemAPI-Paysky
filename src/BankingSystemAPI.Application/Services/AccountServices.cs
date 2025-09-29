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
using BankingSystemAPI.Application.Specifications.AccountSpecification;

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

            var spec = new AccountByIdSpecification(id);
            var account = await _unitOfWork.AccountRepository.FindAsync(spec);
            if (account == null)
                throw new NotFoundException($"Account with ID '{id}' not found.");

            if (_accountAuth is not null)
                await _accountAuth.CanViewAccountAsync(id);

            return _mapper.Map<AccountDto>(account);
        }

        public async Task<AccountDto> GetAccountByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new BadRequestException("Account number is required.");

            var spec = new AccountByAccountNumberSpecification(accountNumber);
            var account = await _unitOfWork.AccountRepository.FindAsync(spec);
            if (account == null)
                throw new NotFoundException($"Account with number '{accountNumber}' not found.");

            if (_accountAuth is not null)
                await _accountAuth.CanViewAccountAsync(account.Id);

            return _mapper.Map<AccountDto>(account);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BadRequestException("User id is required.");

            var spec = new AccountsByUserIdSpecification(userId);
            var accounts = await _unitOfWork.AccountRepository.ListAsync(spec);
            return _mapper.Map<IEnumerable<AccountDto>>(accounts);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByNationalIdAsync(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
                throw new BadRequestException("National id is required.");

            var spec = new AccountsByNationalIdSpecification(nationalId);
            var accounts = await _unitOfWork.AccountRepository.ListAsync(spec);
            return _mapper.Map<IEnumerable<AccountDto>>(accounts);
        }

        public async Task DeleteAccountAsync(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid account id.");

            var spec = new AccountByIdSpecification(id);
            var account = await _unitOfWork.AccountRepository.FindAsync(spec);
            if (account == null)
                throw new NotFoundException($"Account with ID '{id}' not found.");
            if (account.Balance > 0)
                throw new BadRequestException("Cannot delete an account with a positive balance.");

            if (_accountAuth is not null)
                await _accountAuth.CanModifyAccountAsync(id, AccountModificationOperation.Delete);

            await _unitOfWork.AccountRepository.DeleteAsync(account);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAccountsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
                throw new BadRequestException("At least one account id must be provided.");

            var distinctIds = ids.Distinct().ToList();
            var spec = new AccountsByIdsSpecification(distinctIds);
            var accountsToDelete = await _unitOfWork.AccountRepository.ListAsync(spec);
            if (accountsToDelete.Count() != distinctIds.Count())
                throw new NotFoundException("One or more specified accounts could not be found.");
            if (accountsToDelete.Any(a => a.Balance > 0))
                throw new BadRequestException("Cannot delete accounts that have a positive balance.");

            foreach (var acc in accountsToDelete)
            {
                if (_accountAuth is not null)
                    await _accountAuth.CanModifyAccountAsync(acc.Id, AccountModificationOperation.Delete);
            }

            await _unitOfWork.AccountRepository.DeleteRangeAsync(accountsToDelete);
            await _unitOfWork.SaveAsync();
        }

        public async Task SetAccountActiveStatusAsync(int accountId, bool isActive)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _unitOfWork.AccountRepository.FindAsync(spec);
            if (account == null) throw new NotFoundException($"Account with ID '{accountId}' not found.");

            if (_accountAuth is not null)
                await _accountAuth.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            account.IsActive = isActive;
            await _unitOfWork.AccountRepository.UpdateAsync(account);
            await _unitOfWork.SaveAsync();
        }
    }
}
