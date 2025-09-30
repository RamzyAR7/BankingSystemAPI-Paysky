using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
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
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Features.Transactions.Commands.Transfer;

namespace BankingSystemAPI.UnitTests
{
    public class InactiveFeatureTests
    {
        [Fact]
        public async Task CreateAccount_InactiveUser_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var accountAuth = new Mock<IAccountAuthorizationService>();
            var createHandler = new CreateCheckingAccountCommandHandler(uow.Object, mapper.Object, accountAuth.Object);
            var user = new ApplicationUser { Id = "u1", IsActive = false };
            uow.Setup(u => u.CurrencyRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Currency { Id = 1, IsActive = true });
            // Replace legacy FindWithIncludesAsync with specification-based FindAsync
            uow.Setup(u => u.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(user);
            uow.Setup(u => u.RoleRepository.GetRoleByUserIdAsync("u1")).ReturnsAsync(new ApplicationRole { Name = "Client" });
            var req = new CheckingAccountReqDto { UserId = "u1", CurrencyId = 1, InitialBalance = 0 };

            var res = await createHandler.Handle(new CreateCheckingAccountCommand(req), default);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task CreateAccount_InactiveCurrency_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var accountAuth = new Mock<IAccountAuthorizationService>();
            var createHandler = new CreateCheckingAccountCommandHandler(uow.Object, mapper.Object, accountAuth.Object);
            var user = new ApplicationUser { Id = "u1", IsActive = true };
            uow.Setup(u => u.CurrencyRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Currency { Id = 1, IsActive = false });
            // Replace legacy FindWithIncludesAsync with specification-based FindAsync
            uow.Setup(u => u.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(user);
            uow.Setup(u => u.RoleRepository.GetRoleByUserIdAsync("u1")).ReturnsAsync(new ApplicationRole { Name = "Client" });
            var req = new CheckingAccountReqDto { UserId = "u1", CurrencyId = 1, InitialBalance = 0 };

            var res = await createHandler.Handle(new CreateCheckingAccountCommand(req), default);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Deposit_InactiveAccount_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var helper = new Mock<ITransactionHelperService>();
            var accountAuth = new Mock<IAccountAuthorizationService>();
            var transactionAuth = new Mock<ITransactionAuthorizationService>();
            var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();

            uow.Setup(u => u.AccountRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            var depositHandler = new DepositCommandHandler(uow.Object, mapper.Object);
            var req = new DepositReqDto { AccountId = 1, Amount = 10 };
            var res = await depositHandler.Handle(new DepositCommand(req), default);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Withdraw_InactiveAccount_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var helper = new Mock<ITransactionHelperService>();
            var accountAuth = new Mock<IAccountAuthorizationService>();
            var transactionAuth = new Mock<ITransactionAuthorizationService>();
            var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();

            uow.Setup(u => u.AccountRepository.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            var withdrawHandler = new WithdrawCommandHandler(uow.Object, mapper.Object);
            var req = new WithdrawReqDto { AccountId = 1, Amount = 10 };
            var res = await withdrawHandler.Handle(new WithdrawCommand(req), default);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Transfer_InactiveAccount_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            var helper = new Mock<ITransactionHelperService>();
            var accountAuth = new Mock<IAccountAuthorizationService>();
            var transactionAuth = new Mock<ITransactionAuthorizationService>();
            var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            var currentUser = new Mock<ICurrentUserService>();

            // Source inactive, target active
            uow.Setup(u => u.AccountRepository.FindAsync(It.IsAny<Application.Specifications.AccountSpecification.AccountByIdSpecification>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            uow.Setup(u => u.AccountRepository.FindAsync(It.IsAny<Application.Specifications.AccountSpecification.AccountByIdSpecification>())).ReturnsAsync(new CheckingAccount { Id = 2, IsActive = true, Currency = new Currency { Id = 1, IsActive = true } });
            var transferHandler = new TransferCommandHandler(uow.Object, mapper.Object, helper.Object, transactionAuth.Object);
            var req = new TransferReqDto { SourceAccountId = 1, TargetAccountId = 2, Amount = 10 };
            var res = await transferHandler.Handle(new TransferCommand(req), default);
            Assert.False(res.Succeeded);

            // Source active, target inactive
            uow.Setup(u => u.AccountRepository.FindAsync(It.IsAny<Application.Specifications.AccountSpecification.AccountByIdSpecification>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = true, Currency = new Currency { Id = 1, IsActive = true } });
            uow.Setup(u => u.AccountRepository.FindAsync(It.IsAny<Application.Specifications.AccountSpecification.AccountByIdSpecification>())).ReturnsAsync(new CheckingAccount { Id = 2, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            req = new TransferReqDto { SourceAccountId = 1, TargetAccountId = 2, Amount = 10 };
            res = await transferHandler.Handle(new TransferCommand(req), default);
            Assert.False(res.Succeeded);

            // Both source and target inactive
            uow.Setup(u => u.AccountRepository.FindAsync(It.IsAny<Application.Specifications.AccountSpecification.AccountByIdSpecification>())).ReturnsAsync(new CheckingAccount { Id = 1, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            uow.Setup(u => u.AccountRepository.FindAsync(It.IsAny<Application.Specifications.AccountSpecification.AccountByIdSpecification>())).ReturnsAsync(new CheckingAccount { Id = 2, IsActive = false, Currency = new Currency { Id = 1, IsActive = true } });
            req = new TransferReqDto { SourceAccountId = 1, TargetAccountId = 2, Amount = 10 };
            res = await transferHandler.Handle(new TransferCommand(req), default);
            Assert.False(res.Succeeded);
        }
    }
}
