using BankingSystemAPI.Application.DTOs.Role;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.DTOs.Role
{
    public class RoleDtoTests
    {
        [Fact]
        public void RoleReqDto_ValidName_ShouldPassValidation()
        {
            // Arrange
            var dto = new RoleReqDto { Name = "ValidRole" };
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void RoleReqDto_EmptyOrNullName_ShouldFailValidation(string name)
        {
            // Arrange
            var dto = new RoleReqDto { Name = name };
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage.Contains("Role name is required"));
        }

        [Theory]
        [InlineData("A")] // Too short
        [InlineData("ThisRoleNameIsDefinitelyTooLongAndExceedsFiftyChars")] // Too long
        public void RoleReqDto_InvalidNameLength_ShouldFailValidation(string name)
        {
            // Arrange
            var dto = new RoleReqDto { Name = name };
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage.Contains("must be between 2 and 50 characters"));
        }

        [Fact]
        public void RoleResDto_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var dto = new RoleResDto();

            // Assert
            Assert.Equal(string.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.Name);
            Assert.NotNull(dto.Claims);
            Assert.Empty(dto.Claims);
        }

        [Fact]
        public void RoleUpdateResultDto_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var dto = new RoleUpdateResultDto();

            // Assert
            Assert.Equal(string.Empty, dto.Operation);
            Assert.Null(dto.Role);
        }

        [Fact]
        public void RoleClaimsUpdateResultDto_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var dto = new RoleClaimsUpdateResultDto();

            // Assert
            Assert.Equal(string.Empty, dto.RoleName);
            Assert.NotNull(dto.UpdatedClaims);
            Assert.Empty(dto.UpdatedClaims);
        }

        [Fact]
        public void RoleClaimsResDto_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var dto = new RoleClaimsResDto();

            // Assert
            Assert.Null(dto.Name);
            Assert.NotNull(dto.Claims);
            Assert.Empty(dto.Claims);
        }
    }
}