# Domain Model: Accounts, Savings, Checking & Transactions

## 1. Aggregate Overview
| Aggregate Root | Related Entities | Notes |
|----------------|------------------|-------|
| Account (abstract) | CheckingAccount, SavingsAccount, AccountTransaction, Transaction | Account is a polymorphic root |
| Transaction | AccountTransaction (join entity) | Supports multi-account roles (source/target) |

## 2. Inheritance Hierarchy
```csharp
public abstract class Account {
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; protected set; }
    public string UserId { get; set; } = string.Empty;
    public int CurrencyId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }

    public virtual decimal GetAvailableBalance() => Balance;
    public virtual void Deposit(decimal amount) => Balance += amount;
    public virtual void Withdraw(decimal amount) {
        if (amount <= 0) throw new InvalidOperationException("Amount must be positive.");
        if (amount > GetAvailableBalance()) throw new InvalidOperationException("Insufficient funds.");
        Balance -= amount;
    }
}

public sealed class SavingsAccount : Account {
    public decimal InterestRate { get; set; }
    public InterestType InterestType { get; set; }
    public override decimal GetAvailableBalance() => Balance; // no overdraft
}

public sealed class CheckingAccount : Account {
    public decimal OverdraftLimit { get; set; }
    public bool IsOverdrawn() => Balance < 0;
    public override decimal GetAvailableBalance() => Balance + OverdraftLimit;
}
```

## 3. Transaction Model
```csharp
public class Transaction {
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TransactionType TransactionType { get; set; }
    public ICollection<AccountTransaction> AccountTransactions { get; set; } = new List<AccountTransaction>();
}

public class AccountTransaction {
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal Fees { get; set; }
    public TransactionRole Role { get; set; } // Source / Target
    public string TransactionCurrency { get; set; } = string.Empty; // snapshot
}
```

### 3.1 Roles
- `Source`: Debited account (withdraw / transfer sender)
- `Target`: Credited account (transfer recipient)
- Single-party transactions (Deposit / Withdraw) still create one `AccountTransaction` role = `Source` (or domain-chosen semantic)

## 4. Savings vs Checking Behavior
| Aspect | Savings | Checking |
|--------|---------|----------|
| Overdraft | Not allowed | Allowed up to OverdraftLimit |
| Interest | Periodic accrual | Typically none (unless business rules later) |
| Available Balance | Equals Balance | Balance + OverdraftLimit |
| Withdrawal rule | Must have funds | Can dip into overdraft |

## 5. Interest Accrual (Savings)
A background job (`AddInterestJob`) runs periodically:
1. Load eligible savings accounts (based on `InterestType` cadence)
2. Compute interest: `interest = Balance * (InterestRate / 100)`
3. Update `Balance += interest`
4. Append `InterestLog` entry with timestamp & amount
5. Persist changes (batched to reduce locking)

Pseudo:
```csharp
foreach (var account in dueAccounts) {
    var interest = account.Balance * account.InterestRate / 100m;
    account.Balance += interest;
    _interestLogs.Add(new InterestLog { Amount = interest, SavingsAccountId = account.Id });
}
await _uow.SaveAsync();
```

## 6. Transaction Lifecycle (Example: Transfer)
```
Validate source & target accounts
  ?
Check bank isolation (BankGuard)
  ?
Validate funds (source.GetAvailableBalance() >= amount)
  ?
Create Transaction (type=Transfer)
  ?
Create AccountTransaction (Source, debit)
  ?
Create AccountTransaction (Target, credit)
  ?
Apply domain methods: source.Withdraw(amount); target.Deposit(amount)
  ?
Persist (atomic save)
  ?
Return Result<TransactionResDto>
```

## 7. EF Core Mapping Considerations
| Concern | Approach |
|---------|----------|
| Table per Type | Inheritance: SavingsAccount / CheckingAccount share base table (TPH or TPT depending config) |
| Polymorphic queries | Use `OfType<SavingsAccount>()` for interest job |
| Concurrency | Leverage optimistic concurrency (row version if configured) |
| Snapshotting | Store currency code & fees in `AccountTransaction` (historical integrity) |
| Lazy vs Eager | Critical aggregates explicitly included (e.g., savings + InterestLogs) |

## 8. Consistency & Invariants
| Invariant | Enforcement |
|----------|-------------|
| Withdraw must not exceed available | `Account.Withdraw` override logic |
| Overdraft only for Checking | `CheckingAccount.GetAvailableBalance()` |
| Interest not applied twice in period | Interest job check using last log timestamp |
| Transfer requires both active accounts | ValidationService + BankGuard + account flags |
| Currency snapshot preserved | Copy `Code` into `AccountTransaction.TransactionCurrency` |

## 9. Fees & Extensions
Potential future hook points:
- Pre?withdraw fee calculation
- Transfer multi?currency conversion with rate snapshot
- Overdraft interest accumulation for negative checking balances

## 10. Example: Withdraw (Checking with Overdraft)
```csharp
if (amount > checking.GetAvailableBalance())
    return Result<TransactionResDto>.BadRequest("Insufficient funds.");
checking.Withdraw(amount); // may push Balance below zero but >= -OverdraftLimit
```

## 11. Example: Savings Interest Trigger Logic (Simplified)
```csharp
bool ShouldAddInterest(SavingsAccount acc) => acc.InterestType switch {
    InterestType.Monthly => acc.LastLogOrCreated().AddMonths(1) <= UtcNow,
    InterestType.Quarterly => acc.LastLogOrCreated().AddMonths(3) <= UtcNow,
    InterestType.Annually => acc.LastLogOrCreated().AddYears(1) <= UtcNow,
    InterestType.every5minutes => acc.LastLogOrCreated().AddMinutes(5) <= UtcNow,
    _ => false
};
```

## 12. DTO Projection Rationale
Domain objects are *not* exposed directly:
- Prevents overposting
- Shields internal invariants
- Allows independent evolution of API contracts
- Example: `TransactionResDto` flattens roles & amounts

## 13. Failure Points & Safeguards
| Point | Risk | Mitigation |
|-------|------|------------|
| Concurrent withdrawal | Double spend | EF concurrency + retry (in handler) |
| Interest job partial failure | Inconsistent accrual | Batch commit & per-account try/catch |
| Cross-bank transfer attempt | Data isolation breach | BankGuard.ValidateSameBank |
| Currency deactivation mid-use | Invalid operations | ValidationService checks `IsActive` |
| Overdraft abuse | Limit bypass | OverdraftLimit enforced in `GetAvailableBalance` |

## 14. Extension Roadmap
| Feature | Design Hook |
|---------|------------|
| Multi-currency ledger | Add conversion service + store source/target amounts |
| Reversal transactions | Introduce `ReversalTransactionId` link |
| Soft delete accounts | Add `IsDeleted` + query filters |
| Account statements | Aggregate `AccountTransaction` projections |
| Event sourcing | Emit domain events on Transaction commit |

## 15. Summary
The account & transaction domain relies on polymorphic account types, a normalized transaction + linking entity design, strict domain methods for balance mutation, and background interest accrual. Integrity is enforced at both domain (methods) and application (ValidationService + Guard) layers. Result pattern ensures all failure signals remain explicit, side?effect free, and testable.

---
*End of domain modeling documentation.*
