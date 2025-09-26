# Implementation Report: Banking System API

This document provides a detailed analysis of the implemented Banking System API solution against the technical task requirements.

---

## 1. Met Requirements

This section confirms that all mandatory requirements of the technical task have been successfully implemented.

| Requirement                 | Implementation Details                                                                                                                                                                                          |
| --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Account Types**           | The system implements `CheckingAccount` and `SavingsAccount` using a Table-Per-Concrete-Type (TPC) inheritance strategy in EF Core. `CheckingAccount` includes an `OverdraftLimit`, and `SavingsAccount` includes properties for `InterestRate` and `InterestType`. |
| **Core Operations**         | All specified operations are implemented in the `TransactionService` and exposed via the API: `Deposit`, `Withdraw`, `Transfer`, and `GetBalance`.                                                              |
| **Account-Specific Logic**  | The `Withdraw` logic correctly enforces the rules for each account type: overdrafts are permitted for `CheckingAccount` up to their limit, while `SavingsAccount` withdrawals are strictly limited to the available balance. |
| **Transaction Tracking**    | Every financial operation is recorded in the `Transactions` and `AccountTransactions` tables. These records store a comprehensive history, including transaction type, amount, fees, and timestamp. |
| **Database & EF Core**      | The solution uses Entity Framework Core for all data persistence. The database schema includes all required tables, and data access is managed via a robust Repository and Unit of Work pattern. |
| **System Design & OOP**     | The project is built on a clean, layered architecture (Presentation, Application, Domain, Infrastructure), promoting modularity and separation of concerns. It effectively uses interfaces, inheritance, and other OOP principles. |
| **RESTful API**             | A RESTful API is implemented with ASP.NET Core. The required endpoints are logically organized in the `AccountTransactionsController` and adhere to REST principles, using JSON for requests and responses. |
| **Validation & Errors**     | The system features robust validation at multiple levels: DTOs use data annotation attributes, services contain business rule validation, and a global `ExceptionHandlingMiddleware` provides consistent, secure error responses for the entire API. |

---

## 2. Bonus Points

This section details the implementation of the optional enhancements, all of which were successfully completed.

| Bonus Feature               | Implementation Details                                                                                                                                                                                                                            |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Unit Testing**            | The solution includes a dedicated unit test project (`BankingSystemAPI.UnitTests`) with extensive test coverage for services like `TransactionService`, `AccountService`, `RoleHierarchyService`, and more, ensuring the reliability of the core business logic. |
| **Multiple Currencies**     | The system fully supports multiple currencies. It features a `Currency` entity, a `CurrencyService` for management, and the `TransactionService` handles cross-currency transfers, including the calculation of appropriate fees. |
| **Multi-Account Users**     | The data model is designed so that a single `ApplicationUser` can own multiple `Account` entities, fulfilling the multi-account user requirement.                                                                              |
| **Swagger Documentation**   | The API is fully documented using Swagger/OpenAPI. All controllers and endpoints have been decorated with XML comments and attributes (`[ApiExplorerSettings]`, `[ProducesResponseType]`) to generate a rich, interactive API definition. The Postman collection has also been updated with detailed descriptions and examples. |
| **Input Validation & Logging** | DTOs use validation attributes to enforce data integrity at the model binding stage. Furthermore, structured logging (`ILogger`) is integrated throughout the application, from services to background jobs, providing a detailed trace of operations. |

---

## 3. Extra Features (Above and Beyond)

To build a truly robust and enterprise-ready solution, several advanced features were implemented that go significantly beyond the original scope of the task.

### Advanced RBAC & Hierarchy
The system includes a complete Role-Based Access Control system with full hierarchy management, exposed via a dedicated set of controllers:
- **`RoleController`**: For creating and deleting roles.
- **`RoleClaimsController`**: For assigning fine-grained permissions to roles.
- **`UserRolesController`**: For assigning roles to users.
- **`RoleHierarchyController`**: For defining the parent-child relationships between roles, which forms the core of the management authority system.

### Middleware & Action Filters
The request pipeline is protected and enhanced by several custom components:
- **`ExceptionHandlingMiddleware`**: A global error handler that catches all exceptions and returns clean, standardized error responses.
- **`RequestTimingMiddleware`**: Logs the processing time for every single request, helping to identify performance bottlenecks.
- **`RequestResponseLoggingFilter`**: Provides deep, structured logging of entire request and response objects for diagnostic purposes.
- **`PermissionFilter`**: An authorization filter that secures individual endpoints by checking for specific permission claims.
- **`RoleHierarchyFilter`**: An advanced authorization filter that enforces management authority based on the role hierarchy and provides a configurable whitelist for self-service actions.

### Secure Authentication Flow
A complete, secure authentication system was implemented, managed by the `AuthService`:
- **JWT Access Tokens**: Short-lived, claim-based tokens for API access.
- **Refresh Tokens**: Long-lived, securely stored in `HttpOnly` cookies to prevent XSS attacks.
- **Token Rotation**: Used refresh tokens are immediately invalidated and new ones are issued, preventing replay attacks.
- **Instant Invalidation**: A security stamp mechanism ensures that all of a user's sessions can be revoked instantly.

### Automated Background Jobs
The system uses .NET's `BackgroundService` to run essential maintenance and business logic tasks asynchronously:

- **`AddInterestJob`**: Automatically calculates and applies interest to all eligible savings accounts. It supports different interest types (Monthly, Quarterly, Annually). The job processes accounts in batches of 100 for performance, includes comprehensive concurrency handling with retries, and maintains detailed audit logs of all interest applications.

- **`RefreshTokenCleanupJob`**: Periodically removes expired and invalid refresh tokens from the database. It processes tokens in batches, handles concurrency conflicts with retry logic, and ensures the authentication system remains performant and secure by preventing token table bloat.

### Advanced Swagger Generation
The API documentation is automatically enhanced using custom `IOperationFilter` implementations:
- **`AuthResponsesOperationFilter`**: Automatically adds `401 Unauthorized` and `403 Forbidden` responses to all protected endpoints in Swagger.
- **`DefaultResponsesOperationFilter`**: Automatically adds common `400`, `404`, and `500` level error responses to endpoints, ensuring documentation is consistent and complete.

### Automated Database Seeding
The application includes a robust seeding mechanism (`IdentitySeeding`, `CurrencySeeding`) that runs on startup. It populates the database with essential data, including:
- Default currencies (USD, EUR, EGP, etc.).
- A `SuperAdmin` user and default roles (`Admin`, `Client`).
- A full set of permissions for every action.
- The initial role hierarchy (e.g., `SuperAdmin` > `Admin` > `Client`).

### Concurrency Control
To prevent data corruption from simultaneous update operations, the `Account` entity uses a `RowVersion` concurrency token implemented as optimistic concurrency control in EF Core. The `TransactionService` employs a robust retry mechanism to handle conflicts gracefully:

- **RowVersion Mechanism**: SQL Server automatically increments the `byte[]` RowVersion field on every update. EF Core includes the original RowVersion in UPDATE WHERE clauses, causing the operation to fail if the row was modified by another transaction.

- **Retry Logic**: All financial operations (Deposit, Withdraw, Transfer) are wrapped in `ExecuteWithRetryAsync`, which catches `DbUpdateConcurrencyException` and retries up to 3 times. On conflict, it reloads tracked entities via the Unit of Work pattern before retrying.

- **Benefits**: Ensures data integrity under high concurrency while maintaining good performance and user experience through automatic conflict resolution.

### Professional Design Patterns
- **Unit of Work & Repository Pattern**: The data access layer is cleanly abstracted using these patterns. This centralizes data logic, improves testability, and makes the application easier to maintain.
- **Automated Object Mapping**: The solution uses **AutoMapper** (`MappingProfile.cs`) to handle the complex transformation of data between domain entities and DTOs. This removes vast amounts of boilerplate mapping code and reduces the chance of human error.

---

## Database Schema

The following is the updated database schema based on the current entity configurations and context setup.

```dbml
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

Table RoleRelations {
  Id int [pk, increment]
  ParentRoleId nvarchar(450) [not null]
  ChildRoleId nvarchar(450) [not null]
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
    CreatedAt datetime [not null, default: `getutcdate()`]
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
Ref: AspNetRoles.Id < RoleRelations.ParentRoleId
Ref: AspNetRoles.Id < RoleRelations.ChildRoleId
Ref: Banks.Id < AspNetUsers.BankId

// Note: AccountTransaction.AccountId can refer to either CheckingAccount.Id or SavingsAccount.Id (polymorphic relationship)
// Note: RoleRelation has a unique index on (ParentRoleId, ChildRoleId)
```