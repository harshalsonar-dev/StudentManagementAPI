# Student Management Enterprise API (.NET 8)

An enterprise-grade RESTful API for student management built with C# and .NET 8, applying Clean Architecture, CQRS with MediatR, Entity Framework Core with SQL Server, ASP.NET Core Identity, and JWT authentication with cryptographic refresh tokens.

---

## Architecture Overview

The solution adheres to Clean Architecture principles, ensuring clear separation of concerns, decoupling from frameworks, and high testability:

- **Domain Layer (`src/StudentManagement.Domain`)**
  - Contains core business entities (`Student`, `RefreshToken`).
  - Defines shared abstractions (`BaseEntity`), application roles (`AppRoles`), domain-specific exceptions, and repository contracts (`IStudentRepository`, `IGenericRepository`, `IUnitOfWork`).
  - Free of external dependencies or framework couplings.

- **Application Layer (`src/StudentManagement.Application`)**
  - Implements CQRS command and query handlers via MediatR.
  - Request validation using FluentValidation rules integrated directly into the pipeline (`ValidationBehavior`).
  - Cross-cutting concerns such as performance and telemetry logging (`LoggingBehavior`).
  - Standardized API envelope models (`ApiResponse<T>`), pagination structures (`PagedResult<T>`, `PaginationParameters`), and Data Transfer Objects (DTOs).

- **Infrastructure Layer (`src/StudentManagement.Infrastructure`)**
  - EF Core database context (`ApplicationDbContext`) and migrations.
  - Implementation of Generic Repository and Unit of Work with transaction management.
  - ASP.NET Core Identity integration (`IdentityService`) for secure password hashing and account lockout.
  - Token service (`TokenService`) for HMAC-SHA256 JWT generation and secure 64-byte cryptographic refresh token rotation.
  - Automated database migration and initial administrator seeding (`DbSeeder`).

- **API Presentation Layer (`src/StudentManagement.API`)**
  - Thin API controllers deriving from `ApiControllerBase`.
  - Global exception middleware (`GlobalExceptionHandlingMiddleware`) providing uniform JSON responses across all status codes.
  - Rate limiting policies (fixed-window limiters for authentication and general endpoints).
  - Structured application logging with Serilog (Console and daily rolling file sinks).
  - Interactive Swagger / OpenAPI documentation.

---

## Default Administrator Credentials

Upon first startup, the database is automatically created and seeded with default roles and an administrator account:

| Field | Value |
|---|---|
| **Email** | `admin@studententerprise.com` |
| **Password** | `EnterpriseAdmin@2026!` |
| **Assigned Role** | `Admin` |

---

## API Endpoints Reference

### Authentication Endpoints (`/api/v1/auth`)

| Method | Endpoint | Description | Access Level |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Register a new user account | Public |
| `POST` | `/api/v1/auth/login` | Authenticate with email and password | Public |
| `POST` | `/api/v1/auth/refresh-token` | Renew access token using refresh token | Public |
| `POST` | `/api/v1/auth/revoke-token` | Revoke a refresh token | Public |
| `GET` | `/api/v1/auth/me` | Fetch profile information of the current user | Authenticated |
| `GET` | `/api/v1/auth/roles` | List all system roles | Admin only |
| `POST` | `/api/v1/auth/assign-role` | Assign role (`Admin`, `Teacher`, `Student`) to a user | Admin only |

### Student Management Endpoints (`/api/v1/students`)

| Method | Endpoint | Description | Access Level |
|---|---|---|---|
| `GET` | `/api/v1/students` | Get paginated list of students with filtering and sorting | Authenticated |
| `GET` | `/api/v1/students/{id}` | Retrieve single student by unique ID | Authenticated |
| `GET` | `/api/v1/students/statistics` | Course-wise enrollment statistics | Authenticated |
| `POST` | `/api/v1/students` | Register a new student record | Admin, Teacher |
| `PUT` | `/api/v1/students/{id}` | Update student details | Admin, Teacher |
| `DELETE` | `/api/v1/students/{id}` | Remove a student record | Admin only |

---

## Standardized Response Envelope

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

## Quick Start & Usage Examples

### 1. Authenticate & Obtain JWT Token

```bash
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@studententerprise.com",
    "password": "EnterpriseAdmin@2026!"
  }'
```

### 2. Query Students (with Search, Filter, Sort & Pagination)

```bash
curl -X GET "http://localhost:5000/api/v1/students?pageNumber=1&pageSize=10&searchTerm=John&course=Computer%20Science&sortBy=LastName&isAscending=true" \
  -H "Authorization: Bearer <YOUR_ACCESS_TOKEN>"
```

### 3. Create a New Student

```bash
curl -X POST "http://localhost:5000/api/v1/students" \
  -H "Authorization: Bearer <YOUR_ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@example.com",
    "dateOfBirth": "2002-08-20",
    "phoneNumber": "+1234567890",
    "course": "Data Science",
    "enrollmentDate": "2024-09-01",
    "isActive": true
  }'
```

---

## Automated Test Suite

The solution includes an extensive automated test project (`tests/StudentManagement.UnitTests`) containing **81 unit tests** with a **100% pass rate**.

```
Test Execution Summary:
----------------------------------------------------------------------------------------
Total Tests : 81
Passed      : 81
Failed      : 0
Skipped     : 0
Tooling     : xUnit, Moq, FluentAssertions, EF Core In-Memory Database
----------------------------------------------------------------------------------------
```

### Test Coverage Highlights
- **Command & Query Handlers**: Complete CRUD coverage for student operations and authentication workflows.
- **FluentValidation Rules**: Positive and edge-case validation checks (boundary ages, phone formats, required fields).
- **Security & Token Services**: Cryptographic token generation, refresh token rotation, token reuse detection, and role assignment validation.
- **Generic Repository & Unit of Work**: In-memory database integration tests verifying transactional integrity and queries.
- **Global Exception Middleware**: Verification of status codes and sanitized responses for unexpected exceptions.

To run all automated tests:
```powershell
dotnet test
```

---

## Local Setup & Development

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server LocalDB (`(localdb)\mssqllocaldb`)

### Steps to Run
1. **Clone the repository:**
   ```bash
   git clone https://github.com/harshalsonar-dev/StudentManagementAPI.git
   cd StudentManagementAPI
   ```
2. **Build the solution:**
   ```powershell
   dotnet build
   ```
3. **Execute database migrations (optional, applied automatically on launch):**
   ```powershell
   dotnet ef database update --project "src/StudentManagement.Infrastructure" --startup-project "src/StudentManagement.API"
   ```
4. **Start the API:**
   ```powershell
   dotnet run --project "src/StudentManagement.API"
   ```
5. **Explore Swagger UI:**
   Navigate to [http://localhost:5000](http://localhost:5000) in your web browser.
