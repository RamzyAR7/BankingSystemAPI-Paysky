# ?? Banking System API - Quick Start Guide

## ? Quick Setup (5 Minutes)

### 1. Prerequisites Check
```bash
dotnet --version  # Should be 8.0 or higher
```

### 2. Clone & Setup
```bash
git clone https://github.com/RamzyAR7/BankingSystemAPI-Paysky.git
cd BankingSystemAPI-Paysky
dotnet restore
```

### 3. Database Setup
```bash
cd src/BankingSystemAPI.Presentation
dotnet ef database update
```

### 4. Run Application
```bash
dotnet run
```

### 5. Access API
- **Swagger**: https://localhost:7071/swagger
- **Base URL**: https://localhost:7071/api

---

## ?? Default Login Credentials

### System Administrator
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

### Bank Manager
```json
{
  "username": "manager",
  "password": "Manager@123"
}
```

### Regular User
```json
{
  "username": "user",
  "password": "User@123"
}
```

---

## ?? Essential API Endpoints

### Authentication
```http
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```

### Create Account
```http
POST /api/checking-accounts
Authorization: Bearer {token}
{
  "userId": "user-id",
  "initialBalance": 1000.00,
  "currencyId": 1,
  "overdraftLimit": 500.00
}
```

### Transfer Money
```http
POST /api/transactions/transfer
Authorization: Bearer {token}
{
  "sourceAccountId": 1,
  "targetAccountId": 2,
  "amount": 100.00,
  "description": "Payment"
}
```

### Check Balance
```http
GET /api/transactions/balance/{accountId}
Authorization: Bearer {token}
```

---

## ??? Project Structure Quick Reference

```
src/
??? Domain/          # Business entities & rules
??? Application/     # Use cases & business logic
??? Infrastructure/  # Data access & external services
??? Presentation/    # API controllers & configuration
```

---

## ?? Common Configuration

### Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BankingSystemDB;Trusted_Connection=true;"
  }
}
```

### JWT Settings
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "BankingSystemAPI",
    "Audience": "BankingSystemAPI",
    "AccessTokenExpirationMinutes": 30
  }
}
```

---

## ?? Troubleshooting

### Database Issues
```bash
# Reset database
dotnet ef database drop
dotnet ef database update
```

### JWT Issues
- Check key length (minimum 32 characters)
- Verify token in Authorization header: `Bearer {token}`

### Rate Limiting
- Auth endpoints: 5 requests/minute per IP
- Money endpoints: 20 requests/minute per user

---

## ?? Key Features

? **Result Pattern**: No exceptions for better performance  
? **JWT Authentication**: With security stamp validation  
? **Rate Limiting**: Built-in API throttling  
? **Multi-Currency**: Support for different currencies  
? **Role-Based Access**: Granular permissions  
? **Background Jobs**: Interest calculation & token cleanup  
? **Swagger Documentation**: Interactive API docs  

---

## ?? Next Steps

1. **Review Full Documentation**: [Banking-System-API-Documentation.md](./Banking-System-API-Documentation.md)
2. **Understand Result Pattern**: [Result-Pattern-Architecture.md](./Result-Pattern-Architecture.md)
3. **Check Code Quality**: [Comprehensive-Code-Review.md](./Comprehensive-Code-Review.md)
4. **See Improvement Plan**: [Why-Not-Perfect-Score-Analysis.md](./Why-Not-Perfect-Score-Analysis.md)

---

*Need help? Check the full documentation or contact: rameya683@gmail.com*