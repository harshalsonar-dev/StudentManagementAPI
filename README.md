# Student Management Enterprise API (.NET 8 / 9 / 10 CI)

A production-ready, highly secure, and scalable **Enterprise REST API** for Student Management, built with **C#**, **Clean Architecture**, **CQRS with MediatR**, **Entity Framework Core**, **SQL Server**, **ASP.NET Core Identity**, and **JWT Authentication with Cryptographic Refresh Token Hashing**.

---

## 🏗️ Architecture Overview

The solution adheres strictly to Clean Architecture principles, ensuring clear separation of concerns, decoupling from external frameworks, and high testability:

- **Domain Layer (`src/StudentManagement.Domain`)**
  - Contains core business entities (`Student`, `RefreshToken`).
  - Defines shared abstractions (`BaseEntity`), system roles (`AppRoles`), domain-specific exceptions, and repository contracts (`IStudentRepository`, `IGenericRepository`, `IUnitOfWork`).
  - Completely free of external dependencies or framework couplings.

- **Application Layer (`src/StudentManagement.Application`)**
  - Implements CQRS command and query handlers via **MediatR**.
  - Request validation using **FluentValidation** integrated into the pipeline (`ValidationBehavior`).
  - Telemetry and performance logging behavior (`LoggingBehavior`).
  - Standardized API envelope models (`ApiResponse<T>`), pagination structures (`PagedResult<T>`, `PaginationParameters`), and Data Transfer Objects (DTOs).

- **Infrastructure Layer (`src/StudentManagement.Infrastructure`)**
  - EF Core database context (`ApplicationDbContext`) and migrations.
  - Implementation of Generic Repository and Unit of Work with transaction management.
  - ASP.NET Core Identity integration (`IdentityService`) for secure password hashing and account lockout.
  - Cryptographic token service (`TokenService`) implementing HMAC-SHA256 JWT access tokens and 64-byte random refresh tokens **stored as SHA-256 hashes at rest**.
  - Secure administrator seeding via `IConfiguration` without hardcoded passwords (`DbSeeder`).

- **API Presentation Layer (`src/StudentManagement.API`)**
  - Thin API controllers deriving from `ApiControllerBase` with full `CancellationToken` propagation.
  - Global exception middleware (`GlobalExceptionHandlingMiddleware`) providing uniform JSON responses across all status codes.
  - Fixed-window rate limiting policies (10 req/min auth, 100 req/min general).
  - Structured application logging with **Serilog** (Console and 14-day rolling file sink).
  - Production-hardened CORS policies (no wildcard fallback in non-development environments).
  - Interactive Swagger / OpenAPI documentation.

---

## 🔒 Security Hardening

1. **Cryptographic Refresh Token Hashing**: Refresh tokens are generated using cryptographically secure 64-byte random byte arrays and stored in the database exclusively as **SHA-256 hashes**. Plaintext tokens are never stored at rest.
2. **Token Rotation & Hijack Detection**: Refresh tokens are rotated upon each renewal. If a previously revoked token is reused, all active tokens for that user are immediately revoked to protect against session hijacking.
3. **Privilege Escalation Prevention**: Public registration (`POST /api/v1/auth/register`) strictly assigns the `Student` role. The `Role` field is completely omitted from the public registration DTO. Privileged roles (`Admin`, `Teacher`) can only be assigned by an authenticated administrator.
4. **Secret Management & Zero Hardcoded Passwords**: All production connection strings, JWT signing keys, and administrator passwords have been removed from source-controlled configuration files. Secrets are managed using **ASP.NET Core User Secrets** in local development and environment variables in production.
5. **Strict CORS Policy**: Disallows wildcard origins (`*`) whenever credentials are enabled. Non-development environments require explicit origin configuration and fail fast if missing.
6. **Account Lockout & Password Complexity**: Enforces 8+ characters with uppercase, lowercase, numeric, and special character rules. Automatically locks accounts for 5 minutes after 5 consecutive failed login attempts.

---

## 📡 API Endpoints Reference

### 🔐 Authentication & Identity (`/api/v1/auth`)

| Method | Endpoint | Description | Access Level |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Register a new student account (always assigned `Student` role) | Public |
| `POST` | `/api/v1/auth/login` | Authenticate with email and password | Public |
| `POST` | `/api/v1/auth/refresh-token` | Renew access token using refresh token | Public |
| `POST` | `/api/v1/auth/revoke-token` | Revoke a refresh token | Public |
| `GET` | `/api/v1/auth/me` | Fetch profile information of the current user | Authenticated |
| `GET` | `/api/v1/auth/roles` | List all system roles | Admin only |
| `POST` | `/api/v1/auth/assign-role` | Assign role (`Admin`, `Teacher`, `Student`) to an existing user | Admin only |

### 👨‍🎓 Student Management (`/api/v1/students`)

| Method | Endpoint | Description | Access Level |
|---|---|---|---|
| `GET` | `/api/v1/students` | Get paginated list of students with filtering and sorting | Authenticated |
| `GET` | `/api/v1/students/{id}` | Retrieve single student by unique ID | Authenticated |
| `GET` | `/api/v1/students/statistics` | Course-wise enrollment statistics | Authenticated |
| `POST` | `/api/v1/students` | Register a new student record | Admin, Teacher |
| `PUT` | `/api/v1/students/{id}` | Update student details | Admin, Teacher |
| `DELETE` | `/api/v1/students/{id}` | Remove a student record | Admin only |

---

## 📦 Standardized Response Envelope

All API endpoints return responses encapsulated in the unified `ApiResponse<T>` structure:

### Success Response Example (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Students retrieved successfully",
  "data": {
    "items": [
      {
        "id": 1,
        "firstName": "John",
        "lastName": "Doe",
        "email": "john.doe@example.com",
        "dateOfBirth": "2001-05-15",
        "course": "Computer Science",
        "enrollmentDate": "2024-09-01",
        "isActive": true
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "errors": []
}
```

### Validation Error Response Example (`400 Bad Request`)
```json
{
  "success": false,
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "data": null,
  "errors": [
    "First name is required.",
    "Invalid email format."
  ]
}
```

---

## 🧪 Automated Testing & Quality Assurance

The solution includes an automated test suite containing **90 unit tests** with a **100% pass rate**:

```
Test Run Summary:
========================================================================================
Total Tests : 90
Passed      : 90
Failed      : 0
Skipped     : 0
Frameworks  : xUnit, Moq, FluentAssertions, EF Core In-Memory Database
========================================================================================
```

### Test Coverage Highlights:
- **Registration Security**: Verification that public registration assigns only `Student` role and rejects role escalation attempts.
- **Refresh Token Hashing**: Verification that tokens in the database are hashed with SHA-256 and never stored in plaintext.
- **Token Rotation & Revocation**: Verification of token rotation on refresh and immediate revocation of all user tokens upon replay attacks.
- **JWT Cryptographic Verification**: Signature tampering detection, issuer validation, audience validation, and expired token handling.
- **Full CRUD & Validation**: Validations across student creation, boundary age checks, email uniqueness, course statistics, and paginated queries.
- **Global Exception Middleware**: Status code mapping and production sanitization for unexpected exceptions.

---

## 🚀 Local Setup & Configuration

### 1. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server or SQL Server LocalDB (`(localdb)\mssqllocaldb`)

### 2. Clone Repository
```bash
git clone https://github.com/harshalsonar-dev/StudentManagementAPI.git
cd StudentManagementAPI
```

### 3. Configure User Secrets (Local Development)
Set your local development secrets:
```powershell
cd src/StudentManagement.API
dotnet user-secrets set "Jwt:Key" "YourDevelopmentSuperSecretSigningKeyAtLeast32BytesLong!"
dotnet user-secrets set "InitialAdmin:Email" "admin@studententerprise.local"
dotnet user-secrets set "InitialAdmin:Password" "Admin@YourSecurePass2026!"
cd ../..
```

### 4. Build Solution
```powershell
dotnet build StudentManagementSystem.sln --configuration Release
```

### 5. Run Automated Tests
```powershell
dotnet test StudentManagementSystem.sln --configuration Release
```

### 6. Run the API Server
```powershell
dotnet run --project "src/StudentManagement.API"
```

### 7. Interactive API Documentation
Open your browser and navigate to:
👉 **[http://localhost:5000](http://localhost:5000)** *(Swagger UI)*

---

## ⚙️ Continuous Integration (GitHub Actions)

Every push and pull request to the `main` branch is automatically verified using the GitHub Actions workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml):
- Restores dependencies
- Builds the complete solution in Release configuration
- Executes all 90 automated unit tests
- Enforces zero compiler errors or test failures
