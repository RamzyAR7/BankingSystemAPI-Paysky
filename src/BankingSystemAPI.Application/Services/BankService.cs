using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BankingSystemAPI.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Services
{
    public class BankService : IBankService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        public BankService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<BankSimpleResDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var skip = (pageNumber - 1) * pageSize;

            var (banks, total) = await _uow.BankRepository.GetPagedAsync(b => true, pageSize, skip, b => b.Id, "ASC");
            return _mapper.Map<List<BankSimpleResDto>>(banks);
        }

        public async Task<BankResDto> GetByIdAsync(int id)
        {
            // load bank with users and their accounts using include builder
            var bank = await _uow.BankRepository.FindWithIncludeBuilderAsync(
                b => b.Id == id,
                q => q.Include(bk => bk.ApplicationUsers)
            );
            if (bank == null) return null;
            var dto = _mapper.Map<BankResDto>(bank);
            if (bank.ApplicationUsers != null)
                dto.Users = _mapper.Map<List<UserResDto>>(bank.ApplicationUsers);
            return dto;
        }

        public async Task<BankResDto> GetByNameAsync(string name)
        {
            var bank = await _uow.BankRepository.FindWithIncludeBuilderAsync(
                b => b.Name == name,
                q => q.Include(bk => bk.ApplicationUsers)
            );
            if (bank == null) return null;
            var dto = _mapper.Map<BankResDto>(bank);
            if (bank.ApplicationUsers != null)
                dto.Users = _mapper.Map<List<UserResDto>>(bank.ApplicationUsers);
            return dto;
        }

        public async Task<BankResDto> CreateAsync(BankReqDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name))
                throw new BadRequestException("Bank name is required.");

            var normalized = dto.Name.Trim();
            var normalizedLower = normalized.ToLowerInvariant();

            // Check duplicate by name (case-insensitive check)
            var existingCount = await _uow.BankRepository.CountAsync(b => b.Name.ToLower() == normalizedLower);
            if (existingCount > 0)
                throw new BadRequestException("A bank with the same name already exists.");

            var entity = _mapper.Map<Bank>(dto);
            entity.Name = normalized;
            entity.CreatedAt = DateTime.UtcNow;

            try
            {
                await _uow.BankRepository.AddAsync(entity);
                await _uow.SaveAsync();
            }
            catch (DbUpdateException)
            {
                // Could be unique constraint violation from DB - convert to friendly error
                throw new BadRequestException("A bank with the same name already exists.");
            }

            return _mapper.Map<BankResDto>(entity);
        }

        public async Task<BankResDto> UpdateAsync(int id, BankEditDto dto)
        {
            var bank = await _uow.BankRepository.GetByIdAsync(id);
            if (bank == null) return null;
            bank.Name = dto.Name;
            await _uow.BankRepository.UpdateAsync(bank);
            await _uow.SaveAsync();
            return _mapper.Map<BankResDto>(bank);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var bank = await _uow.BankRepository.GetByIdAsync(id);
            if (bank == null) return false;

            // Prevent deleting a bank that still has users
            var hasUsers = await _uow.UserRepository.AnyAsync(u => u.BankId == id);
            if (hasUsers)
                throw new BadRequestException("Cannot delete bank that has existing users.");

            await _uow.BankRepository.DeleteAsync(bank);
            await _uow.SaveAsync();
            return true;
        }

        public async Task SetBankActiveStatusAsync(int id, bool isActive)
        {
            var bank = await _uow.BankRepository.GetByIdAsync(id);
            if (bank == null) throw new System.Exception($"Bank with ID '{id}' not found.");
            bank.IsActive = isActive;
            await _uow.BankRepository.UpdateAsync(bank);
            await _uow.SaveAsync();
        }
    }
}
