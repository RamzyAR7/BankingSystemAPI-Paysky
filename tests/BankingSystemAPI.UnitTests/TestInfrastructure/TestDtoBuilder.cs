using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.UnitTests.TestInfrastructure;

/// <summary>
/// Builder pattern for creating test DTOs with fluent API.
/// </summary>
public static class TestDtoBuilder
{
    public static CurrencyReqDtoBuilder Currency() => new();
    public static UserReqDtoBuilder User() => new();
    public static UserEditDtoBuilder UserEdit() => new();
    public static BankReqDtoBuilder Bank() => new();
    public static CheckingAccountReqDtoBuilder CheckingAccount() => new();
    public static SavingsAccountReqDtoBuilder SavingsAccount() => new();
    public static DepositReqDtoBuilder Deposit() => new();
    public static WithdrawReqDtoBuilder Withdraw() => new();
    public static TransferReqDtoBuilder Transfer() => new();
}

public class CurrencyReqDtoBuilder
{
    private readonly CurrencyReqDto _dto = new() { Code = "USD", ExchangeRate = 1m, IsBase = false };

    public CurrencyReqDtoBuilder WithCode(string code) { _dto.Code = code; return this; }
    public CurrencyReqDtoBuilder WithRate(decimal rate) { _dto.ExchangeRate = rate; return this; }
    public CurrencyReqDtoBuilder AsBase() { _dto.IsBase = true; return this; }
    public CurrencyReqDto Build() => _dto;
}

public class UserReqDtoBuilder  
{
    private readonly UserReqDto _dto = new()
    {
        Username = "testuser",
        Email = "test@example.com", 
        Password = "Test@123",
        PasswordConfirm = "Test@123",
        PhoneNumber = "01234567890",
        FullName = "Test User",
        NationalId = "12345678901234",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
        Role = "Client"
    };

    public UserReqDtoBuilder WithUsername(string username) { _dto.Username = username; return this; }
    public UserReqDtoBuilder WithEmail(string email) { _dto.Email = email; return this; }
    public UserReqDtoBuilder WithPassword(string password) 
    { 
        _dto.Password = password; 
        _dto.PasswordConfirm = password; 
        return this; 
    }
    public UserReqDtoBuilder WithPhoneNumber(string phone) { _dto.PhoneNumber = phone; return this; }
    public UserReqDtoBuilder WithFullName(string fullName) { _dto.FullName = fullName; return this; }
    public UserReqDtoBuilder WithNationalId(string nationalId) { _dto.NationalId = nationalId; return this; }
    public UserReqDtoBuilder WithDateOfBirth(DateOnly dob) { _dto.DateOfBirth = dob; return this; }
    public UserReqDtoBuilder WithRole(string role) { _dto.Role = role; return this; }
    public UserReqDtoBuilder WithBankId(int? bankId) { _dto.BankId = bankId; return this; }
    public UserReqDto Build() => _dto;
}

public class UserEditDtoBuilder
{
    private readonly UserEditDto _dto = new()
    {
        Username = "testuser",
        Email = "test@example.com", 
        PhoneNumber = "01234567890",
        FullName = "Test User",
        NationalId = "12345678901234",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
    };

    public UserEditDtoBuilder WithUsername(string username) { _dto.Username = username; return this; }
    public UserEditDtoBuilder WithEmail(string email) { _dto.Email = email; return this; }
    public UserEditDtoBuilder WithPhoneNumber(string phone) { _dto.PhoneNumber = phone; return this; }
    public UserEditDtoBuilder WithFullName(string fullName) { _dto.FullName = fullName; return this; }
    public UserEditDtoBuilder WithNationalId(string nationalId) { _dto.NationalId = nationalId; return this; }
    public UserEditDtoBuilder WithDateOfBirth(DateOnly dob) { _dto.DateOfBirth = dob; return this; }
    public UserEditDto Build() => _dto;
}

public class BankReqDtoBuilder
{
    private readonly BankReqDto _dto = new() { Name = "Test Bank" };

    public BankReqDtoBuilder WithName(string name) { _dto.Name = name; return this; }
    public BankReqDto Build() => _dto;
}

public class CheckingAccountReqDtoBuilder
{
    private readonly CheckingAccountReqDto _dto = new()
    {
        UserId = Guid.NewGuid().ToString(),
        CurrencyId = 1,
        InitialBalance = 0m,
        OverdraftLimit = 1000m
    };

    public CheckingAccountReqDtoBuilder WithUserId(string userId) { _dto.UserId = userId; return this; }
    public CheckingAccountReqDtoBuilder WithCurrencyId(int currencyId) { _dto.CurrencyId = currencyId; return this; }
    public CheckingAccountReqDtoBuilder WithBalance(decimal balance) { _dto.InitialBalance = balance; return this; }
    public CheckingAccountReqDtoBuilder WithOverdraft(decimal limit) { _dto.OverdraftLimit = limit; return this; }
    public CheckingAccountReqDto Build() => _dto;
}

public class SavingsAccountReqDtoBuilder
{
    private readonly SavingsAccountReqDto _dto = new()
    {
        UserId = Guid.NewGuid().ToString(),
        CurrencyId = 1,
        InitialBalance = 0m,
        InterestRate = 0.05m,
        InterestType = InterestType.Monthly
    };

    public SavingsAccountReqDtoBuilder WithUserId(string userId) { _dto.UserId = userId; return this; }
    public SavingsAccountReqDtoBuilder WithCurrencyId(int currencyId) { _dto.CurrencyId = currencyId; return this; }
    public SavingsAccountReqDtoBuilder WithBalance(decimal balance) { _dto.InitialBalance = balance; return this; }
    public SavingsAccountReqDtoBuilder WithInterestRate(decimal rate) { _dto.InterestRate = rate; return this; }
    public SavingsAccountReqDto Build() => _dto;
}

public class DepositReqDtoBuilder
{
    private readonly DepositReqDto _dto = new() { AccountId = 1, Amount = 100m };

    public DepositReqDtoBuilder ToAccount(int accountId) { _dto.AccountId = accountId; return this; }
    public DepositReqDtoBuilder WithAmount(decimal amount) { _dto.Amount = amount; return this; }
    public DepositReqDto Build() => _dto;
}

public class WithdrawReqDtoBuilder
{
    private readonly WithdrawReqDto _dto = new() { AccountId = 1, Amount = 50m };

    public WithdrawReqDtoBuilder FromAccount(int accountId) { _dto.AccountId = accountId; return this; }
    public WithdrawReqDtoBuilder WithAmount(decimal amount) { _dto.Amount = amount; return this; }
    public WithdrawReqDto Build() => _dto;
}

public class TransferReqDtoBuilder
{
    private readonly TransferReqDto _dto = new() { SourceAccountId = 1, TargetAccountId = 2, Amount = 100m };

    public TransferReqDtoBuilder FromAccount(int sourceId) { _dto.SourceAccountId = sourceId; return this; }
    public TransferReqDtoBuilder ToAccount(int targetId) { _dto.TargetAccountId = targetId; return this; }
    public TransferReqDtoBuilder WithAmount(decimal amount) { _dto.Amount = amount; return this; }
    public TransferReqDto Build() => _dto;
}