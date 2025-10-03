# ?? Banking System API Reference

## ?? Base Information
- **Base URL**: `https://localhost:7071/api`
- **Authentication**: JWT Bearer Token
- **Content-Type**: `application/json`
- **API Version**: v1

---

## ?? Authentication Endpoints

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "superadmin@paysky.io",
  "password": "SuperAdmin@123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh_token_here",
    "expiresAt": "2024-10-30T12:00:00Z",
    "user": {
      "id": "user-id",
      "username": "admin",
      "fullName": "System Administrator",
      "email": "admin@bank.com"
    }
  }
}
```

### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "refresh_token_here"
}
```

### Logout
```http
POST /api/auth/logout
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "refreshToken": "refresh_token_here"
}
```

### Revoke Token
```http
POST /api/auth/revoke-token
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "token": "token_to_revoke"
}
```

---

## ?? User Management

### Get All Users
```http
GET /api/users?pageNumber=1&pageSize=10&searchTerm=john
Authorization: Bearer {access_token}
```

### Get User by ID
```http
GET /api/users/{userId}
Authorization: Bearer {access_token}
```

### Create User
```http
POST /api/users
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "username": "newuser",
  "email": "user@example.com",
  "password": "Password@123",
  "fullName": "John Doe",
  "nationalId": "1234567890",
  "dateOfBirth": "1990-01-01T00:00:00Z",
  "phoneNumber": "+1234567890",
  "bankId": 1
}
```

### Update User
```http
PUT /api/users/{userId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "fullName": "John Updated",
  "email": "updated@example.com",
  "phoneNumber": "+1234567891",
  "dateOfBirth": "1990-01-01T00:00:00Z"
}
```

### Delete User
```http
DELETE /api/users/{userId}
Authorization: Bearer {access_token}
```

### Change Password
```http
POST /api/users/{userId}/change-password
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "currentPassword": "OldPassword@123",
  "newPassword": "NewPassword@123"
}
```

---

## ?? Account Management

### Get Account by ID
```http
GET /api/accounts/{accountId}
Authorization: Bearer {access_token}
```

### Get Account by Account Number
```http
GET /api/accounts/number/{accountNumber}
Authorization: Bearer {access_token}
```

### Get Accounts by User ID
```http
GET /api/accounts/user/{userId}?pageNumber=1&pageSize=10
Authorization: Bearer {access_token}
```

### Get Accounts by National ID
```http
GET /api/accounts/national/{nationalId}
Authorization: Bearer {access_token}
```

### Delete Account
```http
DELETE /api/accounts/{accountId}
Authorization: Bearer {access_token}
```

### Set Account Active Status
```http
PATCH /api/accounts/{accountId}/active-status
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "isActive": true
}
```

---

## ?? Checking Accounts

### Get All Checking Accounts
```http
GET /api/checking-accounts?pageNumber=1&pageSize=10&searchTerm=account
Authorization: Bearer {access_token}
```

### Create Checking Account
```http
POST /api/checking-accounts
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "userId": "user-id-here",
  "initialBalance": 1000.00,
  "currencyId": 1,
  "overdraftLimit": 500.00,
  "overdraftFee": 25.00
}
```

### Update Checking Account
```http
PUT /api/checking-accounts/{accountId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "overdraftLimit": 750.00,
  "overdraftFee": 30.00
}
```

---

## ?? Savings Accounts

### Get All Savings Accounts
```http
GET /api/savings-accounts?pageNumber=1&pageSize=10
Authorization: Bearer {access_token}
```

### Create Savings Account
```http
POST /api/savings-accounts
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "userId": "user-id-here",
  "initialBalance": 2000.00,
  "currencyId": 1,
  "interestRate": 2.5
}
```

### Update Savings Account
```http
PUT /api/savings-accounts/{accountId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "interestRate": 3.0
}
```

### Get Interest Logs
```http
GET /api/savings-accounts/interest-logs?pageNumber=1&pageSize=10
Authorization: Bearer {access_token}
```

### Get Interest Logs by Account
```http
GET /api/savings-accounts/{accountId}/interest-logs?pageNumber=1&pageSize=10
Authorization: Bearer {access_token}
```

---

## ?? Transactions

### Get All Transactions
```http
GET /api/transactions?pageNumber=1&pageSize=10&searchTerm=deposit
Authorization: Bearer {access_token}
```

### Get Transaction by ID
```http
GET /api/transactions/{transactionId}
Authorization: Bearer {access_token}
```

### Get Transactions by Account
```http
GET /api/account-transactions/{accountId}?pageNumber=1&pageSize=10
Authorization: Bearer {access_token}
```

### Get Account Balance
```http
GET /api/transactions/balance/{accountId}
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accountId": 1,
    "balance": 1500.00,
    "currency": "USD",
    "accountNumber": "ACC001",
    "lastUpdated": "2024-10-30T10:30:00Z"
  }
}
```

### Deposit
```http
POST /api/transactions/deposit
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "accountId": 1,
  "amount": 500.00,
  "description": "Salary deposit"
}
```

### Withdraw
```http
POST /api/transactions/withdraw
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "accountId": 1,
  "amount": 200.00,
  "description": "ATM withdrawal"
}
```

### Transfer
```http
POST /api/transactions/transfer
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "sourceAccountId": 1,
  "targetAccountId": 2,
  "amount": 300.00,
  "description": "Payment to John"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "transactionId": 123,
    "sourceBalance": 1200.00,
    "targetBalance": 800.00,
    "transactionDate": "2024-10-30T10:30:00Z",
    "reference": "TXN123456"
  }
}
```

---

## ?? Bank Management

### Get All Banks
```http
GET /api/banks?pageNumber=1&pageSize=10&searchTerm=national
Authorization: Bearer {access_token}
```

### Get Bank by ID
```http
GET /api/banks/{bankId}
Authorization: Bearer {access_token}
```

### Get Bank by Name
```http
GET /api/banks/name/{bankName}
Authorization: Bearer {access_token}
```

### Create Bank
```http
POST /api/banks
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "New National Bank",
  "address": "123 Banking Street",
  "phoneNumber": "+1234567890",
  "email": "contact@newbank.com"
}
```

### Update Bank
```http
PUT /api/banks/{bankId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "Updated Bank Name",
  "address": "456 New Address",
  "phoneNumber": "+1234567891",
  "email": "newemail@bank.com"
}
```

### Delete Bank
```http
DELETE /api/banks/{bankId}
Authorization: Bearer {access_token}
```

---

## ?? Currency Management

### Get All Currencies
```http
GET /api/currency
Authorization: Bearer {access_token}
```

### Get Currency by ID
```http
GET /api/currency/{currencyId}
Authorization: Bearer {access_token}
```

### Create Currency
```http
POST /api/currency
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "code": "EUR",
  "name": "Euro",
  "symbol": "�"
}
```

### Update Currency
```http
PUT /api/currency/{currencyId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "Updated Euro",
  "symbol": "�"
}
```

### Delete Currency
```http
DELETE /api/currency/{currencyId}
Authorization: Bearer {access_token}
```

---

## ?? Role Management

### Get All Roles
```http
GET /api/roles
Authorization: Bearer {access_token}
```

### Create Role
```http
POST /api/roles
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "name": "CustomRole",
  "description": "Custom role description"
}
```

### Delete Role
```http
DELETE /api/roles/{roleId}
Authorization: Bearer {access_token}
```

### Update User Roles
```http
PUT /api/user-roles/{userId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "roleIds": ["role-id-1", "role-id-2"]
}
```

---

## ?? Role Claims Management

### Get All Claims by Group
```http
GET /api/role-claims?groupBy=module
Authorization: Bearer {access_token}
```

### Update Role Claims
```http
PUT /api/role-claims/{roleId}
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "selectedClaims": [
    "Permission.User.Create",
    "Permission.User.Update",
    "Permission.Account.ReadById"
  ]
}
```

---

## ?? Response Formats

### Success Response
```json
{
  "success": true,
  "data": {
    // Response data here
  }
}
```

### Error Response
```json
{
  "success": false,
  "errors": [
    "Error message 1",
    "Error message 2"
  ]
}
```

### Validation Error Response
```json
{
  "success": false,
  "errors": [
    "Validation failed"
  ],
  "details": {
    "amount": ["Amount must be greater than zero"],
    "accountId": ["Account ID is required"]
  }
}
```

### Paged Response
```json
{
  "success": true,
  "data": {
    "items": [
      // Array of items
    ],
    "totalCount": 100,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 10,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

## ?? HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Invalid request data |
| 401 | Unauthorized | Authentication required |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Business rule violation |
| 422 | Unprocessable Entity | Validation failed |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |

---

## ?? Rate Limiting

### Authentication Endpoints
- **Limit**: 5 requests per minute per IP
- **Policy**: Fixed window
- **Applies to**: `/api/auth/*`

### Financial Operations
- **Limit**: 20 requests per minute per user
- **Policy**: Token bucket
- **Applies to**: `/api/transactions/*`

### Rate Limit Headers
```http
X-RateLimit-Limit: 20
X-RateLimit-Remaining: 15
X-RateLimit-Reset: 2024-10-30T10:31:00Z
Retry-After: 60
```

---

## ?? Query Parameters

### Pagination
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)

### Filtering
- `searchTerm`: Search across relevant fields
- `isActive`: Filter by active status (true/false)
- `bankId`: Filter by bank ID
- `currencyId`: Filter by currency ID

### Sorting
- `sortBy`: Field to sort by
- `sortDirection`: `asc` or `desc`

### Example
```http
GET /api/users?pageNumber=2&pageSize=20&searchTerm=john&isActive=true&sortBy=fullName&sortDirection=asc
```

---

## ??? Security Headers

### Required Headers
```http
Authorization: Bearer {access_token}
Content-Type: application/json
Accept: application/json
```

### Optional Headers
```http
X-Tenant-ID: tenant-identifier  // For multi-tenant scenarios
X-Request-ID: unique-request-id  // For request tracking
User-Agent: MyApp/1.0
```

---

## ?? Testing Examples

### cURL Examples

#### Login
```bash
curl -X POST "https://localhost:7071/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin@123"}'
```

#### Get Balance
```bash
curl -X GET "https://localhost:7071/api/transactions/balance/1" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

#### Transfer Money
```bash
curl -X POST "https://localhost:7071/api/transactions/transfer" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceAccountId": 1,
    "targetAccountId": 2,
    "amount": 100.00,
    "description": "Test transfer"
  }'
```

### Postman Collection
A complete Postman collection is available with pre-configured requests and environment variables.

---

## ?? SDK & Client Libraries

### .NET Client Example
```csharp
public class BankingApiClient
{
    private readonly HttpClient _httpClient;
    private string _accessToken;

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        if (result.Success)
        {
            _accessToken = result.Data.AccessToken;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        
        return result.Data;
    }

    public async Task<decimal> GetBalanceAsync(int accountId)
    {
        var response = await _httpClient.GetAsync($"/api/transactions/balance/{accountId}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BalanceResponse>>();
        return result.Data.Balance;
    }
}
```

---

*API Reference Version: 1.0*  
*Last Updated: October 2024*  
*Base URL: https://localhost:7071/api*