# Manual API Test Cases

This document provides a set of manual test cases for common user flows and security checks. Use these scenarios to walk through the API's functionality and verify its behavior.

**Note:** You will need to use an API client like Postman or the Swagger UI. For authenticated requests, you must first log in and then use the received JWT as a Bearer token in the `Authorization` header for subsequent requests.

---

## Scenario 1: Administrator Onboards a New Client

This scenario covers an administrator creating a new user and a new bank account for them.

### Step 1: Log in as SuperAdmin
- **Action:** Authenticate as the administrator.
- **Endpoint:** `POST /api/auth/login`
- **Request Body:**
  ```json
  {
    "email": "superadmin@paysky.io",
    "password": "SuperAdmin@123"
  }
  ```
- **Expected Result:** A `200 OK` response containing a JWT token. **Copy this token for the next steps.**

### Step 2: Create a New Client User
- **Action:** The administrator creates a new user profile.
- **Endpoint:** `POST /api/users`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body:**
  ```json
  {
    "email": "test.client@example.com",
    "username": "testclient",
    "fullName": "Test Client",
    "nationalId": "99887766554433",
    "phoneNumber": "+15558675309",
    "dateOfBirth": "1985-05-20",
    "password": "ClientPassword123!",
    "passwordConfirm": "ClientPassword123!"
  }
  ```
- **Expected Result:** A `201 Created` response with the new user's data. **Copy the `id` of the new user for the next steps.**

### Step 3: Assign the 'Client' Role
- **Action:** The administrator assigns the `Client` role to the new user.
- **Endpoint:** `POST /api/userroles/Assign`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body:**
  ```json
  {
    "userId": "<paste_new_user_id_here>",
    "role": "Client"
  }
  ```
- **Expected Result:** A `200 OK` response confirming the role assignment.

### Step 4: Create a Checking Account for the New Client
- **Action:** The administrator creates a checking account for the new client.
- **Endpoint:** `POST /api/checking-accounts`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body:**
  ```json
  {
    "userId": "<paste_new_user_id_here>",
    "currencyId": 1,
    "initialBalance": 2500,
    "overdraftLimit": 250
  }
  ```
- **Expected Result:** A `201 Created` response with the new account's details.

---

## Scenario 2: Client Performs Self-Service Actions

This scenario tests the permissions of a standard client user.

### Step 1: Log in as the New Client
- **Action:** The newly created client authenticates.
- **Endpoint:** `POST /api/auth/login`
- **Request Body:**
  ```json
  {
    "email": "test.client@example.com",
    "password": "ClientPassword123!"
  }
  ```
- **Expected Result:** A `200 OK` response with a JWT token. **Copy this token.**

### Step 2 (Failure Case): Client Tries to Create Their Own Account
- **Action:** The client attempts to create an account for themselves. This should be blocked by the `RoleHierarchyFilter`.
- **Endpoint:** `POST /api/savings-accounts`
- **Authorization:** `Bearer {client_token}`
- **Request Body:**
  ```json
  {
    "userId": "<paste_client_user_id_here>",
    "currencyId": 4,
    "initialBalance": 10000,
    "interestRate": 3.0,
    "interestType": 1
  }
  ```
- **Expected Result:** A `403 Forbidden` response with the message: `"This action is not permitted for self-service. Please contact authorized bank personnel."`

### Step 3 (Success Case): Client Deposits Money
- **Action:** The client deposits money into the account created for them by the admin.
- **Endpoint:** `POST /api/accounts/deposit`
- **Authorization:** `Bearer {client_token}`
- **Request Body:**
  ```json
  {
    "accountId": 1, // Use the ID of the account created in Scenario 1
    "amount": 500
  }
  ```
- **Expected Result:** A `200 OK` response with the transaction details.

### Step 4 (Failure Case): Client Tries to Update Their Own Profile
- **Action:** The client attempts to change their `fullName`. This should be blocked by the `RoleHierarchyFilter`.
- **Endpoint:** `PUT /api/users/<paste_client_user_id_here>`
- **Authorization:** `Bearer {client_token}`
- **Request Body:**
  ```json
  {
    "email": "test.client@example.com",
    "username": "testclient",
    "fullName": "A New Name Chosen By The Client",
    "nationalId": "99887766554433",
    "phoneNumber": "+15558675309",
    "dateOfBirth": "1985-05-20"
  }
  ```
- **Expected Result:** A `403 Forbidden` response with the message: `"Users are not permitted to edit their own profile data. Please contact an administrator."`

### Step 5 (Success Case): Client Changes Their Password
- **Action:** The client successfully changes their own password.
- **Endpoint:** `PUT /api/users/<paste_client_user_id_here>/password`
- **Authorization:** `Bearer {client_token}`
- **Request Body:**
  ```json
  {
    "currentPassword": "ClientPassword123!",
    "newPassword": "MyNewPassword456!",
    "confirmNewPassword": "MyNewPassword456!"
  }
  ```
- **Expected Result:** A `200 OK` response.

---

## Scenario 3: Role Hierarchy Enforcement

This scenario tests the core functionality of the role hierarchy system.

### Step 1: Log in as SuperAdmin
- **Action:** Authenticate as SuperAdmin to get a token for administrative actions.
- **Endpoint:** `POST /api/auth/login`
- **Request Body:**
  ```json
  {
    "email": "superadmin@paysky.io",
    "password": "SuperAdmin@123"
  }
  ```
- **Expected Result:** A `200 OK` response with a JWT token. **Copy this token.**

### Step 2: Create Manager and Teller Roles
- **Action:** Create two new roles.
- **Endpoint:** `POST /api/roles/CreateRole`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body (for Manager):**
  ```json
  {
    "name": "Manager"
  }
  ```
- **Request Body (for Teller):**
  ```json
  {
    "name": "Teller"
  }
  ```
- **Expected Result:** `200 OK` responses for both requests.

### Step 3: Create Hierarchy (Manager > Teller)
- **Action:** Define that Managers have authority over Tellers.
- **Endpoint:** `POST /api/rolehierarchy/add-parent`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body:**
  ```json
  {
    "childRole": "Teller",
    "parentRole": "Manager"
  }
  ```
- **Expected Result:** A `200 OK` response.

### Step 4: Create Manager and Teller Users
- **Action:** Create a user for each of the new roles.
- **Endpoint:** `POST /api/users`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body (for Manager):**
  ```json
  {
    "email": "test.manager@example.com",
    "username": "testmanager",
    "fullName": "Test Manager",
    "nationalId": "12121212121212",
    "phoneNumber": "+15551112222",
    "dateOfBirth": "1980-01-01",
    "password": "ManagerPassword123!",
    "passwordConfirm": "ManagerPassword123!"
  }
  ```
- **Request Body (for Teller):**
  ```json
  {
    "email": "test.teller@example.com",
    "username": "testteller",
    "fullName": "Test Teller",
    "nationalId": "34343434343434",
    "phoneNumber": "+15553334444",
    "dateOfBirth": "1995-01-01",
    "password": "TellerPassword123!",
    "passwordConfirm": "TellerPassword123!"
  }
  ```
- **Expected Result:** `201 Created` for both. **Copy the `id` for both the manager and the teller.**

### Step 5: Assign Roles to New Users
- **Action:** Assign the corresponding roles to the new users.
- **Endpoint:** `POST /api/userroles/Assign`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body (for Manager):**
  ```json
  {
    "userId": "<manager_user_id>",
    "role": "Manager"
  }
  ```
- **Request Body (for Teller):**
  ```json
  {
    "userId": "<teller_user_id>",
    "role": "Teller"
  }
  ```
- **Expected Result:** `200 OK` for both.

### Step 6: Create an Account for the Teller
- **Action:** SuperAdmin creates an account owned by the Teller user.
- **Endpoint:** `POST /api/checking-accounts`
- **Authorization:** `Bearer {superadmin_token}`
- **Request Body:**
  ```json
  {
    "userId": "<teller_user_id>",
    "currencyId": 1,
    "initialBalance": 500,
    "overdraftLimit": 0
  }
  ```
- **Expected Result:** `201 Created`. **Copy the new `id` of the account.**

### Step 7 (Success Case): Manager Accesses Teller's Account
- **Action:** Log in as the Manager and attempt to view the Teller's account details. This should be **allowed**.
- **First, Log in as Manager:** `POST /api/auth/login` with manager credentials to get a token.
- **Then, access the endpoint:**
- **Endpoint:** `GET /api/accounts/<teller_account_id>`
- **Authorization:** `Bearer {manager_token}`
- **Expected Result:** A `200 OK` response with the Teller's account details.

### Step 8 (Failure Case): Teller Tries to Update Manager's Profile
- **Action:** Log in as the Teller and attempt to update the Manager's user profile. This should be **blocked**.
- **First, Log in as Teller:** `POST /api/auth/login` with teller credentials to get a token.
- **Then, access the endpoint:**
- **Endpoint:** `PUT /api/users/<manager_user_id>`
- **Authorization:** `Bearer {teller_token}`
- **Request Body:**
  ```json
  {
    "email": "test.manager.hacked@example.com",
    "username": "testmanagerhacked",
    "fullName": "Test Manager Hacked",
    "nationalId": "12121212121212",
    "phoneNumber": "+15551112222",
    "dateOfBirth": "1980-01-01"
  }
  ```
- **Expected Result:** A `403 Forbidden` response with the message: `"Not authorized to manage target user."`