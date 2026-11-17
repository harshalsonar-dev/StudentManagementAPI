# Student Management Enterprise API 🎓 (.NET 8 Web API)

A production-ready, highly secure, and scalable **Enterprise REST API** for Student Management, built with **C#**, **.NET 8**, **Clean Architecture**, **CQRS with MediatR**, **Entity Framework Core**, **SQL Server**, **ASP.NET Core Identity**, and **JWT Authentication with Refresh Tokens**.

---

## 🌟 Key Features

### 1. Clean Architecture & CQRS
- **Domain Layer**: Core domain models, base entity abstraction, enums, exceptions, and repository interfaces. Pure C# with zero external dependencies.
- **Application Layer**: CQRS commands and queries using **MediatR**, validation via **FluentValidation**, MediatR pipeline behaviors (logging and automatic validation), DTOs, and interface contracts.
- **Infrastructure Layer**: EF Core database context, migrations, Generic Repository implementation, Unit of Work with transaction management, ASP.NET Core Identity integration, and cryptographic JWT/Refresh Token services.
- **API Layer**: Clean controllers inheriting from `ApiControllerBase`, global exception handling middleware, enterprise rate limiting, Serilog logging configuration, and interactive Swagger/OpenAPI documentation.

### 2. Enterprise-Grade Security
- **JWT + Refresh Tokens**: Short-lived access tokens (15 minutes) signed with HMAC-SHA256, paired with secure 64-byte cryptographic refresh tokens (7 days).
- **Token Rotation & Theft Detection**: Refresh tokens are rotated upon each renewal. If a previously revoked token is reused, all active tokens for that user are immediately revoked to protect against session hijacking.
- **Role-Based Access Control (RBAC)**: Fine-grained authorization based on user roles (`Admin`, `Teacher`, `Student`).
- **Identity Password & Lockout Policy**: Enforces strict password complexity (minimum 8 characters, uppercase, lowercase, digit, and symbol) and temporarily locks out accounts after 5 failed login attempts for 5 minutes.
- **Rate Limiting**: .NET 8 fixed-window rate limiters protecting against denial-of-service and brute-force attacks (10 req/min on authentication endpoints, 100 req/min for general API calls).
- **CORS**: Configurable cross-origin policies for secure frontend integration (React, Angular, Vue, etc.).

### 3. Robust Data Management & Performance
- **Server-Side Pagination, Filtering & Sorting**: Supports pagination (`pageNumber`, `pageSize`), multi-field search (`searchTerm`), category filtering (`course`, `isActive`), and dynamic multi-column sorting (`sortBy`, `isAscending`).
- **Generic Repository & Unit of Work**: Clean data access abstraction supporting transactional consistency and query decoupling.
- **Duplicate Prevention**: Enforces database-level unique constraints and application-level pre-validation on email addresses.

### 4. Logging & Diagnostics
- **Structured Logging (Serilog)**: Colorful console logging and daily rolling file logs (`logs/student-api-.log`) with 14-day retention.
- **MediatR Pipeline Logging**: Automatically tracks and logs the execution duration of every command and query.

### 5. Standardized Error Handling
- **Global Exception Middleware**: Intercepts unhandled exceptions and validation errors, transforming them into standardized `ApiResponse<T>` envelopes with appropriate HTTP status codes (`400`, `401`, `403`, `404`, `409`, `500`). Production error details are sanitized to prevent leaking sensitive infrastructure info.

---

## 🔑 Default Admin Credentials

When the API launches, the database is automatically migrated and seeded with default roles and an administrator account:

| Parameter | Value |
|---|---|
| **Email** | `admin@studententerprise.com` |
| **Password** | `EnterpriseAdmin@2026!` |
| **Role** | `Admin` |

---

## 📁 Solution Structure

```
StudentManagementAPI/
│
├── src/
│   ├── StudentManagement.Domain/                 # Core domain entities, enums, exceptions, interfaces
│   │   ├── Common/BaseEntity.cs
│   │   ├── Entities/Student.cs, RefreshToken.cs
│   │   ├── Enums/AppRoles.cs
│   │   ├── Exceptions/DomainExceptions.cs
│   │   └── Interfaces/IGenericRepository.cs, IStudentRepository.cs, IUnitOfWork.cs
│   │
│   ├── StudentManagement.Application/            # CQRS commands, queries, validators, DTOs, pipeline behaviors
│   │   ├── Common/
│   │   │   ├── Behaviors/ValidationBehavior.cs, LoggingBehavior.cs
│   │   │   └── Models/ApiResponse.cs, PagedResult.cs, PaginationParameters.cs
│   │   ├── DTOs/Auth/AuthDtos.cs, Students/StudentDtos.cs
│   │   ├── Features/
│   │   │   ├── Auth/Commands (Login, Register, RefreshToken, RevokeToken)
│   │   │   └── Students/ (Commands: Create, Update, Delete | Queries: Paged, ById, Stats)
│   │   └── Interfaces/ITokenService.cs, IIdentityService.cs
│   │
│   ├── StudentManagement.Infrastructure/         # EF Core, Identity, JWT, Repositories, Migrations
│   │   ├── Data/ApplicationDbContext.cs, DbSeeder.cs
│   │   ├── Identity/ApplicationUser.cs
│   │   ├── Migrations/
│   │   ├── Repositories/GenericRepository.cs, StudentRepository.cs, UnitOfWork.cs
│   │   └── Services/IdentityService.cs, TokenService.cs
│   │
│   └── StudentManagement.API/                    # Web API host, controllers, middleware, Swagger, rate limiting
│       ├── Controllers/ApiControllerBase.cs, AuthController.cs, StudentsController.cs
│       ├── Extensions/RateLimitingExtensions.cs
│       ├── Middleware/GlobalExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── StudentManagementEnterprise.http     # Complete ready-to-run REST test requests
│
├── tests/
│   └── StudentManagement.UnitTests/              # 81 comprehensive unit tests (xUnit, Moq, FluentAssertions)
│       ├── Features/Students/ (Create, Update, Delete, ById, Paged, Stats, Validators)
│       ├── Features/Auth/ (Login, Register, RefreshToken, RevokeToken, TokenService, IdentityService)
│       ├── Infrastructure/ (GenericRepository in-memory DB tests)
│       └── Middleware/ (GlobalExceptionHandlingMiddleware tests)
│
└── StudentManagementSystem.sln
```

---

## 📡 API Endpoints Reference

### 🔐 Authentication & Identity (`/api/v1/auth`)

| HTTP Method | Route | Description | Authorization |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Register a new user account (Student / Teacher) | Public |
| `POST` | `/api/v1/auth/login` | Authenticate user; returns JWT Access & Refresh tokens | Public |
| `POST` | `/api/v1/auth/refresh-token` | Exchange expired access token + refresh token for new pair | Public |
| `POST` | `/api/v1/auth/revoke-token` | Revoke an active refresh token | Public |
| `GET`  | `/api/v1/auth/me` | Retrieve profile and roles of authenticated user | `[Authorize]` |
| `GET`  | `/api/v1/auth/roles` | List all available enterprise roles | `[Authorize(Roles="Admin")]` |
| `POST` | `/api/v1/auth/assign-role` | Assign role (`Admin`, `Teacher`, `Student`) to user | `[Authorize(Roles="Admin")]` |

### 👨‍🎓 Student Management (`/api/v1/students`)

| HTTP Method | Route | Description | Authorization |
|---|---|---|---|
| `GET`    | `/api/v1/students` | Get paginated students (search, filter, sort) | `[Authorize]` |
| `GET`    | `/api/v1/students/{id}` | Get student details by ID | `[Authorize]` |
| `GET`    | `/api/v1/students/statistics` | Get student enrollment count grouped by course | `[Authorize]` |
| `POST`   | `/api/v1/students` | Create a new student record | `[Authorize(Roles="Admin,Teacher")]` |
| `PUT`    | `/api/v1/students/{id}` | Update an existing student record | `[Authorize(Roles="Admin,Teacher")]` |
| `DELETE` | `/api/v1/students/{id}` | Delete a student record | `[Authorize(Roles="Admin")]` |

---

## 🧪 Testing & Quality Assurance

The project includes an automated test suite containing **81 unit tests** with **100% pass rate**:

```
Test Run Summary:
========================================================================================
Total Tests : 81
Passed      : 81
Failed      : 0
Skipped     : 0
Duration    : ~1.5s
Framework   : xUnit, Moq, FluentAssertions, EF Core In-Memory
========================================================================================
```

### Covered Test Categories:
- **Positive CRUD Operations**: Successful create, retrieve, paged query, update, and delete.
- **Negative & Edge Cases**:
  - `400 Bad Request`: Payload validation failures (names, emails, age boundaries, phone regex, invalid roles).
  - `401 Unauthorized`: Missing tokens, expired tokens, invalid credentials, account lockouts.
  - `404 Not Found`: Non-existent entity queries and deletions.
  - `409 Conflict`: Duplicate email handling on insert and update.
  - `500 Server Error`: Graceful handling and sanitized problem details upon unexpected database or server faults.
- **Security Tests**: Cryptographic token generation, token rotation, token reuse attack detection, and role verification.

---

## 🚀 Getting Started

### 1. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server LocalDB (`(localdb)\mssqllocaldb`)

### 2. Clone Repository
```bash
git clone https://github.com/harshalsonar-dev/StudentManagementAPI.git
cd StudentManagementAPI
```

### 3. Run Database Migrations
Migrations are applied automatically at startup. To run them manually via CLI:
```powershell
dotnet ef database update --project "src/StudentManagement.Infrastructure" --startup-project "src/StudentManagement.API"
```

### 4. Run the API Server
```powershell
dotnet run --project "src/StudentManagement.API"
```

### 5. Access Interactive Documentation
Open your browser and navigate to:
👉 **[http://localhost:5000](http://localhost:5000)** *(Swagger UI)*

### 6. Run Automated Tests
```powershell
dotnet test
```

### 7. Direct HTTP File Testing
You can execute pre-configured requests directly in VS Code (REST Client) or Visual Studio using [`StudentManagementEnterprise.http`](file:///e:/New%20folder/My_Project/StudentManagementAPI/src/StudentManagement.API/StudentManagementEnterprise.http).

---

## 📄 License
This project is open-source and available under the [MIT License](LICENSE).
