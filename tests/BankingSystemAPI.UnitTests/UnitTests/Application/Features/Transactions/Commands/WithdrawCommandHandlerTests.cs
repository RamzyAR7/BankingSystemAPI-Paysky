#region Usings
using AutoMapper;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Repositories;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
#endregion


namespace BankingSystemAPI.UnitTests.UnitTests.Application.Features.Transactions.Commands;

public class WithdrawCommandHandlerTests : TestBase
{
    private readonly WithdrawCommandHandler _handler;

    public WithdrawCommandHandlerTests()
    {
        var logger = new NullLogger<WithdrawCommandHandler>();
        var mockAccountAuth = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IAccountAuthorizationService>();
        mockAccountAuth.Setup(x => x.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<AccountModificationOperation>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result.Success());

        _handler = new WithdrawCommandHandler(UnitOfWork, Mapper, mockAccountAuth.Object);
    }

    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        mapperMock.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>()))
            .Returns((Transaction t) => new TransactionResDto
            {
                TransactionType = t.TransactionType.ToString(),
                Timestamp = t.Timestamp,
                Amount = t.AccountTransactions?.FirstOrDefault()?.Amount ?? 0m
            });
    }

    [Fact]
    public async Task Withdraw_HappyPath_CheckingAccount_Succeeds()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 500m);
        var req = new WithdrawReqDto { AccountId = account.Id, Amount = 100m };

        // Act
        var result = await _handler.Handle(new WithdrawCommand(req), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Context.Entry(account).Reload();
        Assert.Equal(400m, account.Balance);
    }

    [Fact]
    public async Task Withdraw_InvalidAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 500m);
        var req = new WithdrawReqDto { AccountId = account.Id, Amount = 0m };

        // Act
        var result = await _handler.Handle(new WithdrawCommand(req), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Transfer amount must be greater than zero") || e.Contains("greater than zero"));
    }

    [Fact]
    public async Task Withdraw_AuthorizationDenied_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 500m);

        var mockAccountAuth = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IAccountAuthorizationService>();
        mockAccountAuth.Setup(x => x.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<AccountModificationOperation>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result.Forbidden("Access denied."));

        var handler = new WithdrawCommandHandler(UnitOfWork, Mapper, mockAccountAuth.Object);
        var req = new WithdrawReqDto { AccountId = account.Id, Amount = 10m };

        // Act
        var result = await handler.Handle(new WithdrawCommand(req), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ErrorItems, e => e.Type == BankingSystemAPI.Domain.Constant.ErrorType.Forbidden);
    }

    [Fact]
    public async Task Withdraw_InactiveAccount_ShouldReturnBadRequest()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 100m);
        account.IsActive = false;
        Context.SaveChanges();

        var req = new WithdrawReqDto { AccountId = account.Id, Amount = 10m };

        // Act
        var result = await _handler.Handle(new WithdrawCommand(req), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("inactive") || e.Contains("inactive"));
    }

    private CheckingAccount CreateTestCheckingAccount(string userId, decimal balance = 0m)
    {
        var currency = GetBaseCurrency();
        var account = TestEntityFactory.CreateCheckingAccount(userId, currency.Id, balance);
        Context.CheckingAccounts.Add(account);
        Context.SaveChanges();
        return account;
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_SavingsAccount_Fails()
    {
        // Arrange
        var user = CreateTestUser();
        var savings = TestEntityFactory.CreateSavingsAccount(user.Id, GetBaseCurrency().Id, balance: 50m);
        Context.SavingsAccounts.Add(savings);
        Context.SaveChanges();

        var req = new WithdrawReqDto { AccountId = savings.Id, Amount = 100m };

        // Act
        var result = await _handler.Handle(new WithdrawCommand(req), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Insufficient funds") || e.Contains("Insufficient"));
    }

    [Fact]
    public async Task Withdraw_ConcurrencyConflict_RetriesAndReturnsConflict()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 500m);

        // Create a fake UnitOfWork that delegates to the real UnitOfWork but throws concurrency on SaveAsync
        var fakeUow = new FakeUnitOfWork(UnitOfWork, throwTimes: 3);
        var mockAccountAuth = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IAccountAuthorizationService>();
        mockAccountAuth.Setup(x => x.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<AccountModificationOperation>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result.Success());

        var handlerWithFakeUow = new WithdrawCommandHandler(fakeUow, Mapper, mockAccountAuth.Object);

        var req = new WithdrawReqDto { AccountId = account.Id, Amount = 10m };

        // Act
        var result = await handlerWithFakeUow.Handle(new WithdrawCommand(req), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.ErrorItems, e => e.Type == ErrorType.Conflict);
    }

    // Simple wrapper that delegates to an existing IUnitOfWork but can be configured to throw concurrency exceptions
    private class FakeUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private int _remainingThrows;

        public FakeUnitOfWork(IUnitOfWork inner, int throwTimes = 1)
        {
            _inner = inner;
            _remainingThrows = throwTimes;
        }

        public IRoleRepository RoleRepository => _inner.RoleRepository;
        public IUserRepository UserRepository => _inner.UserRepository;
        public IAccountRepository AccountRepository => _inner.AccountRepository;
        public ITransactionRepository TransactionRepository => _inner.TransactionRepository;
        public IAccountTransactionRepository AccountTransactionRepository => _inner.AccountTransactionRepository;
        public IInterestLogRepository InterestLogRepository => _inner.InterestLogRepository;
        public ICurrencyRepository CurrencyRepository => _inner.CurrencyRepository;
        public IBankRepository BankRepository => _inner.BankRepository;

        public void Dispose() => _inner.Dispose();

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => _inner.BeginTransactionAsync(cancellationToken);
        public Task CommitAsync(CancellationToken cancellationToken = default) => _inner.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken = default) => _inner.RollbackAsync(cancellationToken);
        public Task ReloadTrackedEntitiesAsync(CancellationToken cancellationToken = default) => _inner.ReloadTrackedEntitiesAsync(cancellationToken);
        public void DetachEntity<T>(T entity) where T : class => _inner.DetachEntity(entity);

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (_remainingThrows > 0)
            {
                _remainingThrows--;
                return Task.FromException(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException());
            }

            return _inner.SaveAsync(cancellationToken);
        }
    }
}
