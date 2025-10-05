# BankingSystemAPI: Full Application Manual & Technical Book

---
## Table of Contents
1. Introduction & Vision
2. Solution Structure & Layered Architecture
3. Database Design: Diagram & Entity Relations
4. Domain Model Deep Dive
5. Application Services: Full Details
6. Controllers & API Endpoints
7. DTOs & Data Contracts
8. Jobs & Background Processing
9. Middleware & Action Filters
10. Authentication, Refresh Token, & Security
11. Role Hierarchy, Permissions, & RBAC
12. Business Logic: Fees, Currency Conversion, Interest
13. Extensibility, Patterns, & Best Practices
14. Advanced Scenarios & Edge Cases
15. Power Points: Why This App Stands Out
16. Appendix: Code Samples & Diagrams

---

## 1. Introduction & Vision
BankingSystemAPI is a modern, enterprise-grade banking backend built on .NET 8. The project has been refactored to adopt CQRS (Command Query Responsibility Segregation) implemented via MediatR, a Result&lt;T&gt; pattern for standardized service responses, FluentValidation for request-level validation, and the Specification pattern for composable, testable queries. Pipeline behaviors (validation, caching, logging) and lightweight caching have been introduced to improve performance and keep handlers focused on business logic. It continues to support multi-user, multi-role, secure financial operations and is designed for extensibility, maintainability, and real-world banking needs.

### Key Features
- Modular, layered architecture
- Advanced RBAC (Role-Based Access Control) with hierarchy
- Secure JWT authentication and refresh token flow
- Multi-currency, fee calculation, and interest accrual
- Background jobs for cleanup and business logic
- Action filters and middleware for logging, security, and rate limiting
- Full audit logging and diagnostics
- Extensible for new business requirements

---

## 2. Solution Structure & Layered Architecture

### Directory Layout
```
src/
  BankingSystemAPI.Domain/         # Domain models, enums, constants
  BankingSystemAPI.Application/    # Application logic, DTOs, interfaces, services
  BankingSystemAPI.Infrastructure/ # Data access, jobs, identity, migrations
  BankingSystemAPI.Presentation/   # API controllers, filters, middleware
  docs/                            # Documentation
  ...
tests/
  BankingSystemAPI.UnitTests/      # Unit and integration tests
```

### Layered Architecture Diagram
```
+-------------------+
|   Presentation    |  <-- Controllers, Filters, Middleware
+-------------------+
         |
+-------------------+
|   Application     |  <-- Services, DTOs, Business Logic
+-------------------+
         |
+-------------------+
|   Domain          |  <-- Entities, Value Objects, Enums
+-------------------+
         |
+-------------------+
|   Infrastructure  |  <-- Data Access, Jobs, Identity
+-------------------+
```

### Layer Responsibilities

- **Presentation**: Thin HTTP controllers that delegate Commands and Queries to MediatR. Controllers handle request binding, authorization, and return standardized Result&lt;T&gt; responses (success / failure payloads). They do not contain domain logic.

- **Application**: Implements CQRS. Requests are modeled as Commands and Queries handled by MediatR handlers. This layer contains DTOs, MediatR handlers, FluentValidation validators, pipeline behaviors (ValidationBehavior, optional CachingBehavior, LoggingBehavior), application services, mapping profiles, and modules that orchestrate business operations without direct persistence concerns.

- **Domain**: Pure business rules, entities, aggregates, domain exceptions (e.g., BusinessRuleException), and value objects. Domain contains no framework-specific code and remains the single source of truth for business invariants.

- **Infrastructure**: Data persistence (EF Core), repositories, Unit of Work, Specification implementations used by repository/query handlers, caching implementations, background jobs, identity plumbing, and any external integrations.

---

## 3. Database Schema and Entity Configurations

This section provides a detailed, code-first overview of the database schema as defined by the Entity Framework Core configurations. This is the ground truth for table structures, constraints, and relationships.

### **Inheritance Strategy: Table-Per-Concrete-Type (TPC)**
- The `Account` entity and its children (`CheckingAccount`, `SavingsAccount`) use the TPC mapping strategy (`UseTpcMappingStrategy()`).
- This means there is **no** `Accounts` table in the database. Instead, there are separate `CheckingAccounts` and `SavingsAccounts` tables, each containing all the properties of the base `Account` class plus their own specific properties.
- This strategy is chosen for performance, as it avoids the joins required by other inheritance models.

---

### **Entity: `ApplicationUser`**
- **Table**: Mapped to `AspNetUsers` by default.
- **Properties**:
  - `NationalId`: `string(20)`, required, and has a **unique index** to prevent duplicate entries.
  - `FullName`: `string(200)`.
  - `IsActive`: `bool`, required, with a default value of `true`.
  - `BankId`: `int`, nullable. Foreign key to the `Banks` table.
- **Relationships**:
  - **One-to-Many with `Account`**: A user can have many accounts. If a user is deleted, all their associated accounts are also deleted (`OnDelete(DeleteBehavior.Cascade)`).
  - **One-to-Many with `RefreshToken`**: A user can have many refresh tokens. Deleting a user also deletes their tokens (`OnDelete(DeleteBehavior.Cascade)`).
  - **Many-to-One with `Bank`**: A user can be associated with a bank.

---

### **Entity: `Account` (Base Class)**
- **Properties**:
  - `AccountNumber`: `string(50)`, required.
  - `Balance`: `decimal(18, 2)`.
  - `RowVersion`: This is a special `byte[]` property configured as a **concurrency token** (`IsRowVersion()`). EF Core uses this to detect optimistic concurrency conflicts. If two users try to update the same account simultaneously, the second user's update will fail, preventing data corruption.
  - `IsActive`: `bool`, required, defaults to `true`.
- **Relationships**:
  - **Many-to-One with `Currency`**: Each account must have a currency.

### **Concurrency Control with RowVersion**

To prevent data corruption from simultaneous update operations, the `Account` entity uses a `RowVersion` concurrency token. This implements optimistic concurrency control in EF Core.

- **How it Works**:
  - The `RowVersion` is a `byte[]` property automatically managed by SQL Server (configured with `IsRowVersion()` in EF Core).
  - Every time an account is updated, SQL Server increments the `RowVersion` value.
  - When EF Core attempts to save changes, it includes the original `RowVersion` in the WHERE clause.
  - If another transaction has modified the row since it was loaded, the `RowVersion` will have changed, causing the update to affect zero rows.
  - EF Core throws a `DbUpdateConcurrencyException` when this happens.

- **Handling in TransactionService**:
  - All financial operations (Deposit, Withdraw, Transfer) are wrapped in an `ExecuteWithRetryAsync` method.
  - On `DbUpdateConcurrencyException`, the method retries the operation up to 3 times.
  - Before retrying, it calls `_unitOfWork.ReloadTrackedEntitiesAsync()` to refresh the entity with the latest data from the database.
  - If all retries fail, it throws an `InvalidAccountOperationException` with a user-friendly message.
  - This ensures data integrity under high concurrency while providing a good user experience.

- **Benefits**:
  - Prevents lost updates and inconsistent data.
  - Allows multiple users to operate concurrently without blocking.
  - Automatic conflict detection and resolution through retries.

---

### **Entity: `CheckingAccount`**
- **Table**: `CheckingAccounts`.
- **Inheritance**: Inherits all properties from `Account`.
- **Properties**:
  - `OverdraftLimit`: `decimal(18, 2)`.

---

### **Entity: `SavingsAccount`**

**Table**: `SavingsAccounts`.
**Inheritance**: Inherits all properties from `Account`.
**Properties**:
  - `InterestRate`: `decimal(5, 2)`.
  - `InterestType`: Stored as a `string` in the database. Can be `Monthly`, `Quarterly`, or `Annually`.
  - `IsActive`: `bool`, required, defaults to `true`.
**Relationships**:
  - **One-to-Many with `InterestLog`**: A savings account can have many interest log entries.
  - **InterestLog**: Records every interest accrual, including amount, timestamp, and account details.

#### **Interest Accrual Actions**
- Interest is automatically calculated and applied by the `AddInterestJob` background service.
- Eligibility is determined by the `ShouldAddInterest` method, based on `InterestType` and last interest log date.
- When interest is due:
  - The system calculates `balance * interestRate / 100`.
  - Adds the amount to the account balance.
  - Creates a new `InterestLog` entry for auditing.
- All actions are atomic and retried on concurrency conflicts.

---

### **Entity: `Transaction`**
- **Table**: `Transactions`.
- **Properties**:
  - `TransactionType`: Stored as a `string`.
  - `Timestamp`: `datetime`, required.
- **Relationships**:
  - **One-to-Many with `AccountTransaction`**: A single transaction (like a transfer) involves multiple account transaction records.

---

### **Entity: `AccountTransaction` (Junction Table)**
- **Table**: `AccountTransactions`.
- **Purpose**: This is a many-to-many junction entity that links `Account` and `Transaction`.
- **Primary Key**: A **composite key** made of `{ AccountId, TransactionId }`.
- **Properties**:
  - `Amount`: `decimal(18, 2)`.
  - `Fees`: `decimal(18, 2)`, defaults to `0`.
  - `Role`: Stored as a `string` (e.g., "Source", "Target").

---

### **Entity: `Currency`**
- **Table**: `Currencies`.
- **Properties**:
  - `Code`: `string(10)`, required, with a **unique index**.
  - `ExchangeRate`: `decimal(18, 6)` to allow for high precision.
  - `IsActive`: `bool`, required, defaults to `true`.

---

### **Entity: `RoleRelation`**
- **Table**: `RoleRelations`.
- **Purpose**: This table creates the role hierarchy structure.
- **Primary Key**: `Id` (int).
- **Constraints**: Has a **unique index** on the combination of `{ ParentRoleId, ChildRoleId }` to prevent duplicate hierarchy entries.
- **Relationships**:
  - Defines two relationships back to the `AspNetRoles` table (one for `ParentRole`, one for `ChildRole`).
  - Deletion behavior is set to `Restrict` to prevent a role from being deleted if it is part of a hierarchy, ensuring integrity.

---

### **Entity: `RefreshToken`**
- **Table**: `RefreshTokens`.
- **Primary Key**: The `Token` string itself is the primary key.
- **Properties**:
  - `Token`: `string(200)`, required.
  - All date properties (`CreatedOn`, `ExpiresOn`, `AbsoluteExpiresOn`) are required.

---

### **Entity: `InterestLog`**
- **Table**: `InterestLogs`.
- **Properties**:
  - `Amount`: `decimal(18, 2)`.

---

### **Entity: `Bank`**
- **Table**: `Banks`.
- **Properties**:
  - `Name`: `string(200)`, required.
  - `IsActive`: `bool`, required, defaults to `true`.
  - `CreatedAt`: `datetime`, required, defaults to `getutcdate()`.
- **Relationships**:
  - **One-to-Many with `ApplicationUser`**: A bank can have many users.

---

## 4. Domain Model Deep Dive

### User
- Identity, profile, status, roles
- Methods: Activate, Deactivate, ChangePassword

### Account
- Types: Checking, Savings
- Properties: Balance, Overdraft, Interest, Currency
- Methods: Deposit, Withdraw, Transfer, AccrueInterest

### Transaction
- Types: Deposit, Withdraw, Transfer
- Properties: Amount, Fee, Status, Timestamp
- Methods: Validate, ApplyFee, Rollback

### Role & RoleClaims
- Hierarchy: Parent/child, management rights
- Permissions: Fine-grained claims

### Currency
- Properties: Code, Symbol, Rate
- Methods: Convert, UpdateRate

### InterestLog
- Properties: Rate, Period, AccruedAmount
- Methods: Calculate, LogAccrual

### Bank
- Properties: Name, IsActive, CreatedAt
- Methods: Activate, Deactivate

---

## 5. Application Services: Full Details

This section provides a deep dive into the core services of the application, explaining their responsibilities, methods, and the logic they encapsulate.

### Patterns & Conventions (CQRS, MediatR, Result&lt;T&gt;, Validation, Specifications, Caching)

This codebase was refactored to follow a clear set of application-level patterns and conventions to improve separation of concerns, testability, and performance. The most important conventions are listed here so developers and reviewers understand how to add new features consistently.

- CQRS via MediatR
  - All user-facing operations are modeled as either *Commands* (state-changing) or *Queries* (read-only) and sent through MediatR. Controllers are thin and only translate HTTP requests into Commands/Queries.
  - Typical shapes:
    - Command: `CreateUserCommand : IRequest<Result&lt;UserDto&gt;>`
    - Query: `GetUsersQuery : IRequest<Result&lt;PagedResult&lt;UserDto&gt;&gt;>`

- Result&lt;T&gt; Pattern
  - Handlers return a standardized `Result&lt;T&gt;` (or `Result` for void) which carries either a successful value or a list of errors. This keeps controllers simple: they translate Result objects to appropriate HTTP responses (200/201, 400, 404, 409, etc.).

- FluentValidation and ValidationBehavior
  - Every Command/Query that accepts input has a corresponding FluentValidation validator (e.g., `CreateUserCommandValidator`).
  - A `ValidationBehavior<TRequest,TResponse>` runs as a MediatR pipeline behavior and short-circuits requests that fail validation, returning a `Result` containing validation errors.

- Pipeline Behaviors (order and responsibilities)
  - The typical pipeline order is: **Validation -> Caching (optional) -> Logging -> Handler**.
  - Behaviors are centralized and reusable (examples in `Application/Behaviors/`).

- Caching
  - Query handlers may be decorated with a caching behavior or explicitly use an `ICacheService`. Caching keys are deterministic and usually include the query type name and serialized parameters.
  - Cache duration is configurable per query and may use sliding or absolute expirations depending on the handler.

- Specification Pattern for Queries
  - Read-side logic uses the Specification pattern to compose filters, pagination, and ordering. Repositories accept specifications (e.g., `PagedSpecification<T>`) and translate them to EF queries. This keeps query logic testable and composable.

- Handler Signature Examples
  - Handler for a command:

    ```csharp
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
    {
        public async Task<Result<UserDto>> Handle(CreateUserCommand req, CancellationToken ct)
        {
            // ... business logic via services/repositories
        }
    }
    ```

  - Handler for a query:

    ```csharp
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
    {
        public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery req, CancellationToken ct)
        {
            // build specification, query repository, map to DTOs, return Result
        }
    }
    ```

These conventions are enforced by code structure and examples in the `Application` project. New features should follow the same patterns to maintain consistency.

---

### **AuthService**: Authentication and Token Management

The `AuthService` is the heart of the security system. It handles user login, token generation, refresh token mechanics, and logout processes.

#### Key Responsibilities:
- Validating user credentials.
- Generating JWT (JSON Web Tokens) access tokens.
- Managing the entire refresh token lifecycle (generation, validation, rotation, and cookie management).
- Invalidating tokens upon logout or revocation.
- Ensuring security policies like active user checks are enforced.

#### Methods Deep Dive:

- **`LoginAsync(LoginReqDto request)`**
  - **Logic**:
    1. Finds the user by email.
    2. Verifies the provided password.
    3. **Crucially, it checks if `user.IsActive` is `true`. If not, login is blocked.** This is a key security feature to disable accounts.
    4. It also verifies the user has at least one role assigned before proceeding.
    5. Calls `CreateJwtToken` to generate a new access token.
    6. Generates a new, cryptographically secure `RefreshToken`.
    7. **Token Rotation**: It revokes any old refresh tokens and adds the new one to the user's record.
    8. **Cookie Management**: The new refresh token is set in a secure, `HttpOnly` cookie, preventing access from client-side scripts.
    9. Returns a comprehensive `AuthResultDto` with the access token, user details, roles, and token expiration information.

- **`RefreshTokenAsync(string? tokenFromRequest)`**
  - **Logic (The Refresh Token Flow)**:
    1. Retrieves the refresh token from the incoming `HttpOnly` cookie.
    2. Finds the user associated with that specific refresh token.
    3. **Security Checks**:
       - Is the token revoked (`RevokedOn != null`)?
       - Is the token still active (`IsActive`)?
       - Has the token passed its absolute expiry date (`IsAbsoluteExpired`)?
       - If any of these fail, the request is rejected, forcing the user to log in again.
    4. **Token Rotation**: The used refresh token is immediately revoked (`RevokedOn = DateTime.UtcNow`), and a **new** refresh token is generated and stored. This is a critical security measure to prevent token replay attacks.
    5. A new JWT access token is generated.
    6. The new refresh token is sent back to the client in a new `HttpOnly` cookie.
    7. This process provides a seamless and secure way for users to stay logged in without re-entering credentials.

- **`LogoutAsync(string userId)`**
  - **Logic**:
    1. Finds the user by their ID.
    2. Revokes all active refresh tokens for that user.
    3. **Crucially, it calls `_userManager.UpdateSecurityStampAsync(user)`.** This changes a security value inside the user's record, which causes all previously issued JWT access tokens for that user to become invalid instantly.
    4. Deletes the `refreshToken` cookie from the browser.

- **`CreateJwtToken(ApplicationUser user)`**
  - **Logic**:
    1. Gathers all roles assigned to the user.
    2. **Role Hierarchy Integration**: It calls `_roleHierarchyService.GetChildrenAsync(role)` to get all descendant roles and adds them to the token's claims. This means a "Manager" token will also contain claims for "Teller", granting them implicit access.
    3. Gathers all permission claims associated with those roles.
    4. Bundles standard claims (user ID, email, security stamp) and the role/permission claims into a JWT.
    5. Signs the token with the secret key from `JwtSettings`.

---

### **TransactionService**: Core Financial Operations

This service handles all financial transactions, ensuring data integrity, applying business rules like fees, and handling concurrency.

#### Key Responsibilities:
- Executing deposits, withdrawals, and transfers.
- Calculating and applying transaction fees.
- Handling cross-currency conversions during transfers.
- Ensuring atomicity of operations using database transactions.
- Implementing a retry mechanism to handle `DbUpdateConcurrencyException`.

#### Methods Deep Dive:

- **`DepositAsync(DepositReqDto request)`**
  - **Logic**:
    1. Validates that the deposit amount is positive.
    2. Wraps the core logic in an `ExecuteWithRetryAsync` block to handle potential concurrency issues.
    3. Fetches the account and validates that it exists and is active.
    4. Calls the `account.Deposit(amount)` domain method to update the balance.
    5. Creates a `Transaction` record and a corresponding `AccountTransaction` record to log the details of the operation (who, what, when).
    6. Saves all changes to the database within a single unit of work.

- **`WithdrawAsync(WithdrawReqDto request)`**
  - **Logic**:
    1. Similar validation and retry logic as `DepositAsync`.
    2. Calls the `account.Withdraw(amount)` domain method. This method contains the critical business rule: it checks if the withdrawal would result in a negative balance that exceeds any overdraft limit. If so, it throws an `InvalidAccountOperationException`.
    3. Creates `Transaction` and `AccountTransaction` records.
    4. Saves changes.

- **`TransferAsync(TransferReqDto request)`**
  - **Logic (The most complex operation)**:
    1. Validates that the amount is positive and that the source and target accounts are not the same.
    2. Uses `ExecuteWithRetryAsync` for concurrency safety.
    3. Fetches both the source and target accounts, ensuring they exist and are active.
    4. **Currency Conversion & Fees**:
       - It checks if the source and target accounts have different currencies.
       - If they do, it calls `_transactionHelperService.ConvertAsync` to get the equivalent amount in the target currency.
       - It calculates the transaction fee based on whether it's a same-currency (0.5%) or cross-currency (1.0%) transfer. The fee is calculated based on the source amount.
    5. **Database Transaction**: It wraps the financial movements in an explicit `_unitOfWork.BeginTransactionAsync()`.
    6. **Atomicity**:
       - It withdraws the `amount + fee` from the source account.
       - It deposits the `targetAmount` (which may have been converted) into the target account.
       - It creates a single `Transaction` record and **two** `AccountTransaction` records (one for the source, one for the target), linking them together.
    7. **Commit/Rollback**: If all steps succeed, it commits the database transaction. If any step fails, it rolls back all changes, ensuring the system's financial state remains consistent.

- **`GetAllAsync(...)` & `GetByAccountIdAsync(...)`**
  - **Logic**: These methods retrieve transaction histories.
  - **Role Hierarchy Authorization**: They don't just return all data. They check the current user's roles and use the `_roleHierarchyService` to determine which transactions the user is allowed to see. A "Manager" can see transactions for all "Tellers" and customers they manage, but a "Teller" cannot see the Manager's transactions. This is a powerful, centralized security feature.

---

### **TransactionHelperService**: Currency Conversion

A specialized service dedicated to handling currency conversions.

- **`ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount)`**
  - **Logic**:
    1. Fetches the `Currency` entities for both the source and target codes.
    2. If the currencies are the same, it returns the original amount.
    3. It handles three conversion scenarios:
       - **From Base Currency**: `amount * toCurrency.ExchangeRate`
       - **To Base Currency**: `amount / fromCurrency.ExchangeRate`
       - **Cross-Currency (Neither is Base)**: It first converts the `from` amount to the base currency (`amount / fromCurrency.ExchangeRate`), and then converts that base amount to the `to` currency.

---

### **AccountService**: Account Lifecycle Management

This service manages the lifecycle of all account types.

- **Methods**:
  - `GetAccountByIdAsync`, `GetAccountByAccountNumberAsync`, `GetAccountsByUserIdAsync`, `GetAccountsByNationalIdAsync`: Provides flexible methods to retrieve account data, including related entities like `User`, `Currency`, and `InterestLogs` for savings accounts.
  - `DeleteAccountAsync`: Enforces the business rule that an account cannot be deleted if it has a positive balance.
  - `SetAccountActiveStatusAsync`: Allows administrators to enable or disable an account, which is checked by the `TransactionService` before any operation.

---

### **RoleHierarchyService**: Manages Role Relationships

This is a critical infrastructure service that underpins the application's advanced RBAC (Role-Based Access Control).

- **Methods**:
  - `AddParentAsync`, `RemoveParentAsync`: These methods create (`RoleRelation`) the directional links between roles, establishing the hierarchy (e.g., making "Manager" a parent of "Teller").
  - `GetParentsAsync`, `GetChildrenAsync`: Retrieve immediate relatives of a role.
  - `GetAncestorsAsync`, `GetDescendantsAsync`: Recursively traverse the role hierarchy to get all roles above or below a given role. This is used by the `AuthService` to include all inherited roles in a user's JWT.
  - **`CanManageAsync(string actingRole, string targetRole)`**: The most important method. It determines if `actingRole` has authority over `targetRole` by checking if `targetRole` is a descendant of `actingRole`. This is the core of the `RoleHierarchyFilter`.
  - `GetRoleHierarchyAsync`: Provides a full map of the entire role structure, useful for admin UIs.


---

## 6. Controllers & API Endpoints

### Recent documentation updates

The codebase documentation has been synchronized with the Postman collection. The following XML documentation remarks were added to controller actions so they appear in generated API docs (Swagger/OpenAPI) when XML comments are enabled:

- `UserController` (`/api/users`)
  - Create (POST `/api/users`): added roles, banks table, and an example request body in XML remarks.
  - Update (PUT `/api/users/{userId}`): added roles, banks table, and an example request body in XML remarks.

- `CheckingAccountController` (`/api/checking-accounts`)
  - Create (POST `/api/checking-accounts`): added currencies table and an example request body in XML remarks.

- `SavingsAccountController` (`/api/savings-accounts`)
  - Create (POST `/api/savings-accounts`): added currencies table, interest types table, and an example request body in XML remarks.

Notes:
- These updates only add XML comments above existing controller actions; they do not change API signatures or runtime behavior.
- To surface these remarks in Swagger UI you must enable XML documentation generation for the Presentation project and configure Swagger to include the generated XML file. See the "Swagger / OpenAPI" section later in this document for instructions (or ask me to apply the project changes and wiring automatically).

- Ordering / sorting documentation:
  - Many controller GET endpoints that return collections were updated with XML comments documenting the ordering parameters (`orderBy` and `orderDirection`), common allowed fields, and ASC/DESC behavior. Affected controllers include `BankController`, `UserController`, `CheckingAccountController`, `SavingsAccountController`, `TransactionsController`, and related endpoints. These are documentation-only changes; consider adding server-side validation of `orderBy` values to avoid runtime errors for invalid property names.


This section details the public API surface of the application, controller by controller. All endpoints are secured by default and require authentication unless marked otherwise.

### **AuthController** (`/api/auth`)
Handles authentication and token management.

| Method | Route                  | Permission Required  | Description                                      |
|--------|------------------------|----------------------|--------------------------------------------------|
| POST   | `/login`               | None                 | Authenticates a user and returns JWT tokens.    |
| POST   | `/refresh-token`       | None                 | Obtains a new JWT access token using refresh token.|
| POST   | `/logout`              | Authenticated        | Logs out the user by revoking tokens.           |
| POST   | `/revoke-token/{userId}` | Auth.RevokeToken   | Revokes all tokens for a specific user.         |

---

#### **Login**
- **Method**: `POST`
- **Route**: `/api/auth/login`
- **Description**: Authenticates a user and returns a JWT access token and a refresh token.
- **Auth**: Anonymous
- **Request**:
  - **Body**: `LoginReqDto`
    ```json
    {
      "email": "user@example.com",
      "password": "YourPassword"
    }
    ```
- **Responses**:
  - **`200 OK`**: Login was successful.
    ```json
    {
      "message": "Login successful.",
      "auth": { ... } // AuthResDto
    }
    ```
  - **`400 Bad Request`**: The request is missing required fields.
  - **`401 Unauthorized`**: Invalid credentials were provided.

---

#### **Refresh Token**
- **Method**: `POST`
- **Route**: `/api/auth/refresh-token`
- **Description**: Obtains a new JWT access token using the refresh token stored in an `HttpOnly` cookie.
- **Auth**: Anonymous
- **Request**: None (the refresh token is sent automatically by the browser in a cookie).
- **Responses**:
  - **`200 OK`**: A new set of tokens was issued.
    ```json
    {
      "message": "Token refreshed successfully.",
      "auth": { ... } // AuthResDto
    }
    ```
  - **`401 Unauthorized`**: The refresh token is invalid, expired, or has been revoked.

---

#### **Logout**
- **Method**: `POST`
- **Route**: `/api/auth/logout`
- **Description**: Logs out the currently authenticated user by revoking their refresh tokens and invalidating their security stamp.
- **Auth**: Authenticated
- **Request**: None.
- **Responses**:
  - **`200 OK`**: Logout was successful.
    ```json
    {
      "message": "User logged out successfully."
    }
    ```
  - **`401 Unauthorized`**: The user is not authenticated.

---

#### **Revoke Token**
- **Method**: `POST`
- **Route**: `/api/auth/revoke-token/{userId}`
- **Description**: Allows an authorized user (typically an admin) to revoke all refresh tokens for a specific user.
- **Auth**: Authenticated
- **Permissions**: `Auth.RevokeToken`
- **Request**:
  - `userId` (string, required): The ID of the user whose tokens will be revoked, passed in the URL path.
- **Responses**:
  - **`200 OK`**: Tokens were revoked successfully.
    ```json
    {
      "message": "Tokens revoked successfully for user."
    }
    ```
  - **`400 Bad Request`**: The `userId` was not provided.
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.
  - **`404 Not Found`**: The specified user does not exist.

---

### **UserController** (`/api/users`)
Manages the user lifecycle. All endpoints are subject to the `RoleHierarchyFilter`.

| Method | Route                  | Permission Required  | Description                                      |
|--------|------------------------|----------------------|--------------------------------------------------|
| GET    | `/`                    | User.ReadAll         | Retrieves a paginated list of all users.         |
| GET    | `/by-username/{username}` | User.ReadByUsername | Retrieves a user by username.                   |
| GET    | `/{userId}`            | User.ReadById        | Retrieves a user by ID.                          |
| GET    | `/me`                  | User.ReadSelf        | Retrieves current user's info.                   |
| POST   | `/`                    | User.Create          | Creates a new user.                              |
| PUT    | `/{userId}`            | User.Update          | Updates a user's profile.                        |
| PUT    | `/{userId}/password`   | User.ChangePassword  | Changes a user's password.                       |
| PUT    | `/{userId}/active`     | User.Update          | Sets user's active status.                       |
| DELETE | `/{userId}`            | User.Delete          | Deletes a user.                                  |
| DELETE | `/bulk`                | User.DeleteRange     | Deletes multiple users.                          |

---

#### **Get All Users**
- **Method**: `GET`
- **Route**: `/api/users`
- **Description**: Retrieves a paginated list of all users.
- **Permissions**: `User.ReadAll`
- **Request**:
  - **Query Parameters**:
    - `pageNumber` (integer, optional, default: 1): The page number to retrieve.
    - `pageSize` (integer, optional, default: 10): The number of users per page.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    [ ... ] // Paginated list of UserDto
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: No users were found.

---

#### **Get User by Username**
- **Method**: `GET`
- **Route**: `/api/users/by-username/{username}`
- **Description**: Retrieves a single user by their username.
- **Permissions**: `User.ReadByUsername`
- **Request**:
  - `username` (string, required): The username of the user, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    { ... } // UserDto
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified user does not exist.

---

#### **Get User by ID**
- **Method**: `GET`
- **Route**: `/api/users/{userId}`
- **Description**: Retrieves a single user by their unique ID.
- **Permissions**: `User.ReadById`
- **Request**:
  - `userId` (string, required): The ID of the user, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    { ... } // UserDto
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified user does not exist.

---

#### **Get Current User Info**
- **Method**: `GET`
- **Route**: `/api/users/me`
- **Description**: Retrieves the information of the currently authenticated user.
- **Permissions**: `User.ReadSelf`
- **Request**: None.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    { ... } // UserDto
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The user's record could not be found.

---

#### **Create User**
- **Method**: `POST`
- **Route**: `/api/users`
- **Description**: Creates a new user.
- **Permissions**: `User.Create`
- **Request**:
  - **Body**: `UserReqDto`
    ```json
    {
      "email": "newuser@example.com",
      "username": "newuser",
      "password": "Password123!",
      ...
    }
    ```
- **Responses**:
  - **`201 Created`**: The user was created successfully.
    ```json
    { ... } // The newly created UserDto
    ```
  - **`400 Bad Request`**: The request body is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Update User**
- **Method**: `PUT`
- **Route**: `/api/users/{userId}`
- **Description**: Updates an existing user's profile information.
- **Permissions**: `User.Update`
- **Request**:
  - `userId` (string, required): The ID of the user to update, passed in the URL path.
  - **Body**: `UserEditDto`
    ```json
    {
      "fullName": "New Full Name",
      ...
    }
    ```
- **Responses**:
  - **`200 OK`**: The user was updated successfully.
    ```json
    { ... } // The updated UserDto
    ```
  - **`400 Bad Request`**: The request body is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Change User Password**
- **Method**: `PUT`
- **Route**: `/api/users/{userId}/password`
- **Description**: Changes a user's password.
- **Permissions**: `User.ChangePassword`
- **Request**:
  - `userId` (string, required): The ID of the user, passed in the URL path.
  - **Body**: `ChangePasswordReqDto`
    ```json
    {
      "currentPassword": "OldPassword123!",
      "newPassword": "NewPassword123!",
      "confirmNewPassword": "NewPassword123!"
    }
    ```
- **Responses**:
  - **`200 OK`**: The password was changed successfully.
  - **`400 Bad Request`**: The request is invalid (e.g., new password doesn't meet complexity requirements).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Set User Active Status**
- **Method**: `PUT`
- **Route**: `/api/users/{userId}/active?isActive={isActive}`
- **Description**: Sets a user's account to active or inactive.
- **Permissions**: `User.Update`
- **Request**:
  - `userId` (string, required): The ID of the user, passed in the URL path.
  - `isActive` (boolean, required): The desired status, passed as a query parameter.
- **Responses**:
  - **`200 OK`**: The status was updated successfully.
    ```json
    {
      "message": "User active status changed to true."
    }
    ```
  - **`400 Bad Request`**: Invalid request.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Delete User**
- **Method**: `DELETE`
- **Route**: `/api/users/{userId}`
- **Description**: Deletes a single user.
- **Permissions**: `User.Delete`
- **Request**:
  - `userId` (string, required): The ID of the user to delete, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The user was deleted successfully.
    ```json
    {
      "success": true,
      "message": "User deleted successfully."
    }
    ```
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Delete Multiple Users**
- **Method**: `DELETE`
- **Route**: `/api/users/bulk`
- **Description**: Deletes a list of users in a single operation.
- **Permissions**: `User.DeleteRange`
- **Request**:
  - **Body**: A JSON array of user IDs.
    ```json
    [ "user-guid-1", "user-guid-2" ]
    ```
- **Responses**:
  - **`200 OK`**: The users were deleted successfully.
    ```json
    {
      "success": true,
      "message": "Users deleted successfully."
    }
    ```
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

### **AccountController** (`/api/accounts`)
Provides various ways to read and delete accounts. All endpoints are subject to the `RoleHierarchyFilter`.

| Method | Route                      | Permission Required          | Description                                      |
|--------|----------------------------|------------------------------|--------------------------------------------------|
| GET    | `/{id}`                    | None                         | Retrieves account details by ID.                 |
| GET    | `/by-number/{accountNumber}` | Account.ReadByAccountNumber | Retrieves account by account number.             |
| GET    | `/by-user/{userId}`        | Account.ReadByUserId        | Retrieves accounts by user ID.                   |
| GET    | `/by-national-id/{nationalId}` | Account.ReadByNationalId | Retrieves accounts by national ID.               |
| PUT    | `/{id}/active`             | Account.Update               | Sets account active status.                      |
| DELETE | `/{id}`                    | Account.Delete               | Deletes an account.                              |
| DELETE | `/bulk`                    | Account.DeleteMany           | Deletes multiple accounts.                       |

---

#### **Get Account by ID**
- **Method**: `GET`
- **Route**: `/api/accounts/{id}`
- **Description**: Retrieves the details of a specific account by its unique ID.
- **Permissions**: No specific permission is required, but the `RoleHierarchyFilter` ensures that a user can only access accounts they are authorized to see based on their role.
- **Request**:
  - `id` (integer, required): The ID of the account to retrieve, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Account retrieved successfully.",
      "account": {
        "id": 1,
        "accountNumber": "ACC123456",
        "balance": 1000.00,
        "isActive": true,
        "userId": "user-guid-1",
        "currencyId": 1
      }
    }
    ```
  - **`404 Not Found`**: The account with the specified ID was not found.
    ```json
    {
      "message": "Account not found.",
      "account": null
    }
    ```
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user is not authorized to view this account.

---

#### **Get Account by Account Number**
- **Method**: `GET`
- **Route**: `/api/accounts/by-number/{accountNumber}`
- **Description**: Retrieves account details using the public account number.
- **Permissions**: `Account.ReadByAccountNumber`
- **Request**:
  - `accountNumber` (string, required): The account number, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Account retrieved successfully.",
      "account": { ... }
    }
    ```
  - **`404 Not Found`**: The account was not found.
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.

---

#### **Get Accounts by User ID**
- **Method**: `GET`
- **Route**: `/api/accounts/by-user/{userId}`
-- **Description**: Retrieves a list of all accounts belonging to a specific user.
- **Permissions**: `Account.ReadByUserId`
- **Request**:
  - `userId` (string, required): The ID of the user, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Accounts retrieved successfully.",
      "accounts": [
        { "id": 1, "accountNumber": "ACC123456", ... },
        { "id": 2, "accountNumber": "ACC654321", ... }
      ]
    }
    ```
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.

---

#### **Get Accounts by National ID**
- **Method**: `GET`
- **Route**: `/api/accounts/by-national-id/{nationalId}`
- **Description**: Retrieves all accounts associated with a user's National ID.
- **Permissions**: `Account.ReadByNationalId`
- **Request**:
  - `nationalId` (string, required): The user's National ID, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Accounts retrieved successfully.",
      "accounts": [ ... ]
    }
    ```
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.

---

#### **Delete Account**
- **Method**: `DELETE`
- **Route**: `/api/accounts/{id}`
- **Description**: Deletes a single account by its ID. An account cannot be deleted if it has a positive balance.
- **Permissions**: `Account.Delete`
- **Request**:
  - `id` (integer, required): The ID of the account to delete, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The account was deleted successfully.
    ```json
    {
      "message": "Account deleted successfully."
    }
    ```
  - **`400 Bad Request`**: The request was invalid (e.g., trying to delete an account with a balance).
  - **`404 Not Found`**: The account was not found.
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.

---

#### **Delete Multiple Accounts**
- **Method**: `DELETE`
- **Route**: `/api/accounts/bulk`
- **Description**: Deletes a list of accounts in a single operation.
- **Permissions**: `Account.DeleteMany`
- **Request**:
  - **Body**: A JSON array of account IDs.
    ```json
    [ 1, 2, 3 ]
    ```
- **Responses**:
  - **`200 OK`**: The accounts were deleted successfully.
    ```json
    {
      "message": "Accounts deleted successfully."
    }
    ```
  - **`400 Bad Request`**: The request was invalid.
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.

---

#### **Set Account Active Status**
- **Method**: `PUT`
- **Route**: `/api/accounts/{id}/active?isActive={isActive}`
- **Description**: Sets an account's status to active or inactive. Inactive accounts cannot be used for transactions.
- **Permissions**: `Account.Update`
- **Request**:
  - `id` (integer, required): The ID of the account, passed in the URL path.
  - `isActive` (boolean, required): The desired status, passed as a query parameter.
- **Responses**:
  - **`200 OK`**: The status was updated successfully.
    ```json
    {
      "message": "Account active status changed to true."
    }
    ```
  - **`400 Bad Request`**: The request was invalid.
  - **`401 Unauthorized`**: The user is not authenticated.
  - **`403 Forbidden`**: The user lacks the required permission.

---

### **AccountTransactionsController** (`/api/accounts`)
Handles core financial operations. All endpoints are subject to the `RoleHierarchyFilter`.

| Method | Route          | Permission Required  | Description                                      |
|--------|----------------|----------------------|--------------------------------------------------|
| POST   | `/deposit`     | Transaction.Deposit  | Deposits money into an account.                  |
| POST   | `/withdraw`    | Transaction.Withdraw | Withdraws money from an account.                 |
| POST   | `/transfer`    | Transaction.Transfer | Transfers money between accounts.                |
| GET    | `/{id}/balance`| Transaction.ReadBalance | Retrieves account balance.                      |

---

#### **Deposit**
- **Method**: `POST`
- **Route**: `/api/accounts/deposit`
- **Description**: Deposits money into a specified account.
- **Permissions**: `Transaction.Deposit`
- **Request**:
  - **Body**: `DepositReqDto`
    ```json
    {
      "accountId": 1,
      "amount": 100.50
    }
    ```
- **Responses**:
  - **`200 OK`**: Deposit was successful.
    ```json
    {
      "message": "Deposit completed successfully.",
      "transaction": { ... } // TransactionResDto
    }
    ```
  - **`400 Bad Request`**: Invalid request (e.g., negative amount).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified account does not exist.

---

#### **Withdraw**
- **Method**: `POST`
- **Route**: `/api/accounts/withdraw`
- **Description**: Withdraws money from a specified account. Checks for sufficient funds and overdraft limits.
- **Permissions**: `Transaction.Withdraw`
- **Request**:
  - **Body**: `WithdrawReqDto`
    ```json
    {
      "accountId": 1,
      "amount": 50.00
    }
    ```
- **Responses**:
  - **`200 OK`**: Withdrawal was successful.
    ```json
    {
      "message": "Withdrawal completed successfully.",
      "transaction": { ... } // TransactionResDto
    }
    ```
  - **`400 Bad Request`**: Invalid request or insufficient funds.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified account does not exist.

---

#### **Transfer**
- **Method**: `POST`
- **Route**: `/api/accounts/transfer`
- **Description**: Transfers money from a source account to a target account. Handles currency conversion and fees.
- **Permissions**: `Transaction.Transfer`
- **Request**:
  - **Body**: `TransferReqDto`
    ```json
    {
      "sourceAccountId": 1,
      "targetAccountId": 2,
      "amount": 75.00
    }
    ```
- **Responses**:
  - **`200 OK`**: Transfer was successful.
    ```json
    {
      "message": "Transfer completed successfully.",
      "transaction": { ... } // TransactionResDto
    }
    ```
  - **`400 Bad Request`**: Invalid request (e.g., transferring to the same account).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: Either the source or target account does not exist.
  - **`409 Conflict`**: A concurrency issue occurred or the source account has insufficient funds.

---

#### **Get Account Balance**
- **Method**: `GET`
- **Route**: `/api/accounts/{id}/balance`
- **Description**: Retrieves the current balance for a specific account.
- **Permissions**: `Transaction.ReadBalance`
- **Request**:
  - `id` (integer, required): The ID of the account, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Balance retrieved successfully.",
      "balance": 1234.56
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified account does not exist.

---

### **CheckingAccountController** (`/api/checking-accounts`)
Manages the lifecycle of checking accounts. All endpoints are subject to the `RoleHierarchyFilter`.

| Method | Route          | Permission Required      | Description                                      |
|--------|----------------|--------------------------|--------------------------------------------------|
| GET    | `/`            | CheckingAccount.ReadAll  | Retrieves all checking accounts.                 |
| POST   | `/`            | CheckingAccount.Create   | Creates a new checking account.                  |
| PUT    | `/{id}`        | CheckingAccount.Update   | Updates a checking account.                      |
| PUT    | `/{id}/active` | CheckingAccount.Update   | Sets checking account active status.             |

---

#### **Get All Checking Accounts**

**Method**: `GET`
**Route**: `/api/checking-accounts`
**Description**: Retrieves a paginated list of all checking accounts.
**Permissions**: `CheckingAccount.ReadAll`
**Request**:
  - **Query Parameters**:
    - `pageNumber` (integer, optional, default: 1): The page number to retrieve.
    - `pageSize` (integer, optional, default: 10): The number of accounts per page.
**Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Checking accounts retrieved successfully.",
      "accounts": [ ... ] // Paginated list of CheckingAccountDto
    }
    ```
  - **`400 Bad Request`**: Invalid pagination parameters.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Create Checking Account**
- **Method**: `POST`
- **Route**: `/api/checking-accounts`
- **Description**: Creates a new checking account for a specified user.
- **Permissions**: `CheckingAccount.Create`
- **Request**:
  - **Body**: `CheckingAccountReqDto`
    ```json
    {
      "userId": "user-guid-1",
      "currencyId": 1,
      "initialBalance": 500.00,
      "overdraftLimit": 100.00
    }
    ```
- **Responses**:
  - **`201 Created`**: The account was created successfully.
    ```json
    {
      "message": "Checking account created successfully.",
      "account": { ... } // CheckingAccountDto
    }
    ```
  - **`400 Bad Request`**: Invalid request body.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`409 Conflict`**: A conflict occurred (e.g., user not found).

---

#### **Update Checking Account**
- **Method**: `PUT`
- **Route**: `/api/checking-accounts/{id}`
- **Description**: Updates the details of an existing checking account.
- **Permissions**: `CheckingAccount.Update`
- **Request**:
  - `id` (integer, required): The ID of the account to update, passed in the URL path.
  - **Body**: `CheckingAccountEditDto`
    ```json
    {
      "overdraftLimit": 200.00
    }
    ```
- **Responses**:
  - **`200 OK`**: The account was updated successfully.
    ```json
    {
      "message": "Checking account updated successfully.",
      "account": { ... } // CheckingAccountDto
    }
    ```
  - **`400 Bad Request`**: Invalid request body.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified account does not exist.

---

#### **Set Checking Account Active Status**
- **Method**: `PUT`
- **Route**: `/api/checking-accounts/{id}/active?isActive={isActive}`
- **Description**: Sets a checking account's status to active or inactive.
- **Permissions**: `CheckingAccount.Update`
- **Request**:
  - `id` (integer, required): The ID of the account, passed in the URL path.
  - `isActive` (boolean, required): The desired status, passed as a query parameter.
- **Responses**:
  - **`200 OK`**: The status was updated successfully.
    ```json
    {
      "message": "Checking account active status changed to true."
    }
    ```
  - **`400 Bad Request`**: Invalid request.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

### **SavingsAccountController** (`/api/savings-accounts`)
Manages the lifecycle of savings accounts. All endpoints are subject to the `RoleHierarchyFilter`.

| Method | Route          | Permission Required     | Description                                      |
|--------|----------------|-------------------------|--------------------------------------------------|
| GET    | `/`            | SavingsAccount.ReadAll  | Retrieves all savings accounts.                  |
| POST   | `/`            | SavingsAccount.Create   | Creates a new savings account.                   |
| PUT    | `/{id}`        | SavingsAccount.Update   | Updates a savings account.                       |
| PUT    | `/{id}/active` | SavingsAccount.Update   | Sets savings account active status.              |
| GET    | `/{id}/interest-logs` | SavingsAccount.ReadInterestLogs | Retrieves all interest logs for a savings account. |

---

#### **Get All Savings Accounts**
- **Method**: `GET`
- **Route**: `/api/savings-accounts`
- **Description**: Retrieves a paginated list of all savings accounts.
- **Permissions**: `SavingsAccount.ReadAll`
- **Request**:
  - **Query Parameters**:
    - `pageNumber` (integer, optional, default: 1): The page number to retrieve.
    - `pageSize` (integer, optional, default: 10): The number of accounts per page.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Savings accounts retrieved successfully.",
      "accounts": [ ... ] // Paginated list of SavingsAccountDto
    }
    ```
  - **`400 Bad Request`**: Invalid pagination parameters.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Create Savings Account**
- **Method**: `POST`
- **Route**: `/api/savings-accounts`
- **Description**: Creates a new savings account for a specified user.
- **Permissions**: `SavingsAccount.Create`
- **Request**:
  - **Body**: `SavingsAccountReqDto`
    ```json
    {
      "userId": "user-guid-1",
      "currencyId": 1,
      "initialBalance": 1000.00,
      "interestRate": 1.5,
      "interestType": "Monthly"
    }
    ```
- **Responses**:
  - **`201 Created`**: The account was created successfully.
    ```json
    {
      "message": "Savings account created successfully.",
      "account": { ... } // SavingsAccountDto
    }
    ```
  - **`400 Bad Request`**: Invalid request body.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`409 Conflict`**: A conflict occurred (e.g., user not found).

---

#### **Update Savings Account**
- **Method**: `PUT`
- **Route**: `/api/savings-accounts/{id}`
- **Description**: Updates the details of an existing savings account.
- **Permissions**: `SavingsAccount.Update`
- **Request**:
  - `id` (integer, required): The ID of the account to update, passed in the URL path.
  - **Body**: `SavingsAccountEditDto`
    ```json
    {
      "interestRate": 2.0,
      "interestType": "Quarterly"
    }
    ```
- **Responses**:
  - **`200 OK`**: The account was updated successfully.
    ```json
    {
      "message": "Savings account updated successfully.",
      "account": { ... } // SavingsAccountDto
    }
    ```
  - **`400 Bad Request`**: Invalid request body.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified account does not exist.

---

#### **Set Savings Account Active Status**
- **Method**: `PUT`
- **Route**: `/api/savings-accounts/{id}/active?isActive={isActive}`
- **Description**: Sets a savings account's status to active or inactive.
- **Permissions**: `SavingsAccount.Update`
- **Request**:
  - `id` (integer, required): The ID of the account, passed in the URL path.
  - `isActive` (boolean, required): The desired status, passed as a query parameter.
- **Responses**:
  - **`200 OK`**: The status was updated successfully.
    ```json
    {
      "message": "Savings account active status changed to true."
    }
    ```
  - **`400 Bad Request`**: Invalid request.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

### **TransactionsController** (`/api/transactions`)
Provides endpoints for reading transaction history. All endpoints are subject to the `RoleHierarchyFilter`.

| Method | Route                  | Permission Required        | Description                                      |
|--------|------------------------|----------------------------|--------------------------------------------------|
| GET    | `/{accountId}/history` | Transaction.ReadById       | Retrieves transaction history for an account.    |
| GET    | `/{transactionId}`     | Transaction.ReadById       | Retrieves a transaction by ID.                   |
| GET    | `/`                    | Transaction.ReadAllHistory | Retrieves all transactions.                      |

---

#### **Get Transaction History for an Account**
- **Method**: `GET`
- **Route**: `/api/transactions/{accountId}/history`
- **Description**: Retrieves a paginated history of all transactions for a specific account.
- **Permissions**: `Transaction.ReadById`
- **Request**:
  - `accountId` (integer, required): The ID of the account, passed in the URL path.
  - **Query Parameters**:
    - `pageNumber` (integer, optional, default: 1): The page number to retrieve.
    - `pageSize` (integer, optional, default: 20): The number of transactions per page.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Transaction history retrieved successfully.",
      "history": [ ... ] // Paginated list of TransactionResDto
    }
    ```
  - **`400 Bad Request`**: Invalid pagination parameters.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified account does not exist.

---

#### **Get Transaction by ID**
- **Method**: `GET`
- **Route**: `/api/transactions/{transactionId}`
- **Description**: Retrieves the full details of a single transaction by its ID.
- **Permissions**: `Transaction.ReadById`
- **Request**:
  - `transactionId` (integer, required): The ID of the transaction, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Transaction retrieved successfully.",
      "transaction": { ... } // TransactionResDto
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified transaction does not exist.

---

#### **Get All Transactions**
- **Method**: `GET`
- **Route**: `/api/transactions`
- **Description**: Retrieves a paginated list of all transactions across the system.
- **Permissions**: `Transaction.ReadAllHistory`
- **Request**:
  - **Query Parameters**:
    - `pageNumber` (integer, optional, default: 1): The page number to retrieve.
    - `pageSize` (integer, optional, default: 10): The number of transactions per page.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Transactions retrieved successfully.",
      "transactions": [ ... ] // Paginated list of TransactionResDto
    }
    ```
  - **`400 Bad Request`**: Invalid pagination parameters.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

### **RoleController** (`/api/roles`)
Manages user roles.

| Method | Route                  | Permission Required | Description                                      |
|--------|------------------------|---------------------|--------------------------------------------------|
| GET    | `/GetAllRoles`         | Role.ReadAll        | Retrieves all roles.                             |
| POST   | `/CreateRole`          | Role.Create         | Creates a new role.                              |
| DELETE | `/DeleteRole/{roleId}` | Role.Delete         | Deletes a role.                                  |

---

#### **Get All Roles**
- **Method**: `GET`
- **Route**: `/api/roles/GetAllRoles`
- **Description**: Retrieves a list of all available roles in the system.
- **Permissions**: `Role.ReadAll`
- **Request**: None.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Roles retrieved successfully.",
      "roles": [ ... ] // List of RoleResDto
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: No roles were found.

---

#### **Create Role**
- **Method**: `POST`
- **Route**: `/api/roles/CreateRole`
- **Description**: Creates a new role.
- **Permissions**: `Role.Create`
- **Request**:
  - **Body**: `RoleReqDto`
    ```json
    {
      "name": "NewRoleName"
    }
    ```
- **Responses**:
  - **`200 OK`**: The role was created successfully.
    ```json
    {
      "message": "Role created successfully.",
      "role": { ... } // RoleResDto
    }
    ```
  - **`400 Bad Request`**: The role name is invalid or already exists.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Delete Role**
- **Method**: `DELETE`
- **Route**: `/api/roles/DeleteRole/{roleId}`
- **Description**: Deletes a role by its ID.
- **Permissions**: `Role.Delete`
- **Request**:
  - `roleId` (string, required): The ID of the role to delete, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The role was deleted successfully.
    ```json
    {
      "message": "Role deleted successfully.",
      "role": { ... } // The deleted RoleResDto
    }
    ```
  - **`400 Bad Request`**: The request is invalid (e.g., trying to delete a role that is in use).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified role does not exist.

---

### **RoleClaimsController** (`/api/roleclaims`)
Manages the assignment of permissions to roles.

| Method | Route           | Permission Required | Description                                      |
|--------|-----------------|---------------------|--------------------------------------------------|
| GET    | `/GetAllClaims` | RoleClaims.ReadAll  | Retrieves all available permissions.             |
| POST   | `/Assign`       | RoleClaims.Assign   | Assigns permissions to a role.                   |

---

#### **Get All Claims**
- **Method**: `GET`
- **Route**: `/api/roleclaims/GetAllClaims`
- **Description**: Retrieves a list of all available permissions, grouped by their functional category (e.g., "Users", "Accounts").
- **Permissions**: `RoleClaims.ReadAll`
- **Request**: None.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "Users": [
        "Users.Create",
        "Users.Read",
        ...
      ],
      "Accounts": [
        "Accounts.Create",
        ...
      ]
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Assign Claims to Role**
- **Method**: `POST`
- **Route**: `/api/roleclaims/Assign`
- **Description**: Assigns a set of permissions (claims) to a specific role. This will overwrite any existing permissions for that role.
- **Permissions**: `RoleClaims.Assign`
- **Request**:
  - **Body**: `UpdateRoleClaimsDto`
    ```json
    {
      "roleName": "Teller",
      "claims": [
        "Accounts.Read",
        "Transactions.Deposit",
        "Transactions.Withdraw"
      ]
    }
    ```
- **Responses**:
  - **`200 OK`**: The claims were assigned successfully.
    ```json
    { ... } // The updated RoleClaimsDto
    ```
  - **`400 Bad Request`**: The request is invalid (e.g., role does not exist, or a claim is invalid).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

### **UserRolesController** (`/api/userroles`)
Manages the assignment of roles to users.

| Method | Route    | Permission Required | Description                                      |
|--------|----------|---------------------|--------------------------------------------------|
| POST   | `/Assign`| UserRoles.Assign    | Assigns a role to a user.                        |

---

#### **Assign Role to User**
- **Method**: `POST`
- **Route**: `/api/userroles/Assign`
- **Description**: Assigns a role to a specific user.
- **Permissions**: `UserRoles.Assign`
- **Request**:
  - **Body**: `UpdateUserRolesDto`
    ```json
    {
      "userId": "user-guid-1",
      "role": "Teller"
    }
    ```
- **Responses**:
  - **`200 OK`**: The role was assigned successfully.
    ```json
    { ... } // The updated UserRoleDto
    ```
  - **`400 Bad Request`**: The request is invalid (e.g., user or role does not exist).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

### **RoleHierarchyController** (`/api/rolehierarchy`)
Manages the relationships between roles. **Restricted to SuperAdmin only.**

| Method | Route                          | Permission Required | Description                                      |
|--------|--------------------------------|--------------------|--------------------------------------------------|
| GET    | `/hierarchy`                   | SuperAdmin         | Retrieves the entire role hierarchy.             |
| GET    | `/{roleName}/parents`          | SuperAdmin         | Retrieves direct parent roles.                   |
| GET    | `/{roleName}/ancestors`        | SuperAdmin         | Retrieves all ancestor roles.                    |
| GET    | `/{roleName}/children`         | SuperAdmin         | Retrieves direct child roles.                    |
| GET    | `/{roleName}/descendants`      | SuperAdmin         | Retrieves all descendant roles.                  |
| GET    | `/can-manage`                  | SuperAdmin         | Checks if a role can manage another.            |
| POST   | `/add-parent`                  | SuperAdmin         | Adds a parent-child relationship.                |
| POST   | `/add-parents`                 | SuperAdmin         | Adds multiple parent relationships.              |
| DELETE | `/remove-parent`               | SuperAdmin         | Removes a parent-child relationship.            |
| DELETE | `/remove-parents/{childRole}`  | SuperAdmin         | Removes multiple parent relationships.           |

---

#### **Get Role Hierarchy**
- **Method**: `GET`
- **Route**: `/api/rolehierarchy/hierarchy`
- **Description**: Retrieves the entire role hierarchy as a nested tree structure.
- **Auth**: SuperAdmin
- **Request**: None.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    [ ... ] // List of RoleHierarchyDto
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Get Direct Parents of a Role**
- **Method**: `GET`
- **Route**: `/api/rolehierarchy/{roleName}/parents`
- **Description**: Retrieves the direct parent roles of a specific role.
- **Auth**: SuperAdmin
- **Request**:
  - `roleName` (string, required): The name of the role, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    [ ... ] // List of parent role names
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Get All Ancestors of a Role**
- **Method**: `GET`
- **Route**: `/api/rolehierarchy/{roleName}/ancestors`
- **Description**: Recursively retrieves all ancestor roles for a given role.
- **Auth**: SuperAdmin
- **Request**:
  - `roleName` (string, required): The name of the role, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    [ ... ] // List of ancestor role names
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Get Direct Children of a Role**
- **Method**: `GET`
- **Route**: `/api/rolehierarchy/{roleName}/children`
- **Description**: Retrieves the direct child roles of a specific role.
- **Auth**: SuperAdmin
- **Request**:
  - `roleName` (string, required): The name of the role, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    [ ... ] // List of child role names
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Get All Descendants of a Role**
- **Method**: `GET`
- **Route**: `/api/rolehierarchy/{roleName}/descendants`
- **Description**: Recursively retrieves all descendant roles for a given role.
- **Auth**: SuperAdmin
- **Request**:
  - `roleName` (string, required): The name of the role, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    [ ... ] // List of descendant role names
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Check if a Role Can Manage Another**
- **Method**: `GET`
- **Route**: `/api/rolehierarchy/can-manage`
- **Description**: Determines if an `actingRole` has management authority over a `targetRole`.
- **Auth**: SuperAdmin
- **Request**:
  - **Query Parameters**:
    - `actingRole` (string, required): The role performing the action.
    - `targetRole` (string, required): The role being targeted.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "actingRole": "Manager",
      "targetRole": "Teller",
      "canManage": true
    }
    ```
  - **`400 Bad Request`**: Required parameters are missing.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Add Parent to Role**
- **Method**: `POST`
- **Route**: `/api/rolehierarchy/add-parent`
- **Description**: Establishes a parent-child relationship between two roles.
- **Auth**: SuperAdmin
- **Request**:
  - **Body**: `RoleParentReqDto`
    ```json
    {
      "childRole": "Teller",
      "parentRole": "Manager"
    }
    ```
- **Responses**:
  - **`200 OK`**: The relationship was created successfully.
  - **`400 Bad Request`**: The request is invalid (e.g., roles do not exist, creates a circular dependency).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Add Multiple Parents to Role**
- **Method**: `POST`
- **Route**: `/api/rolehierarchy/add-parents`
- **Description**: Atomically adds multiple parent roles to a single child role.
- **Auth**: SuperAdmin
- **Request**:
  - **Body**: `RoleParentsReqDto`
    ```json
    {
      "childRole": "Teller",
      "parentRoles": [ "Manager", "Auditor" ]
    }
    ```
- **Responses**:
  - **`200 OK`**: The relationships were created successfully.
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Remove Parent from Role**
- **Method**: `DELETE`
- **Route**: `/api/rolehierarchy/remove-parent`
- **Description**: Removes a parent-child relationship.
- **Auth**: SuperAdmin
- **Request**:
  - **Query Parameters**:
    - `childRole` (string, required): The child role.
    - `parentRole` (string, required): The parent role to remove.
- **Responses**:
  - **`200 OK`**: The relationship was removed successfully.
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

#### **Remove Multiple Parents from Role**
- **Method**: `DELETE`
- **Route**: `/api/rolehierarchy/remove-parents/{childRole}`
- **Description**: Atomically removes multiple parent roles from a child role.
- **Auth**: SuperAdmin
- **Request**:
  - `childRole` (string, required): The child role, passed in the URL path.
  - **Body**: A JSON array of parent role names.
    ```json
    [ "Manager", "Auditor" ]
    ```
- **Responses**:
  - **`200 OK`**: The relationships were removed successfully.
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User is not a SuperAdmin.

---

### **CurrencyController** (`/api/currency`)
Manages currencies and exchange rates.

| Method | Route          | Permission Required  | Description                                      |
|--------|----------------|----------------------|--------------------------------------------------|
| GET    | `/`            | `Currency.ReadAll`   | Gets a list of all currencies.                   |
| GET    | `/{id}`        | `Currency.ReadById`  | Gets a single currency by its ID.                |
| POST   | `/`            | `Currency.Create`    | Creates a new currency.                          |
| PUT    | `/{id}`        | `Currency.Update`    | Updates a currency.                              |
| PUT    | `/{id}/active` | `Currency.Update`    | Sets a currency to active or inactive.           |
| DELETE | `/{id}`        | `Currency.Delete`    | Deletes a currency.                              |

---

#### **Get All Currencies**
- **Method**: `GET`
- **Route**: `/api/currency`
- **Description**: Retrieves a list of all currencies.
- **Permissions**: `Currency.ReadAll`
- **Request**: None.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Currencies retrieved successfully.",
      "currencies": [ ... ] // List of CurrencyDto
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Get Currency by ID**
- **Method**: `GET`
- **Route**: `/api/currency/{id}`
- **Description**: Retrieves a single currency by its ID.
- **Permissions**: `Currency.ReadById`
- **Request**:
  - `id` (integer, required): The ID of the currency, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The request was successful.
    ```json
    {
      "message": "Currency retrieved successfully.",
      "currency": { ... } // CurrencyDto
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.
  - **`404 Not Found`**: The specified currency does not exist.

---

#### **Create Currency**
- **Method**: `POST`
- **Route**: `/api/currency`
- **Description**: Creates a new currency.
- **Permissions**: `Currency.Create`
- **Request**:
  - **Body**: `CurrencyReqDto`
    ```json
    {
      "code": "EGP",
      "exchangeRate": 1.0,
      "isBase": true
    }
    ```
- **Responses**:
  - **`201 Created`**: The currency was created successfully.
    ```json
    {
      "message": "Currency created successfully.",
      "currency": { ... } // The newly created CurrencyDto
    }
    ```
  - **`400 Bad Request`**: The request is invalid (e.g., code already exists).
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Update Currency**
- **Method**: `PUT`
- **Route**: `/api/currency/{id}`
- **Description**: Updates an existing currency.
- **Permissions**: `Currency.Update`
- **Request**:
  - `id` (integer, required): The ID of the currency to update, passed in the URL path.
  - **Body**: `CurrencyReqDto`
    ```json
    {
      "code": "EGP",
      "exchangeRate": 1.1,
      "isBase": true
    }
    ```
- **Responses**:
  - **`200 OK`**: The currency was updated successfully.
    ```json
    {
      "message": "Currency updated successfully.",
      "currency": { ... } // The updated CurrencyDto
    }
    ```
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Delete Currency**
- **Method**: `DELETE`
- **Route**: `/api/currency/{id}`
- **Description**: Deletes a currency.
- **Permissions**: `Currency.Delete`
- **Request**:
  - `id` (integer, required): The ID of the currency to delete, passed in the URL path.
- **Responses**:
  - **`200 OK`**: The currency was deleted successfully.
    ```json
    {
      "message": "Currency deleted successfully."
    }
    ```
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

#### **Set Currency Active Status**
- **Method**: `PUT`
- **Route**: `/api/currency/{id}/active?isActive={isActive}`
- **Description**: Sets a currency's status to active or inactive.
- **Permissions**: `Currency.Update`
- **Request**:
  - `id` (integer, required): The ID of the currency, passed in the URL path.
  - `isActive` (boolean, required): The desired status, passed as a query parameter.
- **Responses**:
  - **`200 OK`**: The status was updated successfully.
    ```json
    {
      "message": "Currency active status changed to true."
    }
    ```
  - **`400 Bad Request`**: The request is invalid.
  - **`401 Unauthorized`**: User is not authenticated.
  - **`403 Forbidden`**: User lacks the required permission.

---

## 7. DTOs & Data Contracts

Data Transfer Objects (DTOs) define the shape of the data that is sent to and from the API. They act as a clear contract for the API and include validation rules.

### **Authentication DTOs**

- **`LoginReqDto`** (Request)
  - `Email` (string, required, email format): The user's email address.
  - `Password` (string, required): The user's plain-text password.

- **`AuthResDto`** (Response)
  - `IsAuthenticated` (bool): True if login was successful.
  - `Username`, `Email`, `Roles`: Basic user information.
  - `Token` (string): The short-lived JWT access token.
  - `ExpiresOn` (DateTime): The expiration timestamp for the access token.
  - `RefreshTokenExpiration` (DateTime): The sliding expiration for the refresh token.

### **User & Role DTOs**

- **`UserReqDto`** (Request)
  - Used for creating a new user. Contains properties for `Email`, `Username`, `FullName`, `NationalId`, `PhoneNumber`, `DateOfBirth`, and `Password` with validation attributes like `[Required]`, `[StringLength]`, and `[Compare]`.

- **`UserEditDto`** (Request)
  - Similar to `UserReqDto` but used for updates and excludes password fields.

- **`ChangePasswordReqDto`** (Request)
  - `CurrentPassword` (string): The user's existing password.
  - `NewPassword` (string, required, min length 6): The desired new password.
  - `ConfirmNewPassword` (string, required, must match `NewPassword`): Password confirmation.

- **`UpdateUserRolesDto`** (Request)
  - `UserId` (string): The ID of the user to modify.
  - `Role` (string): The name of the role to assign.

- **`RoleReqDto`** (Request)
  - `Name` (string, required): The name of the role to create.

- **`UpdateRoleClaimsDto`** (Request)
  - `RoleName` (string): The name of the role to modify.
  - `Claims` (List<string>): A list of permission strings to assign to the role.

### **Account DTOs**

- **`CheckingAccountReqDto`** (Request)
  - `UserId` (string, required): The owner of the account.
  - `CurrencyId` (int, required): The currency for the account.
  - `InitialBalance` (decimal, non-negative): The starting balance.
  - `OverdraftLimit` (decimal, non-negative): The allowed overdraft amount.

- **`SavingsAccountReqDto`** (Request)
  - `UserId`, `CurrencyId`, `InitialBalance`: Same as above.
  - `InterestRate` (decimal, range 0-100): The percentage interest rate.
  - `InterestType` (enum): The frequency of interest calculation (e.g., `Monthly`, `Annually`).

### **Transaction DTOs**

- **`DepositReqDto`** / **`WithdrawReqDto`** (Request)
  - `AccountId` (int, required): The ID of the account to act upon.
  - `Amount` (decimal, required, positive): The amount for the transaction.

- **`TransferReqDto`** (Request)
  - `SourceAccountId` (int, required): The account to withdraw from.
  - `TargetAccountId` (int, required): The account to deposit into.
  - `Amount` (decimal, required, positive): The amount to transfer.

- **`TransactionResDto`** (Response)
  - A rich object providing full details of a completed transaction, including `TransactionId`, `SourceAccountId`, `TargetAccountId`, `Amount`, `Fees`, `TransactionType`, and `Timestamp`.

### **Currency DTOs**

- **`CurrencyReqDto`** (Request)
  - `Code` (string, required, length 3-5): The currency code (e.g., "USD").
  - `IsBase` (bool): Whether this is the base currency for conversions.
  - `ExchangeRate` (decimal, positive): The rate relative to the base currency.

### **Bank DTOs**

- **`BankReqDto`** (Request)
  - `Name` (string, required): The name of the bank.

- **`BankEditDto`** (Request)
  - `Name` (string, required): The new name of the bank.

---

## 8. Jobs & Background Processing

Background jobs are essential for performing tasks that are long-running, scheduled, or need to be decoupled from the main application request-response cycle. This application uses .NET's `BackgroundService` for robust, long-running job management.

### **AddInterestJob**: Automated Interest Accrual

This job is responsible for automatically calculating and applying interest to all eligible savings accounts.

- **Trigger**: The job runs on a periodic schedule once every 24 hours.
- **Logic**:
  1. **Scope Creation**: It creates a new dependency injection scope to ensure services like the `IUnitOfWork` are resolved correctly and disposed of properly for each run.
  2. **Fetch Accounts**: It retrieves all `SavingsAccount` entities from the database, including their `InterestLogs` for determining eligibility.
  3. **Batch Processing**: Accounts are processed in batches of 100 to optimize performance and memory usage.
  4. **Determine Eligibility**: For each account, it calls the `ShouldAddInterest` method. This method checks the account's `InterestType` and compares the current date to the timestamp of the last interest log:
     - `Monthly`: Interest is due if more than 1 month has passed since the last log.
     - `Quarterly`: Interest is due if more than 3 months have passed.
     - `Annually`: Interest is due if more than 1 year has passed.
  5. **Calculate & Apply Interest**:
     - It calculates the interest amount using: `balance * interestRate / 100`.
     - It adds the calculated amount to the account's balance.
     - It creates a new `InterestLog` record with the amount, timestamp, and account details.