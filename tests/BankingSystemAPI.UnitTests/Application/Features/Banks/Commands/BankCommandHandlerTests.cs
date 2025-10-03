using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Features.Banks.Commands.CreateBank;
using BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank;
using BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank;
using BankingSystemAPI.Application.Features.Banks.Commands.SetBankActiveStatus;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Features.Banks.Commands
{
    public class BankCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CreateBankCommandHandler>> _mockCreateLogger;
        private readonly Mock<ILogger<UpdateBankCommandHandler>> _mockUpdateLogger;
        private readonly Mock<ILogger<DeleteBankCommandHandler>> _mockDeleteLogger;
        private readonly CreateBankCommandHandler _createHandler;
        private readonly UpdateBankCommandHandler _updateHandler;
        private readonly DeleteBankCommandHandler _deleteHandler;
        private readonly SetBankActiveStatusCommandHandler _setActiveStatusHandler;

        public BankCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateLogger = new Mock<ILogger<CreateBankCommandHandler>>();
            _mockUpdateLogger = new Mock<ILogger<UpdateBankCommandHandler>>();
            _mockDeleteLogger = new Mock<ILogger<DeleteBankCommandHandler>>();

            _createHandler = new CreateBankCommandHandler(_mockUnitOfWork.Object, _mockMapper.Object, _mockCreateLogger.Object);
            _updateHandler = new UpdateBankCommandHandler(_mockUnitOfWork.Object, _mockMapper.Object, _mockUpdateLogger.Object);
            _deleteHandler = new DeleteBankCommandHandler(_mockUnitOfWork.Object, _mockDeleteLogger.Object);
            _setActiveStatusHandler = new SetBankActiveStatusCommandHandler(_mockUnitOfWork.Object);
        }

        #region Create Bank Tests

        [Fact]
        public async Task CreateBank_ValidBank_ShouldSucceed()
        {
            // Arrange
            var bankReq = new BankReqDto
            {
                Name = "Test Bank",
                SwiftCode = "TESTUS33",
                Address = "123 Banking St, Finance City",
                PhoneNumber = "01234567890"
            };

            var command = new CreateBankCommand(bankReq);
            var bank = new Bank
            {
                Id = 1,
                Name = "Test Bank",
                SwiftCode = "TESTUS33",
                Address = "123 Banking St, Finance City",
                PhoneNumber = "01234567890",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var bankResDto = new BankResDto
            {
                Id = 1,
                Name = "Test Bank",
                SwiftCode = "TESTUS33",
                Address = "123 Banking St, Finance City",
                PhoneNumber = "01234567890",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            SetupMockBankCreation(bank, bankResDto);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Test Bank", result.Value.Name);
            Assert.Equal("TESTUS33", result.Value.SwiftCode);
            Assert.True(result.Value.IsActive);
        }

        [Fact]
        public async Task CreateBank_DuplicateName_ShouldFail()
        {
            // Arrange
            var bankReq = new BankReqDto
            {
                Name = "Existing Bank",
                SwiftCode = "EXISTUS33",
                Address = "456 Banking Ave, Finance City",
                PhoneNumber = "01987654321"
            };

            var command = new CreateBankCommand(bankReq);
            var existingBank = new Bank { Name = "Existing Bank" };

            _mockUnitOfWork.Setup(x => x.BankRepository.FindAsync(It.IsAny<BankByNormalizedNameSpecification>()))
                .ReturnsAsync(existingBank);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("already exists", string.Join(" ", result.Errors));
        }

        [Theory]
        [InlineData("")]           // Empty name
        [InlineData("A")]          // Too short
        [InlineData("AB")]         // Still too short
        public async Task CreateBank_InvalidName_ShouldFail(string name)
        {
            // Arrange
            var bankReq = new BankReqDto
            {
                Name = name,
                SwiftCode = "TESTUS33",
                Address = "123 Banking St, Finance City",
                PhoneNumber = "01234567890"
            };

            var command = new CreateBankCommand(bankReq);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData("SHORT")]      // Too short (should be 8 or 11 characters)
        [InlineData("TOOLONGSWIFT")] // Too long
        [InlineData("")]           // Empty
        public async Task CreateBank_InvalidSwiftCode_ShouldFail(string swiftCode)
        {
            // Arrange
            var bankReq = new BankReqDto
            {
                Name = "Test Bank",
                SwiftCode = swiftCode,
                Address = "123 Banking St, Finance City",
                PhoneNumber = "01234567890"
            };

            var command = new CreateBankCommand(bankReq);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData("")]           // Empty address
        [InlineData("Short")]      // Too short
        public async Task CreateBank_InvalidAddress_ShouldFail(string address)
        {
            // Arrange
            var bankReq = new BankReqDto
            {
                Name = "Test Bank",
                SwiftCode = "TESTUS33",
                Address = address,
                PhoneNumber = "01234567890"
            };

            var command = new CreateBankCommand(bankReq);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData("")]           // Empty phone
        [InlineData("123")]        // Too short
        [InlineData("abc123def")]  // Invalid format
        public async Task CreateBank_InvalidPhoneNumber_ShouldFail(string phoneNumber)
        {
            // Arrange
            var bankReq = new BankReqDto
            {
                Name = "Test Bank",
                SwiftCode = "TESTUS33",
                Address = "123 Banking St, Finance City",
                PhoneNumber = phoneNumber
            };

            var command = new CreateBankCommand(bankReq);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
        }

        #endregion

        #region Update Bank Tests

        [Fact]
        public async Task UpdateBank_ValidUpdate_ShouldSucceed()
        {
            // Arrange
            var bankId = 1;
            var updateDto = new BankEditDto
            {
                Name = "Updated Bank Name",
                SwiftCode = "UPDATUS33",
                Address = "456 Updated St, New City",
                PhoneNumber = "01987654321"
            };

            var command = new UpdateBankCommand(bankId, updateDto);
            var existingBank = new Bank
            {
                Id = bankId,
                Name = "Original Bank",
                SwiftCode = "ORIGUS33",
                Address = "123 Original St, Old City",
                PhoneNumber = "01234567890",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var updatedBankDto = new BankResDto
            {
                Id = bankId,
                Name = "Updated Bank Name",
                SwiftCode = "UPDATUS33",
                Address = "456 Updated St, New City",
                PhoneNumber = "01987654321",
                IsActive = true,
                CreatedAt = existingBank.CreatedAt
            };

            SetupMockBankUpdate(existingBank, updatedBankDto);

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Updated Bank Name", result.Value.Name);
            Assert.Equal("UPDATUS33", result.Value.SwiftCode);
        }

        [Fact]
        public async Task UpdateBank_NonExistentBank_ShouldFail()
        {
            // Arrange
            var bankId = 999;
            var updateDto = new BankEditDto
            {
                Name = "Updated Bank",
                SwiftCode = "UPDATUS33",
                Address = "456 Updated St, New City",
                PhoneNumber = "01987654321"
            };

            var command = new UpdateBankCommand(bankId, updateDto);

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync((Bank)null);

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        [Fact]
        public async Task UpdateBank_DuplicateNameWithDifferentBank_ShouldFail()
        {
            // Arrange
            var bankId = 1;
            var updateDto = new BankEditDto
            {
                Name = "Another Bank", // This name exists for a different bank
                SwiftCode = "UPDATUS33",
                Address = "456 Updated St, New City",
                PhoneNumber = "01987654321"
            };

            var command = new UpdateBankCommand(bankId, updateDto);
            var existingBank = new Bank
            {
                Id = bankId,
                Name = "Original Bank",
                SwiftCode = "ORIGUS33"
            };

            var anotherBank = new Bank
            {
                Id = 2, // Different bank with the name we're trying to use
                Name = "Another Bank"
            };

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync(existingBank);

            _mockUnitOfWork.Setup(x => x.BankRepository.FindAsync(It.IsAny<BankByNormalizedNameSpecification>()))
                .ReturnsAsync(anotherBank);

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("already exists", string.Join(" ", result.Errors));
        }

        #endregion

        #region Delete Bank Tests

        [Fact]
        public async Task DeleteBank_ValidBank_ShouldSucceed()
        {
            // Arrange
            var bankId = 1;
            var command = new DeleteBankCommand(bankId);
            var bank = new Bank
            {
                Id = bankId,
                Name = "Test Bank",
                IsActive = true
            };

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync(bank);

            _mockUnitOfWork.Setup(x => x.BankRepository.HasUsersAsync(bankId))
                .ReturnsAsync(false);

            _mockUnitOfWork.Setup(x => x.SaveAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteBank_BankWithUsers_ShouldFail()
        {
            // Arrange
            var bankId = 1;
            var command = new DeleteBankCommand(bankId);
            var bank = new Bank
            {
                Id = bankId,
                Name = "Test Bank",
                IsActive = true
            };

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync(bank);

            _mockUnitOfWork.Setup(x => x.BankRepository.HasUsersAsync(bankId))
                .ReturnsAsync(true);

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("users", string.Join(" ", result.Errors).ToLower());
        }

        [Fact]
        public async Task DeleteBank_NonExistentBank_ShouldFail()
        {
            // Arrange
            var bankId = 999;
            var command = new DeleteBankCommand(bankId);

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync((Bank)null);

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        #endregion

        #region Set Active Status Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetBankActiveStatus_ValidRequest_ShouldSucceed(bool isActive)
        {
            // Arrange
            var bankId = 1;
            var command = new SetBankActiveStatusCommand(bankId, isActive);
            var bank = new Bank
            {
                Id = bankId,
                Name = "Test Bank",
                IsActive = !isActive // Opposite of what we're setting
            };

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync(bank);

            _mockUnitOfWork.Setup(x => x.SaveAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _setActiveStatusHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(isActive, bank.IsActive);
        }

        [Fact]
        public async Task SetBankActiveStatus_NonExistentBank_ShouldFail()
        {
            // Arrange
            var bankId = 999;
            var command = new SetBankActiveStatusCommand(bankId, true);

            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bankId))
                .ReturnsAsync((Bank)null);

            // Act
            var result = await _setActiveStatusHandler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        #endregion

        #region Helper Methods

        private void SetupMockBankCreation(Bank bank, BankResDto bankResDto)
        {
            _mockUnitOfWork.Setup(x => x.BankRepository.FindAsync(It.IsAny<BankByNormalizedNameSpecification>()))
                .ReturnsAsync((Bank)null);

            _mockUnitOfWork.Setup(x => x.BankRepository.AddAsync(It.IsAny<Bank>()))
                .ReturnsAsync(bank);

            _mockUnitOfWork.Setup(x => x.SaveAsync())
                .ReturnsAsync(1);

            _mockMapper.Setup(x => x.Map<Bank>(It.IsAny<BankReqDto>()))
                .Returns(bank);

            _mockMapper.Setup(x => x.Map<BankResDto>(It.IsAny<Bank>()))
                .Returns(bankResDto);
        }

        private void SetupMockBankUpdate(Bank bank, BankResDto bankResDto)
        {
            _mockUnitOfWork.Setup(x => x.BankRepository.GetByIdAsync(bank.Id))
                .ReturnsAsync(bank);

            _mockUnitOfWork.Setup(x => x.BankRepository.FindAsync(It.IsAny<BankByNormalizedNameSpecification>()))
                .ReturnsAsync((Bank)null); // No duplicate name found

            _mockUnitOfWork.Setup(x => x.SaveAsync())
                .ReturnsAsync(1);

            _mockMapper.Setup(x => x.Map<BankResDto>(It.IsAny<Bank>()))
                .Returns(bankResDto);
        }

        #endregion
    }
}