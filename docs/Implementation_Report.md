# Implementation Report: Banking System API

This document provides a detailed analysis of the implemented Banking System API solution against the technical task requirements.

## 1. Met Requirements

This section confirms that all mandatory requirements of the technical task have been successfully implemented.

| Requirement | Implementation Details |
|---|---|
| Account Types | `CheckingAccount` and `SavingsAccount` implemented using Table-Per-Concrete-Type (TPC) inheritance in EF Core. `CheckingAccount` includes `OverdraftLimit`. `SavingsAccount` includes `InterestRate` and `InterestType`. |
| Core Operations | All specified operations implemented in `TransactionService` and exposed via API: `Deposit`, `Withdraw`, `Transfer`, `GetBalance`. |
| Account-Specific Logic | `Withdraw` enforces type-specific rules: overdrafts allowed for `CheckingAccount` (up to limit); `SavingsAccount` withdrawals limited to balance. |
| Transaction Tracking | Every operation recorded in `Transactions` and `AccountTransactions` tables; contains transaction type, amount, fees, timestamp. |
| Database & EF Core | EF Core used for persistence; Repository + Unit of Work pattern for data access. |
| System Design & OOP | Layered architecture (Presentation, Application, Domain, Infrastructure); uses interfaces, inheritance, SOLID principles. |
| RESTful API | Implemented with ASP.NET Core; endpoints organized (notably `AccountTransactionsController`) and return JSON. |
| Validation & Errors | DTO data annotations, service-level business validation, and global `ExceptionHandlingMiddleware` for consistent error responses. |

## 2. Bonus Points

The optional enhancements (bonus features) are implemented. Summary below:

| Bonus Feature | Implementation Details |
|---|---|
| Unit Testing | `BankingSystemAPI.UnitTests` project with extensive tests for services such as `TransactionService`, `AccountService`. |
| Multiple Currencies | `Currency` entity + `CurrencyService`; cross-currency transfers handled in `TransactionService` including fees calculation. |
| Multi-Account Users | `ApplicationUser` can own multiple `Account` entities. |
| Swagger Documentation | Full Swagger/OpenAPI documentation; controllers decorated with XML comments and attributes (`[ApiExplorerSettings]`, `[ProducesResponseType]`). Postman collection updated. |
| Input Validation & Logging | DTO validation attributes and structured logging (`ILogger`) across services and background jobs. |

## 3. Extra Features (Above and Beyond)

Advanced features are documented in tables for clarity.

### Advanced Role-Based Access Control (RBAC) with Scoped Authorization

| Component | Description |
|---|---|
| Controllers | `RoleController`, `RoleClaimsController`, `UserRolesController` — endpoints for managing roles, role claims (permissions), and user-role assignments. |
| ScopeResolver | Determines current user's `AccessScope` (Global, BankLevel, Self) based on role (e.g., `SuperAdmin`, `Admin`, `Client`). |
| Authorization Services | `AccountAuthorizationService`, `UserAuthorizationService`, `TransactionAuthorizationService` — enforce business rules according to resolved scope. Example: BankLevel scope restricts operations to the user's bank via `BankGuard`. |

### In-Memory Caching and Cache Services

| Component | Description |
|---|---|
| `ICacheService` | Abstraction for caching, allowing `IMemoryCache` or replacement with a distributed cache like Redis later. |
| `MemoryCacheService` | Concrete implementation using `.NET` `IMemoryCache`. |
| Cached Repositories | `RoleRepository`, `CurrencyRepository`, etc., use `ICacheService` to cache frequently used entities (by ID or keys) to reduce DB load. |
| Cache Invalidation | Repositories invalidate cache entries on create/update/delete to keep data consistent. |

### Specification Pattern

| Component | Description |
|---|---|
| `ISpecification<T>` | Contract for specifications (criteria, includes, order, paging). |
| `Specification<T>` | Base class implementing `ISpecification<T>` with a fluent API. |
| Repository Integration | `IGenericRepository<T>` accepts `ISpecification<T>` to build dynamic EF Core queries (e.g., `ApplySpecification`, `ListAsync`, `GetAsync`). |

### Middleware & Action Filters

| Component | Purpose |
|---|---|
| `ExceptionHandlingMiddleware` | Global error handler returning standardized error responses. |
| `RequestTimingMiddleware` | Logs processing time for each request. |
| `RequestResponseLoggingFilter` | Logs full request and response objects for diagnostics. |
| `PermissionFilter` | Authorization filter that checks permission claims on endpoints. |

### Secure Authentication Flow

| Component | Details |
|---|---|
| JWT Access Tokens | Short-lived, claim-based tokens for API access. |
| Refresh Tokens | Long-lived tokens stored in HttpOnly cookies to protect against XSS. |
| Token Rotation | Used refresh tokens are invalidated and rotated to new tokens to prevent replay attacks. |
| Instant Invalidation | Security stamp mechanism to revoke all user sessions immediately. |

### Automated Background Jobs

| Job | Description |
|---|---|
| `AddInterestJob` | Calculates and applies interest for savings accounts (Monthly/Quarterly/Annually). Processes in batches (e.g., 100), handles concurrency with retries, and writes interest audit logs. |
| `RefreshTokenCleanupJob` | Periodically cleans expired/invalid refresh tokens in batches; includes concurrency handling and retries. |

### Advanced Swagger Generation

| Filter | Purpose |
|---|---|
| `AuthResponsesOperationFilter` | Adds `401` and `403` responses automatically to protected endpoints. |
| `DefaultResponsesOperationFilter` | Adds common `400`, `404`, `500` responses for consistency across endpoints. |

### Automated Database Seeding

| Seeder | What it seeds |
|---|---|
| `IdentitySeeding` | Creates default identity data (SuperAdmin, default roles). |
| `CurrencySeeding` | Adds base currencies (USD, EUR, EGP, etc.). |
| `BankSeeding` | Adds default banks and related initial data. |

### Concurrency Control

| Mechanism | Details |
|---|---|
| RowVersion (Optimistic Concurrency) | `Account` entity includes a `RowVersion` token; SQL Server increments the `rowversion` field on updates; EF Core includes original `RowVersion` in UPDATE `WHERE` clauses so concurrent updates cause an exception. |
| Retry Logic | `ExecuteWithRetryAsync` wraps financial operations (`Deposit`, `Withdraw`, `Transfer`), catches `DbUpdateConcurrencyException`, reloads tracked entities via Unit of Work, and retries up to 3 times. |

### Professional Design Patterns

| Pattern | Use |
|---|---|
| Unit of Work & Repository | Centralizes data access and improves testability and maintainability. |
| AutoMapper | `MappingProfile.cs` used to map between domain entities and DTOs, reducing boilerplate mapping code. |

## Database Schema

The following is the updated database schema based on the current entity configurations and context setup.

```sql
Table AspNetUsers {
  Id nvarchar(450) [pk]
  UserName nvarchar(256)
  NormalizedUserName nvarchar(256)
  Email nvarchar(256)
  NormalizedEmail nvarchar(256)
  EmailConfirmed bit
  PasswordHash nvarchar(max)
  SecurityStamp nvarchar(max)
  ConcurrencyStamp nvarchar(max)
  PhoneNumber nvarchar(max)
  PhoneNumberConfirmed bit
  TwoFactorEnabled bit
  LockoutEnd datetimeoffset
  LockoutEnabled bit
  AccessFailedCount int
  NationalId varchar(20) [unique, not null]
  DateOfBirth date [not null]
  FullName nvarchar(200)
  IsActive bit [not null, default: true]
  BankId int
}

Table RefreshTokens {
  Token nvarchar(200) [pk]
  UserId nvarchar(450)
  CreatedOn datetime [not null]
  ExpiresOn datetime [not null]
  AbsoluteExpiresOn datetime [not null]
  RevokedOn datetime
}

Table AspNetRoles {
  Id nvarchar(450) [pk]
  Name nvarchar(256)
  NormalizedName nvarchar(256)
  ConcurrencyStamp nvarchar(max)
}

Table AspNetUserRoles {
  UserId nvarchar(450)
  RoleId nvarchar(450)
}

Table AspNetUserClaims {
  Id int [pk, increment]
  UserId nvarchar(450)
  ClaimType nvarchar(max)
  ClaimValue nvarchar(max)
}

Table AspNetRoleClaims {
  Id int [pk, increment]
  RoleId nvarchar(450)
  ClaimType nvarchar(max)
  ClaimValue nvarchar(max)
}

Table Currencies {
  Id int [pk, increment]
  Code varchar(10) [unique, not null]
  IsBase bit
  ExchangeRate decimal(18,6)
  IsActive bit [not null, default: true]
}

Table CheckingAccounts {
  Id int [pk, increment]
  AccountNumber varchar(50) [unique, not null]
  Balance decimal(18,2)
  CreatedDate datetime [not null]
  RowVersion rowversion
  UserId nvarchar(450)
  CurrencyId int
  OverdraftLimit decimal(18,2)
  IsActive bit [not null, default: true]
}

Table SavingsAccounts {
  Id int [pk, increment]
  AccountNumber varchar(50) [unique, not null]
  Balance decimal(18,2)
  CreatedDate datetime [not null]
  RowVersion rowversion
  UserId nvarchar(450)
  CurrencyId int
  InterestRate decimal(5,2)
  InterestType varchar(20)
  IsActive bit [not null, default: true]
}

Table InterestLogs {
  Id int [pk, increment]
  SavingsAccountId int
  Amount decimal(18,2)
  Timestamp datetime [not null]
  SavingsAccountNumber varchar(50)
}

Table Transactions {
  Id int [pk, increment]
  Type varchar(20) [not null]
  Timestamp datetime [not null]
}

Table AccountTransactions {
  AccountId int
  TransactionId int
  TransactionCurrency varchar(10)
  Amount decimal(18,2)
  Role varchar(20) [not null]
  Fees decimal(18,2) [default: 0]
}

Table Banks {
  Id int [pk, increment]
  Name nvarchar(200) [not null]
  IsActive bit [not null, default: true]
  CreatedAt datetime [not null, default: getutcdate()]
}

Ref: AspNetUsers.Id < RefreshTokens.UserId
Ref: AspNetUsers.Id < CheckingAccounts.UserId
Ref: AspNetUsers.Id < SavingsAccounts.UserId
Ref: AspNetRoles.Id < AspNetRoleClaims.RoleId
Ref: AspNetUsers.Id < AspNetUserClaims.UserId
Ref: AspNetUsers.Id < AspNetUserRoles.UserId
Ref: AspNetRoles.Id < AspNetUserRoles.RoleId
Ref: Currencies.Id < CheckingAccounts.CurrencyId
Ref: Currencies.Id < SavingsAccounts.CurrencyId
Ref: SavingsAccounts.Id < InterestLogs.SavingsAccountId
Ref: Transactions.Id < AccountTransactions.TransactionId
Ref: Banks.Id < AspNetUsers.BankId
