using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
            var banks = await _uow.BankRepository.FindAllAsync(b => true, take: pageSize, skip: (pageNumber - 1) * pageSize, orderBy: b => b.Id, orderByDirection: "ASC");
            return _mapper.Map<List<BankSimpleResDto>>(banks);
        }

        public async Task<BankResDto> GetByIdAsync(int id)
        {
            var banks = await _uow.BankRepository.FindAllAsync(b => b.Id == id, new[] { "ApplicationUsers.Accounts" });
            var bank = banks.FirstOrDefault();
            if (bank == null) return null;
            var dto = _mapper.Map<BankResDto>(bank);
            if (bank.ApplicationUsers != null)
                dto.Users = _mapper.Map<List<UserResDto>>(bank.ApplicationUsers);
            return dto;
        }

        public async Task<BankResDto> GetByNameAsync(string name)
        {
            var banks = await _uow.BankRepository.FindAllAsync(b => b.Name == name, new[] { "ApplicationUsers.Accounts" });
            var bank = banks.FirstOrDefault();
            if (bank == null) return null;
            var dto = _mapper.Map<BankResDto>(bank);
            if (bank.ApplicationUsers != null)
                dto.Users = _mapper.Map<List<UserResDto>>(bank.ApplicationUsers);
            return dto;
        }

        public async Task<BankResDto> CreateAsync(BankReqDto dto)
        {
            var entity = _mapper.Map<Domain.Entities.Bank>(dto);
            entity.CreatedAt = System.DateTime.UtcNow;
            await _uow.BankRepository.AddAsync(entity);
            await _uow.SaveAsync();
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
