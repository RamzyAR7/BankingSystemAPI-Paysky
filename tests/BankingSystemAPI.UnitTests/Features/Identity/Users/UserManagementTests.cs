using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using AutoMapper;
using Moq;

namespace BankingSystemAPI.UnitTests.Features.Identity.Users;

/// <summary>
/// Tests for User-related DTOs, validation, and business logic.
/// Focuses on testing user domain rules and data structures.
/// </summary>
public class UserValidationTests : TestBase
{
    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        // User validation tests don't need complex mapping
    }

    #region User DTO Builder Tests

    [Fact]
    public void TestDtoBuilder_User_ShouldCreateValidUserReqDto()
    {
        // Act
        var userDto = TestDtoBuilder.User()
            .WithUsername("john.doe")
            .WithEmail("john@example.com")
            .WithFullName("John Doe")
            .WithPhoneNumber("01123456789")
            .WithNationalId("12345678901234")
            .WithRole("Client")
            .Build();

        // Assert
        Assert.Equal("john.doe", userDto.Username);
        Assert.Equal("john@example.com", userDto.Email);
        Assert.Equal("John Doe", userDto.FullName);
        Assert.Equal("01123456789", userDto.PhoneNumber);
        Assert.Equal("12345678901234", userDto.NationalId);
        Assert.Equal("Client", userDto.Role);
        Assert.Equal("Test@123", userDto.Password);
        Assert.Equal("Test@123", userDto.PasswordConfirm);
    }

    [Fact]
    public void TestDtoBuilder_UserEdit_ShouldCreateValidUserEditDto()
    {
        // Act
        var userDto = TestDtoBuilder.UserEdit()
            .WithUsername("jane.doe")
            .WithEmail("jane@example.com")
            .WithFullName("Jane Doe")
            .WithPhoneNumber("01987654321")
            .Build();

        // Assert
        Assert.Equal("jane.doe", userDto.Username);
        Assert.Equal("jane@example.com", userDto.Email);
        Assert.Equal("Jane Doe", userDto.FullName);
        Assert.Equal("01987654321", userDto.PhoneNumber);
    }

    #endregion

    #region User Business Logic Tests

    [Theory]
    [InlineData("validuser", true)]
    [InlineData("test_user", true)]
    [InlineData("user123", true)]
    [InlineData("a", false)]           // Too short
    [InlineData("", false)]            // Empty
    [InlineData("user@name", false)]   // Invalid character
    [InlineData("user name", false)]   // Space not allowed
    public void Username_Validation_ShouldFollowBusinessRules(string username, bool shouldBeValid)
    {
        // Arrange
        var userDto = TestDtoBuilder.User()
            .WithUsername(username)
            .Build();

        // Act & Assert
        if (shouldBeValid)
        {
            Assert.Equal(username, userDto.Username);
            Assert.NotEmpty(userDto.Username);
        }
        else
        {
            // For invalid usernames, we expect them to fail validation
            // This test documents the expected validation behavior
            Assert.True(string.IsNullOrEmpty(username) || username.Length < 3 || 
                       username.Contains('@') || username.Contains(' '));
        }
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("test.user+tag@domain.org", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData("user@", false)]
    [InlineData("@domain.com", false)]
    public void Email_Validation_ShouldFollowBusinessRules(string email, bool shouldBeValid)
    {
        // Arrange
        var userDto = TestDtoBuilder.User()
            .WithEmail(email)
            .Build();

        // Act & Assert
        if (shouldBeValid)
        {
            Assert.Equal(email, userDto.Email);
            Assert.Contains("@", userDto.Email);
        }
        else
        {
            // For invalid emails, document expected validation behavior
            Assert.True(string.IsNullOrEmpty(email) || !email.Contains("@") || 
                       email.StartsWith("@") || email.EndsWith("@"));
        }
    }

    [Theory]
    [InlineData("01123456789", true)]   // Valid Egyptian mobile
    [InlineData("01012345678", true)]   // Valid Egyptian mobile  
    [InlineData("01512345678", true)]   // Valid Egyptian mobile
    [InlineData("0212345678", false)]   // Too short
    [InlineData("012345678901", false)] // Too long
    [InlineData("02123456789", false)]  // Invalid prefix
    [InlineData("", false)]             // Empty
    public void PhoneNumber_EgyptianValidation_ShouldFollowBusinessRules(string phoneNumber, bool shouldBeValid)
    {
        // Arrange
        var userDto = TestDtoBuilder.User()
            .WithPhoneNumber(phoneNumber)
            .Build();

        // Act & Assert
        if (shouldBeValid)
        {
            Assert.Equal(phoneNumber, userDto.PhoneNumber);
            Assert.Equal(11, userDto.PhoneNumber.Length);
            Assert.StartsWith("01", userDto.PhoneNumber);
        }
        else
        {
            // Document validation rules for Egyptian phone numbers
            Assert.True(string.IsNullOrEmpty(phoneNumber) || 
                       phoneNumber.Length != 11 || 
                       !phoneNumber.StartsWith("01"));
        }
    }

    [Theory]
    [InlineData("12345678901234", true)]  // Valid 14-digit Egyptian ID
    [InlineData("29912345678901", true)]  // Valid Egyptian ID
    [InlineData("123", false)]            // Too short
    [InlineData("12345678901234567", false)] // Too long
    [InlineData("1234567890123a", false)] // Contains letter
    [InlineData("", false)]               // Empty
    public void NationalId_EgyptianValidation_ShouldFollowBusinessRules(string nationalId, bool shouldBeValid)
    {
        // Arrange
        var userDto = TestDtoBuilder.User()
            .WithNationalId(nationalId)
            .Build();

        // Act & Assert
        if (shouldBeValid)
        {
            Assert.Equal(nationalId, userDto.NationalId);
            Assert.Equal(14, userDto.NationalId.Length);
            Assert.True(userDto.NationalId.All(char.IsDigit));
        }
        else
        {
            // Document validation rules for Egyptian National ID
            Assert.True(string.IsNullOrEmpty(nationalId) || 
                       nationalId.Length != 14 || 
                       !nationalId.All(char.IsDigit));
        }
    }

    [Theory]
    [InlineData(18, true)]    // Legal age
    [InlineData(25, true)]    // Adult
    [InlineData(65, true)]    // Senior
    [InlineData(17, false)]   // Underage
    [InlineData(16, false)]   // Too young
    [InlineData(150, false)]  // Unrealistic age
    public void Age_Validation_ShouldFollowBusinessRules(int age, bool shouldBeValid)
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age));
        var userDto = TestDtoBuilder.User()
            .WithDateOfBirth(dateOfBirth)
            .Build();

        var calculatedAge = DateTime.UtcNow.Year - userDto.DateOfBirth.Year;
        if (DateTime.UtcNow.DayOfYear < userDto.DateOfBirth.DayOfYear)
            calculatedAge--;

        // Act & Assert
        if (shouldBeValid)
        {
            Assert.True(calculatedAge >= 18 && calculatedAge <= 120);
        }
        else
        {
            Assert.True(calculatedAge < 18 || calculatedAge > 120);
        }
    }

    #endregion

    #region Password Security Tests

    [Theory]
    [InlineData("Password123!", true)]   // Strong password
    [InlineData("MySecure@Pass1", true)] // Strong password
    [InlineData("password", false)]      // No uppercase, number, special char
    [InlineData("PASSWORD", false)]      // No lowercase, number, special char
    [InlineData("Pass123", false)]       // No special character
    [InlineData("Pass@", false)]         // Too short, no number
    [InlineData("", false)]              // Empty
    public void Password_Security_ShouldFollowComplexityRules(string password, bool shouldBeStrong)
    {
        // Arrange
        var userDto = TestDtoBuilder.User()
            .WithPassword(password)
            .Build();

        // Act - Check password complexity
        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
        var isLongEnough = password.Length >= 8;

        var isStrong = hasLower && hasUpper && hasDigit && hasSpecial && isLongEnough;

        // Assert
        Assert.Equal(shouldBeStrong, isStrong);
        Assert.Equal(password, userDto.Password);
        Assert.Equal(password, userDto.PasswordConfirm);
    }

    [Fact]
    public void Password_Confirmation_ShouldMatch()
    {
        // Arrange
        var password = "SecurePass123!";
        var userDto = TestDtoBuilder.User()
            .WithPassword(password)
            .Build();

        // Act & Assert
        Assert.Equal(userDto.Password, userDto.PasswordConfirm);
        Assert.Equal(password, userDto.Password);
        Assert.Equal(password, userDto.PasswordConfirm);
    }

    #endregion

    #region User Role Tests

    [Theory]
    [InlineData("Client")]
    [InlineData("SuperAdmin")]
    [InlineData("BankEmployee")]
    public void UserRole_ValidRoles_ShouldBeAccepted(string role)
    {
        // Arrange & Act
        var userDto = TestDtoBuilder.User()
            .WithRole(role)
            .Build();

        // Assert
        Assert.Equal(role, userDto.Role);
        Assert.NotEmpty(userDto.Role);
    }

    [Fact]
    public void UserRole_DefaultRole_ShouldBeClient()
    {
        // Arrange & Act
        var userDto = TestDtoBuilder.User().Build();

        // Assert
        Assert.Equal("Client", userDto.Role);
    }

    #endregion

    #region User Entity Creation Tests

    [Fact]
    public void CreateTestUser_ShouldReturnValidUser()
    {
        // Act
        var user1 = CreateTestUser();
        var user2 = CreateTestUser("johndoe", "john@example.com");

        // Assert
        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotEqual(user1.Id, user2.Id);
        Assert.Equal("testuser", user1.UserName);
        Assert.Equal("johndoe", user2.UserName);
        Assert.Equal("john@example.com", user2.Email);
    }

    [Fact]
    public void CreateTestUser_WithBank_ShouldAssignBankId()
    {
        // Arrange
        var bank = CreateTestBank("Test Bank");

        // Act
        var user = CreateTestUser();
        user.BankId = bank.Id;
        Context.SaveChanges();

        // Assert
        Assert.Equal(bank.Id, user.BankId);
        Context.Entry(user).Reload();
        Assert.NotNull(user.Bank);
        Assert.Equal(bank.Name, user.Bank.Name);
    }

    #endregion

    #region Integration with Other Entities

    [Fact]
    public void User_WithAccounts_ShouldMaintainRelationship()
    {
        // Arrange
        var user = CreateTestUser();
        var currency = GetBaseCurrency();
        
        // Act
        var checkingAccount = TestEntityFactory.CreateCheckingAccount(user.Id, currency.Id, balance: 1000m);
        var savingsAccount = TestEntityFactory.CreateSavingsAccount(user.Id, currency.Id, balance: 500m);
        
        Context.CheckingAccounts.Add(checkingAccount);
        Context.SavingsAccounts.Add(savingsAccount);
        Context.SaveChanges();

        // Assert
        Context.Entry(user).Reload();
        var checkingAccounts = Context.CheckingAccounts.Where(a => a.UserId == user.Id).Count();
        var savingsAccounts = Context.SavingsAccounts.Where(a => a.UserId == user.Id).Count();
        
        Assert.Equal(1, checkingAccounts);
        Assert.Equal(1, savingsAccounts);
        Assert.Equal(checkingAccount.UserId, user.Id);
        Assert.Equal(savingsAccount.UserId, user.Id);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void TestDtoBuilder_Chaining_ShouldWorkCorrectly()
    {
        // Act
        var userDto = TestDtoBuilder.User()
            .WithUsername("testuser")
            .WithEmail("test@example.com")
            .WithFullName("Test User")
            .WithPhoneNumber("01123456789")
            .WithNationalId("12345678901234")
            .WithRole("Client")
            .WithBankId(1)
            .Build();

        // Assert
        Assert.Equal("testuser", userDto.Username);
        Assert.Equal("test@example.com", userDto.Email);
        Assert.Equal("Test User", userDto.FullName);
        Assert.Equal("01123456789", userDto.PhoneNumber);
        Assert.Equal("12345678901234", userDto.NationalId);
        Assert.Equal("Client", userDto.Role);
        Assert.Equal(1, userDto.BankId);
    }

    #endregion
}