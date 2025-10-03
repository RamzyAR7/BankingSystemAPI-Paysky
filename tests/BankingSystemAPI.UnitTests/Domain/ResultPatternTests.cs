using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BankingSystemAPI.UnitTests.Domain
{
    public class ResultPatternTests
    {
        #region Success Result Tests

        [Fact]
        public void Result_Success_ShouldHaveCorrectProperties()
        {
            // Act
            var result = Result.Success();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ResultT_Success_ShouldHaveCorrectProperties()
        {
            // Arrange
            var value = "test value";

            // Act
            var result = Result<string>.Success(value);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(value, result.Value);
            Assert.Empty(result.Errors);
        }

        #endregion

        #region Failure Result Tests

        [Fact]
        public void Result_Failure_WithSingleError_ShouldHaveCorrectProperties()
        {
            // Arrange
            var error = "Test error";

            // Act
            var result = Result.Failure(error);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Single(result.Errors);
            Assert.Equal(error, result.Errors.First());
        }

        [Fact]
        public void Result_Failure_WithMultipleErrors_ShouldHaveCorrectProperties()
        {
            // Arrange
            var errors = new[] { "Error 1", "Error 2", "Error 3" };

            // Act
            var result = Result.Failure(errors);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal(3, result.Errors.Count);
            Assert.Equal(errors, result.Errors);
        }

        [Fact]
        public void ResultT_Failure_ShouldHaveCorrectProperties()
        {
            // Arrange
            var error = "Test error";

            // Act
            var result = Result<string>.Failure(error);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
            Assert.Single(result.Errors);
            Assert.Equal(error, result.Errors.First());
        }

        #endregion

        #region Semantic Result Factory Methods Tests

        [Fact]
        public void Result_NotFound_ShouldReturnCorrectResult()
        {
            // Act
            var result = Result.NotFound("User", "123");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("User with ID '123' not found.", result.Errors);
        }

        [Fact]
        public void Result_BadRequest_ShouldReturnCorrectResult()
        {
            // Arrange
            var message = "Invalid input data";

            // Act
            var result = Result.BadRequest(message);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains(message, result.Errors);
        }

        [Fact]
        public void Result_Unauthorized_ShouldReturnCorrectResult()
        {
            // Act
            var result = Result.Unauthorized();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Not authenticated", result.Errors.First());
        }

        [Fact]
        public void Result_Forbidden_ShouldReturnCorrectResult()
        {
            // Act
            var result = Result.Forbidden();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Access denied", result.Errors.First());
        }

        [Fact]
        public void Result_ValidationFailed_ShouldReturnCorrectResult()
        {
            // Arrange
            var validationErrors = new[] { "Field 1 is required", "Field 2 is invalid" };

            // Act
            var result = Result.ValidationFailed(validationErrors);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(validationErrors, result.Errors);
        }

        [Fact]
        public void Result_Conflict_ShouldReturnCorrectResult()
        {
            // Arrange
            var message = "Resource already exists";

            // Act
            var result = Result.Conflict(message);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains(message, result.Errors);
        }

        #endregion

        #region Banking-Specific Result Methods Tests

        [Fact]
        public void Result_InsufficientFunds_ShouldReturnCorrectResult()
        {
            // Arrange
            var requested = 1000m;
            var available = 750m;

            // Act
            var result = Result.InsufficientFunds(requested, available);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Insufficient funds", result.Errors.First());
            Assert.Contains("$1,000.00", result.Errors.First());
            Assert.Contains("$750.00", result.Errors.First());
        }

        [Fact]
        public void Result_AccountInactive_ShouldReturnCorrectResult()
        {
            // Arrange
            var accountNumber = "ACC-12345";

            // Act
            var result = Result.AccountInactive(accountNumber);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Account ACC-12345 is inactive", result.Errors.First());
        }

        [Fact]
        public void Result_AlreadyExists_ShouldReturnCorrectResult()
        {
            // Arrange
            var entity = "User";
            var identifier = "john@example.com";

            // Act
            var result = Result.AlreadyExists(entity, identifier);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("User 'john@example.com' already exists", result.Errors.First());
        }

        [Fact]
        public void Result_InvalidCredentials_ShouldReturnCorrectResult()
        {
            // Act
            var result = Result.InvalidCredentials();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Email or password is incorrect", result.Errors.First());
        }

        #endregion

        #region Result Extensions Tests

        [Fact]
        public void ToResult_NullObject_ShouldReturnFailure()
        {
            // Arrange
            string? nullString = null;

            // Act
            var result = nullString.ToResult("String is null");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("String is null", result.Errors);
        }

        [Fact]
        public void ToResult_NonNullObject_ShouldReturnSuccess()
        {
            // Arrange
            var value = "test string";

            // Act
            var result = value.ToResult("String is null");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(value, result.Value);
        }

        [Fact]
        public void OnSuccess_WithSuccessfulResult_ShouldExecuteAction()
        {
            // Arrange
            var actionExecuted = false;
            var result = Result.Success();

            // Act
            result.OnSuccess(() => actionExecuted = true);

            // Assert
            Assert.True(actionExecuted);
        }

        [Fact]
        public void OnSuccess_WithFailedResult_ShouldNotExecuteAction()
        {
            // Arrange
            var actionExecuted = false;
            var result = Result.Failure("Test error");

            // Act
            result.OnSuccess(() => actionExecuted = true);

            // Assert
            Assert.False(actionExecuted);
        }

        [Fact]
        public void OnFailure_WithFailedResult_ShouldExecuteAction()
        {
            // Arrange
            var errors = new List<string>();
            var result = Result.Failure("Test error");

            // Act
            result.OnFailure(errs => errors.AddRange(errs));

            // Assert
            Assert.Single(errors);
            Assert.Equal("Test error", errors.First());
        }

        [Fact]
        public void OnFailure_WithSuccessfulResult_ShouldNotExecuteAction()
        {
            // Arrange
            var actionExecuted = false;
            var result = Result.Success();

            // Act
            result.OnFailure(errs => actionExecuted = true);

            // Assert
            Assert.False(actionExecuted);
        }

        #endregion

        #region Bind Tests

        [Fact]
        public void Bind_WithSuccessfulResult_ShouldExecuteFunction()
        {
            // Arrange
            var initialResult = Result<int>.Success(5);

            // Act
            var result = initialResult.Bind(value => Result<string>.Success(value.ToString()));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("5", result.Value);
        }

        [Fact]
        public void Bind_WithFailedResult_ShouldNotExecuteFunction()
        {
            // Arrange
            var initialResult = Result<int>.Failure("Initial error");

            // Act
            var result = initialResult.Bind(value => Result<string>.Success(value.ToString()));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Initial error", result.Errors);
        }

        [Fact]
        public void Bind_WithSuccessfulResultButFailedFunction_ShouldReturnFailure()
        {
            // Arrange
            var initialResult = Result<int>.Success(5);

            // Act
            var result = initialResult.Bind(value => Result<string>.Failure("Function failed"));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Function failed", result.Errors);
        }

        #endregion

        #region Combine Tests

        [Fact]
        public void Combine_AllSuccessfulResults_ShouldReturnSuccess()
        {
            // Arrange
            var result1 = Result.Success();
            var result2 = Result.Success();
            var result3 = Result.Success();

            // Act
            var combinedResult = Result.Combine(result1, result2, result3);

            // Assert
            Assert.True(combinedResult.IsSuccess);
        }

        [Fact]
        public void Combine_SomeFailedResults_ShouldReturnFailureWithAllErrors()
        {
            // Arrange
            var result1 = Result.Success();
            var result2 = Result.Failure("Error 2");
            var result3 = Result.Failure("Error 3");

            // Act
            var combinedResult = Result.Combine(result1, result2, result3);

            // Assert
            Assert.True(combinedResult.IsFailure);
            Assert.Equal(2, combinedResult.Errors.Count);
            Assert.Contains("Error 2", combinedResult.Errors);
            Assert.Contains("Error 3", combinedResult.Errors);
        }

        [Fact]
        public void Combine_EmptyResults_ShouldReturnSuccess()
        {
            // Act
            var combinedResult = Result.Combine();

            // Assert
            Assert.True(combinedResult.IsSuccess);
        }

        #endregion
    }
}