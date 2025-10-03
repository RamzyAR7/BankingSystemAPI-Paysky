using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Features.Transactions.Commands.Transfer;
using FluentValidation.TestHelper;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Features.Transactions.Validators
{
    public class TransactionValidatorTests
    {
        private readonly DepositCommandValidator _depositValidator;
        private readonly WithdrawCommandValidator _withdrawValidator;
        private readonly TransferCommandValidator _transferValidator;

        public TransactionValidatorTests()
        {
            _depositValidator = new DepositCommandValidator();
            _withdrawValidator = new WithdrawCommandValidator();
            _transferValidator = new TransferCommandValidator();
        }

        #region Deposit Validator Tests

        [Fact]
        public void DepositValidator_ValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new DepositCommand(new BankingSystemAPI.Application.DTOs.Transactions.DepositReqDto
            {
                AccountId = 1,
                Amount = 500m,
                Description = "Valid deposit"
            });

            // Act & Assert
            var result = _depositValidator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        [InlineData(-0.01)]
        public void DepositValidator_InvalidAmount_ShouldHaveError(decimal amount)
        {
            // Arrange
            var command = new DepositCommand(new BankingSystemAPI.Application.DTOs.Transactions.DepositReqDto
            {
                AccountId = 1,
                Amount = amount,
                Description = "Invalid amount"
            });

            // Act & Assert
            var result = _depositValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Request.Amount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void DepositValidator_InvalidAccountId_ShouldHaveError(int accountId)
        {
            // Arrange
            var command = new DepositCommand(new BankingSystemAPI.Application.DTOs.Transactions.DepositReqDto
            {
                AccountId = accountId,
                Amount = 500m,
                Description = "Valid amount but invalid account"
            });

            // Act & Assert
            var result = _depositValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Request.AccountId);
        }

        #endregion

        #region Withdraw Validator Tests

        [Fact]
        public void WithdrawValidator_ValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new WithdrawCommand(new BankingSystemAPI.Application.DTOs.Transactions.WithdrawReqDto
            {
                AccountId = 1,
                Amount = 300m,
                Description = "Valid withdrawal"
            });

            // Act & Assert
            var result = _withdrawValidator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-200)]
        public void WithdrawValidator_InvalidAmount_ShouldHaveError(decimal amount)
        {
            // Arrange
            var command = new WithdrawCommand(new BankingSystemAPI.Application.DTOs.Transactions.WithdrawReqDto
            {
                AccountId = 1,
                Amount = amount,
                Description = "Invalid amount"
            });

            // Act & Assert
            var result = _withdrawValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Request.Amount);
        }

        #endregion

        #region Transfer Validator Tests

        [Fact]
        public void TransferValidator_ValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new TransferCommand(new BankingSystemAPI.Application.DTOs.Transactions.TransferReqDto
            {
                SourceAccountId = 1,
                TargetAccountId = 2,
                Amount = 250m,
                Description = "Valid transfer"
            });

            // Act & Assert
            var result = _transferValidator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-150)]
        public void TransferValidator_InvalidAmount_ShouldHaveError(decimal amount)
        {
            // Arrange
            var command = new TransferCommand(new BankingSystemAPI.Application.DTOs.Transactions.TransferReqDto
            {
                SourceAccountId = 1,
                TargetAccountId = 2,
                Amount = amount,
                Description = "Invalid amount"
            });

            // Act & Assert
            var result = _transferValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Request.Amount);
        }

        [Fact]
        public void TransferValidator_SameSourceAndTarget_ShouldHaveError()
        {
            // Arrange
            var command = new TransferCommand(new BankingSystemAPI.Application.DTOs.Transactions.TransferReqDto
            {
                SourceAccountId = 1,
                TargetAccountId = 1,
                Amount = 250m,
                Description = "Same account transfer"
            });

            // Act & Assert
            var result = _transferValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Request.TargetAccountId);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 2)]
        [InlineData(1, 0)]
        [InlineData(2, -1)]
        public void TransferValidator_InvalidAccountIds_ShouldHaveError(int sourceId, int targetId)
        {
            // Arrange
            var command = new TransferCommand(new BankingSystemAPI.Application.DTOs.Transactions.TransferReqDto
            {
                SourceAccountId = sourceId,
                TargetAccountId = targetId,
                Amount = 250m,
                Description = "Invalid account IDs"
            });

            // Act & Assert
            var result = _transferValidator.TestValidate(command);
            
            if (sourceId <= 0)
                result.ShouldHaveValidationErrorFor(x => x.Request.SourceAccountId);
            
            if (targetId <= 0)
                result.ShouldHaveValidationErrorFor(x => x.Request.TargetAccountId);
        }

        #endregion
    }
}