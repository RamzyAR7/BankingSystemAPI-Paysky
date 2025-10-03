using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.DTOs.Transactions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.DTOs
{
    public class AllDtoValidationTests
    {
        #region User DTOs Tests

        [Fact]
        public void UserReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new UserReqDto
            {
                Username = "validuser123",
                Email = "valid@example.com",
                Password = "ValidPassword123!",
                PasswordConfirm = "ValidPassword123!",
                FullName = "Valid User Name",
                PhoneNumber = "01234567890",
                NationalId = "12345678901234",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Role = "Client"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void UserEditDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new UserEditDto
            {
                Username = "editeduser",
                Email = "edited@example.com",
                FullName = "Edited User Name",
                PhoneNumber = "01987654321"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void ChangePasswordReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new ChangePasswordReqDto
            {
                CurrentPassword = "CurrentPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void UpdateUserRolesDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new UpdateUserRolesDto
            {
                UserId = "user123",
                Role = "Admin"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        #endregion

        #region Role DTOs Tests

        [Fact]
        public void RoleReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new RoleReqDto
            {
                Name = "TestRole"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Theory]
        [InlineData("")]
        [InlineData("A")]
        [InlineData("ThisRoleNameIsTooLongAndExceedsFiftyCharactersLimit")]
        public void RoleReqDto_InvalidName_ShouldFailValidation(string name)
        {
            // Arrange
            var dto = new RoleReqDto { Name = name };

            // Act & Assert
            AssertValidationFails(dto);
        }

        #endregion

        #region Account DTOs Tests

        [Fact]
        public void CheckingAccountReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new CheckingAccountReqDto
            {
                UserId = "user123",
                CurrencyId = 1,
                OverdraftLimit = 1000m,
                OverdraftFee = 25m
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void SavingsAccountReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new SavingsAccountReqDto
            {
                UserId = "user123",
                CurrencyId = 1,
                InterestRate = 0.05m
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void CheckingAccountEditDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new CheckingAccountEditDto
            {
                OverdraftLimit = 1500m,
                OverdraftFee = 30m
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void SavingsAccountEditDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new SavingsAccountEditDto
            {
                InterestRate = 0.06m
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        #endregion

        #region Transaction DTOs Tests

        [Fact]
        public void DepositReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new DepositReqDto
            {
                AccountId = 1,
                Amount = 500m,
                Description = "Valid deposit transaction"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void WithdrawReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new WithdrawReqDto
            {
                AccountId = 1,
                Amount = 300m,
                Description = "Valid withdrawal transaction"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void TransferReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new TransferReqDto
            {
                SourceAccountId = 1,
                TargetAccountId = 2,
                Amount = 250m,
                Description = "Valid transfer transaction"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void TransactionReqDto_InvalidAmount_ShouldFailValidation(decimal amount)
        {
            // Arrange
            var depositDto = new DepositReqDto
            {
                AccountId = 1,
                Amount = amount,
                Description = "Invalid amount"
            };

            // Act & Assert
            AssertValidationFails(depositDto);
        }

        #endregion

        #region Currency DTOs Tests

        [Fact]
        public void CurrencyReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new CurrencyReqDto
            {
                Code = "EUR",
                ExchangeRate = 0.85m,
                IsBase = false
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Theory]
        [InlineData("")]
        [InlineData("AB")]
        [InlineData("ABCD")]
        public void CurrencyReqDto_InvalidCode_ShouldFailValidation(string code)
        {
            // Arrange
            var dto = new CurrencyReqDto
            {
                Code = code,
                ExchangeRate = 1.0m,
                IsBase = false
            };

            // Act & Assert
            AssertValidationFails(dto);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1.5)]
        public void CurrencyReqDto_InvalidExchangeRate_ShouldFailValidation(decimal exchangeRate)
        {
            // Arrange
            var dto = new CurrencyReqDto
            {
                Code = "GBP",
                ExchangeRate = exchangeRate,
                IsBase = false
            };

            // Act & Assert
            AssertValidationFails(dto);
        }

        #endregion

        #region Bank DTOs Tests

        [Fact]
        public void BankReqDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new BankReqDto
            {
                Name = "Test Bank Corporation",
                SwiftCode = "TESTUS33",
                Address = "123 Banking Street, Finance City, Country 12345",
                PhoneNumber = "01234567890"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Fact]
        public void BankEditDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new BankEditDto
            {
                Name = "Updated Bank Name",
                SwiftCode = "UPDATUS33",
                Address = "456 Updated Street, New City, Country 67890",
                PhoneNumber = "01987654321"
            };

            // Act & Assert
            AssertValidationPasses(dto);
        }

        [Theory]
        [InlineData("")]
        [InlineData("AB")]
        public void BankReqDto_InvalidName_ShouldFailValidation(string name)
        {
            // Arrange
            var dto = new BankReqDto
            {
                Name = name,
                SwiftCode = "TESTUS33",
                Address = "123 Banking Street",
                PhoneNumber = "01234567890"
            };

            // Act & Assert
            AssertValidationFails(dto);
        }

        #endregion

        #region Response DTOs Tests

        [Fact]
        public void UserResDto_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var dto = new UserResDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(string.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.Username);
            Assert.Equal(string.Empty, dto.Email);
        }

        [Fact]
        public void RoleResDto_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var dto = new RoleResDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(string.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.Name);
            Assert.NotNull(dto.Claims);
        }

        [Fact]
        public void AccountDto_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var dto = new AccountDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(0, dto.Id);
            Assert.Equal(string.Empty, dto.AccountNumber);
            Assert.Equal(0m, dto.Balance);
        }

        [Fact]
        public void TransactionResDto_DefaultValues_ShouldBeValid()
        {
            // Arrange & Act
            var dto = new TransactionResDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(0, dto.Id);
            Assert.Equal(string.Empty, dto.TransactionType);
            Assert.Equal(0m, dto.Amount);
        }

        #endregion

        #region Helper Methods

        private void AssertValidationPasses(object dto)
        {
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            Assert.True(isValid, $"Validation should pass. Errors: {string.Join(", ", results.Select(r => r.ErrorMessage))}");
        }

        private void AssertValidationFails(object dto)
        {
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            Assert.False(isValid, "Validation should fail");
            Assert.NotEmpty(results);
        }

        #endregion
    }
}