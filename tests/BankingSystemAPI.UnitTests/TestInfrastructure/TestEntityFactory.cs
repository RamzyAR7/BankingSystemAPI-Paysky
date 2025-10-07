#region Usings
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.UnitTests.TestInfrastructure;

/// <summary>
/// Factory for creating test entities with valid data.
/// </summary>
public static class TestEntityFactory
{
    public static CheckingAccount CreateCheckingAccount(
        string userId,
        int currencyId,
        decimal balance = 0m,
        decimal overdraftLimit = 1000m,
    string? accountNumber = null)
    {
        return new CheckingAccount
        {
            AccountNumber = accountNumber ?? GenerateAccountNumber(),
            Balance = balance,
            UserId = userId,
            CurrencyId = currencyId,
            OverdraftLimit = overdraftLimit,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
    }

    public static SavingsAccount CreateSavingsAccount(
        string userId,
        int currencyId,
        decimal balance = 0m,
        decimal interestRate = 0.05m,
        InterestType interestType = InterestType.Monthly,
    string? accountNumber = null)
    {
        return new SavingsAccount
        {
            AccountNumber = accountNumber ?? GenerateAccountNumber(),
            Balance = balance,
            UserId = userId,
            CurrencyId = currencyId,
            InterestRate = interestRate,
            InterestType = interestType,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
    }

    public static Transaction CreateTransaction(
        TransactionType type = TransactionType.Deposit,
        DateTime? timestamp = null)
    {
        return new Transaction
        {
            TransactionType = type,
            Timestamp = timestamp ?? DateTime.UtcNow,
            AccountTransactions = new List<AccountTransaction>()
        };
    }

    public static AccountTransaction CreateAccountTransaction(
        int accountId,
        int transactionId,
        decimal amount,
        TransactionRole role = TransactionRole.Target,
        string currency = "USD",
        decimal fees = 0m)
    {
        return new AccountTransaction
        {
            AccountId = accountId,
            TransactionId = transactionId,
            Amount = amount,
            Role = role,
            TransactionCurrency = currency,
            Fees = fees
        };
    }

    private static string GenerateAccountNumber()
    {
        return $"ACC{Random.Shared.Next(100000, 999999)}";
    }
}
