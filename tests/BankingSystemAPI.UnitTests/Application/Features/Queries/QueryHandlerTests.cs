using BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetById;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetAllTransactions;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Specifications.TransactionSpecification;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Features.Queries
{
    public class QueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<GetBalanceQueryHandler>> _mockBalanceLogger;
        private readonly GetBalanceQueryHandler _getBalanceHandler;
        private readonly GetTransactionByIdQueryHandler _getTransactionByIdHandler;
        private readonly GetTransactionsByAccountQueryHandler _getTransactionsByAccountHandler;
        private readonly GetAllTransactionsQueryHandler _getAllTransactionsHandler;
        private readonly GetAccountByIdQueryHandler _getAccountByIdHandler;
        private readonly GetAccountByAccountNumberQueryHandler _getAccountByNumberHandler;
        private readonly GetAccountsByUserIdQueryHandler _getAccountsByUserHandler;
        private readonly GetAccountsByNationalIdQueryHandler _getAccountsByNationalIdHandler;

        public QueryHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockBalanceLogger = new Mock<ILogger<GetBalanceQueryHandler>>();

            // Create handlers with no authorization services for simple testing
            _getBalanceHandler = new GetBalanceQueryHandler(_mockUnitOfWork.Object, _mockBalanceLogger.Object);
            _getTransactionByIdHandler = new GetTransactionByIdQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
            _getTransactionsByAccountHandler = new GetTransactionsByAccountQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
            _getAllTransactionsHandler = new GetAllTransactionsQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
            _getAccountByIdHandler = new GetAccountByIdQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
            _getAccountByNumberHandler = new GetAccountByAccountNumberQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
            _getAccountsByUserHandler = new GetAccountsByUserIdQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
            _getAccountsByNationalIdHandler = new GetAccountsByNationalIdQueryHandler(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        #region Balance Query Tests

        [Fact]
        public async Task GetBalance_ValidAccountId_ShouldReturnBalance()
        {
            // Arrange
            var accountId = 1;
            var query = new GetBalanceQuery(accountId);
            var account = new CheckingAccount
            {
                Id = accountId,
                Balance = 1500.50m,
                IsActive = true,
                User = new ApplicationUser { IsActive = true }
            };

            _mockUnitOfWork.Setup(x => x.AccountRepository.FindAsync(It.IsAny<AccountByIdSpecification>()))
                .ReturnsAsync(account);

            // Act
            var result = await _getBalanceHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1500.50m, result.Value);
        }

        [Fact]
        public async Task GetBalance_NonExistentAccount_ShouldFail()
        {
            // Arrange
            var accountId = 999;
            var query = new GetBalanceQuery(accountId);

            _mockUnitOfWork.Setup(x => x.AccountRepository.FindAsync(It.IsAny<AccountByIdSpecification>()))
                .ReturnsAsync((Account)null);

            // Act
            var result = await _getBalanceHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        #endregion

        #region Transaction Query Tests

        [Fact]
        public async Task GetTransactionById_ValidId_ShouldReturnTransaction()
        {
            // Arrange
            var transactionId = 1;
            var query = new GetTransactionByIdQuery(transactionId);
            var transaction = new Transaction
            {
                Id = transactionId,
                TransactionType = TransactionType.Deposit,
                Timestamp = System.DateTime.UtcNow
            };

            var transactionDto = new TransactionResDto
            {
                TransactionId = transactionId,
                TransactionType = "Deposit",
                Timestamp = transaction.Timestamp,
                Amount = 500m
            };

            _mockUnitOfWork.Setup(x => x.TransactionRepository.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            _mockMapper.Setup(x => x.Map<TransactionResDto>(transaction))
                .Returns(transactionDto);

            // Act
            var result = await _getTransactionByIdHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Deposit", result.Value.TransactionType);
            Assert.Equal(500m, result.Value.Amount);
        }

        [Fact]
        public async Task GetTransactionById_NonExistentId_ShouldFail()
        {
            // Arrange
            var transactionId = 999;
            var query = new GetTransactionByIdQuery(transactionId);

            _mockUnitOfWork.Setup(x => x.TransactionRepository.GetByIdAsync(transactionId))
                .ReturnsAsync((Transaction)null);

            // Act
            var result = await _getTransactionByIdHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        [Fact]
        public async Task GetTransactionsByAccount_ValidAccountId_ShouldReturnTransactions()
        {
            // Arrange
            var accountId = 1;
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTransactionsByAccountQuery(accountId, pageNumber, pageSize);

            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Deposit },
                new Transaction { Id = 2, TransactionType = TransactionType.Withdraw }
            };

            var transactionDtos = new List<TransactionResDto>
            {
                new TransactionResDto { TransactionId = 1, TransactionType = "Deposit", Amount = 500m },
                new TransactionResDto { TransactionId = 2, TransactionType = "Withdraw", Amount = 200m }
            };

            _mockUnitOfWork.Setup(x => x.TransactionRepository.GetPagedAsync(It.IsAny<TransactionsByAccountPagedSpecification>()))
                .ReturnsAsync((transactions, 2));

            _mockMapper.Setup(x => x.Map<TransactionResDto>(It.IsAny<Transaction>()))
                .Returns<Transaction>(t => transactionDtos.FirstOrDefault(dto => dto.TransactionId == t.Id));

            // Act
            var result = await _getTransactionsByAccountHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public async Task GetAllTransactions_ValidPaging_ShouldReturnPagedResults()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 5;
            var query = new GetAllTransactionsQuery(pageNumber, pageSize);

            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Deposit },
                new Transaction { Id = 2, TransactionType = TransactionType.Withdraw },
                new Transaction { Id = 3, TransactionType = TransactionType.Transfer }
            };

            var transactionDtos = new List<TransactionResDto>
            {
                new TransactionResDto { TransactionId = 1, TransactionType = "Deposit", Amount = 1000m },
                new TransactionResDto { TransactionId = 2, TransactionType = "Withdraw", Amount = 500m },
                new TransactionResDto { TransactionId = 3, TransactionType = "Transfer", Amount = 300m }
            };

            _mockUnitOfWork.Setup(x => x.TransactionRepository.GetPagedAsync(It.IsAny<TransactionsPagedSpecification>()))
                .ReturnsAsync((transactions, 15));

            _mockMapper.Setup(x => x.Map<TransactionResDto>(It.IsAny<Transaction>()))
                .Returns<Transaction>(t => transactionDtos.FirstOrDefault(dto => dto.TransactionId == t.Id));

            // Act
            var result = await _getAllTransactionsHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.Count());
        }

        #endregion

        #region Account Query Tests

        [Fact]
        public async Task GetAccountById_ValidId_ShouldReturnAccount()
        {
            // Arrange
            var accountId = 1;
            var query = new GetAccountByIdQuery(accountId);
            var account = new CheckingAccount
            {
                Id = accountId,
                AccountNumber = "CHK-00000001",
                Balance = 2500m,
                IsActive = true,
                Currency = new Currency { Code = "USD" }
            };

            var accountDto = new AccountDto
            {
                Id = accountId,
                AccountNumber = "CHK-00000001",
                Balance = 2500m,
                IsActive = true,
                CurrencyCode = "USD"
            };

            _mockUnitOfWork.Setup(x => x.AccountRepository.FindAsync(It.IsAny<AccountByIdSpecification>()))
                .ReturnsAsync(account);

            _mockMapper.Setup(x => x.Map<AccountDto>(account))
                .Returns(accountDto);

            // Act
            var result = await _getAccountByIdHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("CHK-00000001", result.Value.AccountNumber);
            Assert.Equal(2500m, result.Value.Balance);
            Assert.Equal("USD", result.Value.CurrencyCode);
        }

        [Fact]
        public async Task GetAccountByAccountNumber_ValidNumber_ShouldReturnAccount()
        {
            // Arrange
            var accountNumber = "CHK-00000001";
            var query = new GetAccountByAccountNumberQuery(accountNumber);
            var account = new CheckingAccount
            {
                Id = 1,
                AccountNumber = accountNumber,
                Balance = 3000m,
                IsActive = true
            };

            var accountDto = new AccountDto
            {
                Id = 1,
                AccountNumber = accountNumber,
                Balance = 3000m,
                IsActive = true
            };

            _mockUnitOfWork.Setup(x => x.AccountRepository.FindAsync(It.IsAny<AccountByAccountNumberSpecification>()))
                .ReturnsAsync(account);

            _mockMapper.Setup(x => x.Map<AccountDto>(account))
                .Returns(accountDto);

            // Act
            var result = await _getAccountByNumberHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(accountNumber, result.Value.AccountNumber);
            Assert.Equal(3000m, result.Value.Balance);
        }

        [Fact]
        public async Task GetAccountsByUserId_ValidUserId_ShouldReturnAccounts()
        {
            // Arrange
            var userId = "user123";
            var query = new GetAccountsByUserIdQuery(userId);
            var accounts = new List<Account>
            {
                new CheckingAccount { Id = 1, AccountNumber = "CHK-00000001", Balance = 1500m },
                new SavingsAccount { Id = 2, AccountNumber = "SAV-00000002", Balance = 5000m }
            };

            var accountDtos = new List<AccountDto>
            {
                new AccountDto { Id = 1, AccountNumber = "CHK-00000001", Balance = 1500m },
                new AccountDto { Id = 2, AccountNumber = "SAV-00000002", Balance = 5000m }
            };

            _mockUnitOfWork.Setup(x => x.AccountRepository.ListAsync(It.IsAny<AccountsByUserIdSpecification>()))
                .ReturnsAsync(accounts);

            _mockMapper.Setup(x => x.Map<List<AccountDto>>(accounts))
                .Returns(accountDtos);

            // Act
            var result = await _getAccountsByUserHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(result.Value, a => a.AccountNumber == "CHK-00000001");
            Assert.Contains(result.Value, a => a.AccountNumber == "SAV-00000002");
        }

        [Fact]
        public async Task GetAccountsByNationalId_ValidId_ShouldReturnAccounts()
        {
            // Arrange
            var nationalId = "12345678901234";
            var query = new GetAccountsByNationalIdQuery(nationalId);
            var accounts = new List<Account>
            {
                new CheckingAccount { Id = 1, AccountNumber = "CHK-00000001", Balance = 2000m },
                new SavingsAccount { Id = 2, AccountNumber = "SAV-00000002", Balance = 8000m }
            };

            var accountDtos = new List<AccountDto>
            {
                new AccountDto { Id = 1, AccountNumber = "CHK-00000001", Balance = 2000m },
                new AccountDto { Id = 2, AccountNumber = "SAV-00000002", Balance = 8000m }
            };

            _mockUnitOfWork.Setup(x => x.AccountRepository.ListAsync(It.IsAny<AccountsByNationalIdSpecification>()))
                .ReturnsAsync(accounts);

            _mockMapper.Setup(x => x.Map<List<AccountDto>>(accounts))
                .Returns(accountDtos);

            // Act
            var result = await _getAccountsByNationalIdHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.All(result.Value, a => Assert.True(a.Balance > 0));
        }

        [Theory]
        [InlineData("")]
        [InlineData("123")] // Too short
        [InlineData("12345678901234567890")] // Too long
        public async Task GetAccountsByNationalId_InvalidNationalId_ShouldFail(string nationalId)
        {
            // Arrange
            var query = new GetAccountsByNationalIdQuery(nationalId);

            // Act
            var result = await _getAccountsByNationalIdHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData("")]
        [InlineData("SHORT")]
        public async Task GetAccountByAccountNumber_InvalidAccountNumber_ShouldFail(string accountNumber)
        {
            // Arrange
            var query = new GetAccountByAccountNumberQuery(accountNumber);

            // Act
            var result = await _getAccountByNumberHandler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
        }

        #endregion
    }
}