using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using AutoMapper;

namespace BankingSystemAPI.UnitTests
{
    public class InactiveFeatureTests
    {
        [Fact]
        public async Task CreateAccount_InactiveUser_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();
            var service = new CheckingAccountService(uow.Object, mapper.Object, userManagerMock.Object, currentUser.Object);
            var user = new ApplicationUser { Id = "u1", IsActive = false };
            userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new[] { "Client" });
            uow.Setup(u => u.CurrencyRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Currency { Id = 1, IsActive = true });
            var req = new CheckingAccountReqDto { UserId = "u1", CurrencyId = 1, InitialBalance = 0 };
            await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAccountAsync(req));
        }

        [Fact]
        public async Task CreateAccount_InactiveCurrency_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();
            var service = new CheckingAccountService(uow.Object, mapper.Object, userManagerMock.Object, currentUser.Object);
            var user = new ApplicationUser { Id = "u1", IsActive = true };
            userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new[] { "Client" });
            uow.Setup(u => u.CurrencyRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Currency { Id = 1, IsActive = false });
            var req = new CheckingAccountReqDto { UserId = "u1", CurrencyId = 1, InitialBalance = 0 };
            await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAccountAsync(req));
        }

        [Fact]
        public async Task Deposit_InactiveAccount_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var helper = new Mock<ITransactionHelperService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new UserManager<ApplicationUser>(userStore.Object, null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<TransactionService>>();
            var service = new TransactionService(uow.Object, mapper.Object, helper.Object, userManager, currentUser.Object, logger.Object);
            uow.Setup(u => u.AccountRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            var req = new DepositReqDto { AccountId = 1, Amount = 10 };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => service.DepositAsync(req));
        }

        [Fact]
        public async Task Withdraw_InactiveAccount_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var helper = new Mock<ITransactionHelperService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new UserManager<ApplicationUser>(userStore.Object, null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<TransactionService>>();
            var service = new TransactionService(uow.Object, mapper.Object, helper.Object, userManager, currentUser.Object, logger.Object);
            uow.Setup(u => u.AccountRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            var req = new WithdrawReqDto { AccountId = 1, Amount = 10 };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => service.WithdrawAsync(req));
        }

        [Fact]
        public async Task Transfer_InactiveAccount_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var helper = new Mock<ITransactionHelperService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new UserManager<ApplicationUser>(userStore.Object, null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<TransactionService>>();
            var service = new TransactionService(uow.Object, mapper.Object, helper.Object, userManager, currentUser.Object, logger.Object);
            // Source inactive, target active
            uow.Setup(u => u.AccountRepository.GetByIdAsync(1)).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            uow.Setup(u => u.AccountRepository.GetByIdAsync(2)).ReturnsAsync(new CheckingAccount { Id = 2, IsActive = true, Currency = new Currency { Id = 1, IsActive = true } });
            var req = new TransferReqDto { SourceAccountId = 1, TargetAccountId = 2, Amount = 10 };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => service.TransferAsync(req));
            // Source active, target inactive
            uow.Setup(u => u.AccountRepository.GetByIdAsync(1)).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = true, Currency = new Currency { Id = 1, IsActive = true } });
            uow.Setup(u => u.AccountRepository.GetByIdAsync(2)).ReturnsAsync(new CheckingAccount { Id = 2, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            req = new TransferReqDto { SourceAccountId = 1, TargetAccountId = 2, Amount = 10 };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => service.TransferAsync(req));
            // Both source and target inactive
            uow.Setup(u => u.AccountRepository.GetByIdAsync(1)).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            uow.Setup(u => u.AccountRepository.GetByIdAsync(2)).ReturnsAsync(new CheckingAccount { Id = 2, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            req = new TransferReqDto { SourceAccountId = 1, TargetAccountId = 2, Amount = 10 };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => service.TransferAsync(req));
        }
    }
}

