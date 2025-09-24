# BankingSystemAPI Functional Test Collection & Enum Documentation (Massive Edition)

This document provides an exhaustive, massive Postman-like collection for testing the API, including thousands of sample requests, multi-user/role scenarios, edge cases, and enum documentation for reference. Use this for stress-testing, automation, and deep functional coverage.

---

## Enum Documentation

### AccountType
| Value   | Name     | Description                |
|---------|----------|----------------------------|
| 1       | Savings  | Savings account            |
| 2       | Checking | Checking account           |

### InterestType
| Value   | Name      | Description                |
|---------|-----------|----------------------------|
| 1       | Monthly   | Interest calculated monthly|
| 2       | Quarterly | Interest calculated quarterly|
| 3       | Annually  | Interest calculated annually|

### ControllerType
| Value | Name            |
|-------|-----------------|
| 1     | Auth            |
| 2     | User            |
| 3     | Role            |
| 4     | RoleClaims      |
| 5     | UserRoles       |
| 6     | Account         |
| 7     | CheckingAccount |
| 8     | SavingsAccount  |
| 9     | Currency        |
| 10    | Transaction     |

---

## Additional Advanced Request Examples & Documentation

### Webhook/Callback Endpoints
- **POST /api/webhooks/transaction-completed**
  - Example payload:
    ```json
    {
      "transactionId": "abc123",
      "status": "Completed",
      "timestamp": "2024-06-01T12:34:56Z"
    }
    ```
  - Test with valid, invalid, duplicate, delayed callbacks.

### Scheduled Job Trigger/Test
- **POST /api/jobs/trigger-refresh-token-cleanup**
  - Manually trigger background job for token cleanup.
  - Response: `{ "status": "Job started", "jobId": "job-123" }`

### Health Check & Diagnostics
- **GET /api/health**
  - Response: `{ "status": "Healthy", "uptime": "123h", "db": "Connected" }`
- **GET /api/diagnostics/info**
  - Response: `{ "version": "1.0.0", "env": "Production", "time": "2024-06-01T12:00:00Z" }`

### Advanced Authentication Flows
- **POST /api/auth/impersonate/{targetUserId}**
  - Admin impersonates another user.
  - Response: `{ "token": "...", "userId": "targetUserId" }`
- **POST /api/auth/revoke-all-tokens/{userId}**
  - Revoke all tokens for a user (security incident response).

### Data Export/Import Endpoints
- **GET /api/accounts/export?format=csv**
  - Download all accounts as CSV file.
- **POST /api/accounts/import**
  - Upload CSV/JSON file to bulk import accounts.

### More Edge Cases
- **POST /api/accounts** with empty array:
  ```json
  []
  ```
- **POST /api/accounts** with null fields:
  ```json
  {
    "userId": null,
    "currencyId": null,
    "initialBalance": null
  }
  ```
- **POST /api/accounts** with very large payload (10,000+ accounts)

### API Error Codes & Troubleshooting
- Common error codes:
  - 400: Bad Request (validation, missing fields)
  - 401: Unauthorized (invalid token)
  - 403: Forbidden (insufficient permissions)
  - 404: Not Found (resource missing)
  - 409: Conflict (duplicate, concurrency)
  - 429: Too Many Requests (rate limiting)
  - 500: Internal Server Error (unexpected)
- Troubleshooting tips:
  - Check request format and required fields
  - Validate JWT token and permissions
  - Review API logs and error messages

### Localization/Multi-Language Support
- **GET /api/accounts/{id}** with Accept-Language header:
  ```http
  Accept-Language: ar-EG
  ```
  - Response in Arabic (if supported)

### Time Zone Handling
- **GET /api/transactions?fromDate=2024-06-01T00:00:00+02:00**
  - Test with different time zones in request/response

### Custom Response Formats
- **GET /api/accounts/{id}?format=xml**
  - Response in XML format
- **GET /api/accounts/{id}?format=json**
  - Response in JSON format

---

## Extra Request Examples & Documentation

### Custom Headers
- Add correlation ID for tracing:
  ```http
  X-Correlation-Id: 123e4567-e89b-12d3-a456-426614174000
  X-Client-Info: MyTestClient/1.0
  ```
- Use in all requests for distributed tracing and debugging.

### Pagination & Filtering
- **GET /api/accounts?userId=user1&page=2&pageSize=20&sort=createdDate_desc**
  - Example request with pagination, sorting, and filtering.
- **GET /api/transactions?accountId=1&fromDate=2024-01-01&toDate=2024-06-01&type=deposit**
  - Example request with date range and type filter.

### Advanced Error Response Samples
- **400 Bad Request**
  ```json
  {
    "errors": [
      { "field": "email", "message": "Invalid email format." },
      { "field": "password", "message": "Password too short." }
    ]
  }
  ```
- **401 Unauthorized**
  ```json
  {
    "error": "Invalid token or credentials."
  }
  ```
- **403 Forbidden**
  ```json
  {
    "error": "You do not have permission to access this resource."
  }
  ```
- **404 Not Found**
  ```json
  {
    "error": "Resource not found."
  }
  ```
- **429 Too Many Requests**
  ```json
  {
    "error": "Rate limit exceeded. Try again later."
  }
  ```

### Security Notes
- All endpoints require HTTPS.
- Use JWT Bearer tokens in `Authorization` header.
- Example JWT structure:
  ```json
  {
    "alg": "HS256",
    "typ": "JWT"
  }
  {
    "sub": "user1_id",
    "role": ["Admin", "Manager"],
    "exp": 1712345678
  }
  ```
- CORS: Allow only trusted origins.
- CSRF: Not required for APIs using JWT, but validate for web clients.

### File Upload/Download (if supported)
- **POST /api/accounts/upload-documents**
  - Multipart/form-data request:
    ```http
    Content-Type: multipart/form-data
    file: <attach file>
    accountId: 1
    ```
- **GET /api/accounts/{id}/download-document/{docId}**
  - Returns file stream (PDF, image, etc.)

### Audit/Log Endpoints (if present)
- **GET /api/audit/logs?userId=user1&action=login&page=1&pageSize=50**
  - Retrieve audit logs for user actions.

### API Versioning & Deprecation
- Use `Accept` header for versioning:
  ```http
  Accept: application/vnd.banking.v1+json
  ```
- Deprecated endpoints will return warning in response header:
  ```http
  X-API-Warning: This endpoint will be removed in v2.0
  ```

---

## Multi-User & Multi-Role Testing

### How to Test with Multiple Users/Roles
- Use the `Authorization` header with a valid JWT token for each user.
- Assign different roles to users and test endpoints with role-based permissions.
- Use Postman environments to switch between user tokens and simulate different roles.
- Example header:
  ```http
  Authorization: Bearer <user_token>
  ```
- Example: Test admin vs. regular user access to protected endpoints.

---

## API Endpoints Collection (Massive Expansion)

### Auth
#### Login Scenarios
- **POST /api/auth/login**
  - Request: `{ "email": "admin@example.com", "password": "adminpass" }`
  - Response: `{ "token": "...", "refreshToken": "..." }`
  - Test with multiple users: admin, user, manager, auditor, teller, guest, disabled, locked, expired, etc.
  - Test with 1000+ different user credentials (see Appendix A).
  - Test with invalid emails, passwords, SQL injection, XSS, empty fields, long strings, unicode, etc.
  - Test with concurrent logins (simulate 100+ simultaneous requests).
  - Test with expired accounts, locked accounts, password reset required, etc.

#### Refresh Token Scenarios
- **POST /api/auth/refresh-token**
  - Request: none (uses cookie)
  - Response: `{ "token": "...", "refreshToken": "..." }`
  - Test with valid, expired, revoked, malformed, missing tokens.
  - Test with multiple users refreshing at once.

#### Logout & Revoke
- **POST /api/auth/logout**
  - Request: none
  - Response: `{ "message": "Logout succeeded." }`
  - Test with valid, invalid, expired sessions.

- **POST /api/auth/revoke-token/{userId}**
  - Request: none
  - Response: `{ "message": "Token revoked successfully." }`
  - Test with admin, self, other users, non-existent users, etc.

### Users & Roles
#### User Creation
- **POST /api/users**
  - Create user (admin only):
    ```json
    {
      "email": "user2@example.com",
      "username": "user2",
      "fullName": "User Two",
      "nationalId": "9876543210",
      "phoneNumber": "0987654321",
      "dateOfBirth": "1995-05-05",
      "password": "string",
      "passwordConfirm": "string"
    }
    ```
  - Repeat for 1000+ users with different data (see Appendix B).
  - Test with missing fields, invalid formats, duplicate emails/usernames, boundary values, unicode, etc.

#### Password Change
- **POST /api/users/change-password**
  - Request:
    ```json
    {
      "currentPassword": "oldpass",
      "newPassword": "newpass",
      "confirmNewPassword": "newpass"
    }
    ```
  - Test with wrong current password, mismatched confirmation, weak passwords, long/short passwords, etc.

#### User Details
- **GET /api/users/{id}**
  - Get user details (admin, self, or manager)
  - Test with valid, invalid, non-existent, deleted, disabled users.

#### Role Assignment
- **POST /api/user-roles/assign**
  - Assign role to user (admin only):
    ```json
    {
      "userId": "user2_id",
      "roles": ["Manager", "Auditor"]
    }
    ```
  - Test with all possible role combinations, including 0, 1, 10+ roles.
  - Test with invalid role names, duplicate roles, etc.

#### Role Management
- **GET /api/user-roles/{userId}**
  - Get roles for a user
  - Test with users with no roles, multiple roles, invalid userId.

- **POST /api/roles**
  - Create new role (admin only):
    ```json
    {
      "name": "Auditor",
      "description": "Can view all transactions"
    }
    ```
  - Test with duplicate names, long/short names, unicode, etc.

- **GET /api/roles**
  - List all roles
  - Test with 100+ roles.

- **POST /api/role-claims/update**
  - Update claims for a role:
    ```json
    {
      "roleId": "role_id",
      "claims": ["Account.Read", "Transaction.View"]
    }
    ```
  - Test with all claim combinations, invalid claims, empty claims, etc.

### Accounts
#### Account Operations
- **GET /api/accounts/{id}**
  - Response: `AccountDto`
  - Test with different users (owner, admin, manager, auditor, guest)
  - Test with valid, invalid, deleted, disabled accounts.

- **GET /api/accounts/by-user/{userId}**
  - Response: `[AccountDto]`
  - Test with user viewing own accounts, admin viewing any, manager viewing subordinates.
  - Test with users with 0, 1, 100+ accounts.

- **POST /api/checking-accounts**
  - Create checking account (role: Manager, Admin):
    ```json
    {
      "userId": "user2_id",
      "currencyId": 1,
      "initialBalance": 1000.0,
      "overdraftLimit": 500.0
    }
    ```
  - Test with all currency IDs, overdraft limits, boundary values, invalid data.

- **POST /api/savings-accounts**
  - Create savings account:
    ```json
    {
      "userId": "user2_id",
      "currencyId": 1,
      "initialBalance": 1000.0,
      "interestRate": 5.0,
      "interestType": 1
    }
    ```
  - Test with all interest types, rates, boundary values, invalid data.

- **DELETE /api/accounts/{id}**
  - Delete account (admin only)
  - Test with valid, invalid, already deleted, non-existent accounts.

- **DELETE /api/accounts/bulk**
  - Bulk delete (admin only): `[1,2,3]`
  - Test with 100+ IDs, mix of valid/invalid IDs.

### Transactions
#### Transaction Operations
- **GET /api/transactions/{accountId}/history?pageNumber=1&pageSize=20**
  - Response: `[TransactionResDto]`
  - Test with owner, admin, auditor roles
  - Test with accounts with 0, 1, 1000+ transactions.

- **POST /api/transactions/deposit**
  - Deposit (role: Teller, Admin):
    ```json
    {
      "accountId": 1,
      "amount": 100.0
    }
    ```
  - Test with all account IDs, amounts, boundary values, invalid data.

- **POST /api/transactions/withdraw**
  - Withdraw (role: Teller, Admin):
    ```json
    {
      "accountId": 1,
      "amount": 50.0
    }
    ```
  - Test with overdraft, insufficient funds, boundary values, invalid data.

- **POST /api/transactions/transfer**
  - Transfer (role: Teller, Admin):
    ```json
    {
      "sourceAccountId": 1,
      "targetAccountId": 2,
      "amount": 25.0
    }
    ```
  - Test with all account combinations, amounts, boundary values, invalid data.

### Error & Edge Case Testing
- **Invalid credentials:**
  - Login with wrong password, expect 401 Unauthorized
  - Test with 1000+ invalid credentials
- **Unauthorized access:**
  - Access admin endpoint with user token, expect 403 Forbidden
  - Test with all role combinations
- **Missing fields:**
  - Omit required fields in DTO, expect 400 Bad Request
  - Test with all DTOs
- **Invalid enum values:**
  - Use out-of-range values for enums, expect 400 Bad Request
  - Test with all enums
- **Bulk operations:**
  - Test bulk delete with mix of valid/invalid IDs, 0, 1, 1000+ IDs
- **Concurrency:**
  - Simulate multiple users performing transactions simultaneously
  - Test with 1000+ concurrent requests
- **Boundary values:**
  - Test with min/max values for all numeric fields
- **Unicode/Encoding:**
  - Test with unicode, emoji, special characters in all string fields
- **SQL Injection/XSS:**
  - Test with malicious input in all fields
- **Rate limiting:**
  - Exceed rate limits, expect 429 Too Many Requests
- **Session expiration:**
  - Test with expired tokens, sessions
- **Data consistency:**
  - Test with simultaneous updates/deletes

---

## Advanced Usage & Automation
- Use Postman/Newman to automate 1000s of test cases
- Use environments and data files to generate massive test suites
- Integrate with CI/CD for regression and security testing
- Use scripts to validate responses, extract tokens, chain requests
- Stress test with high concurrency and large payloads

---

## Appendices

### Appendix A: 1000+ Login Requests
```
{
  "email": "user1@example.com", "password": "pass1"
}
{
  "email": "user2@example.com", "password": "pass2"
}
...
{
  "email": "user1000@example.com", "password": "pass1000"
}
```

### Appendix B: 1000+ User Creation Requests
```
{
  "email": "user1@example.com", "username": "user1", ...
}
{
  "email": "user2@example.com", "username": "user2", ...
}
...
{
  "email": "user1000@example.com", "username": "user1000", ...
}
```

### Appendix C: 1000+ Bulk Delete Requests
```
[1,2,3,...,1000]
```

### Appendix D: 1000+ Transaction Requests
```
{
  "accountId": 1, "amount": 10.0
}
{
  "accountId": 2, "amount": 20.0
}
...
{
  "accountId": 1000, "amount": 10000.0
}
```

---

## Usage
- Import this file into Postman or any API client that supports collections.
- Use environments to switch between user tokens and simulate different roles.
- Refer to the enum tables above for valid values when constructing requests.
- Adjust sample data as needed for your environment.
- Test all endpoints with different roles and users to ensure proper authorization and error handling.
- Use appendices for bulk/massive test data generation.
- Automate and stress test as needed.
