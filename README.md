# Student Management API 🎓 (.NET 8 Web API)

हा एक संपूर्ण (Production-Ready) आणि मॉडर्न **Student Management Web API** प्रोजेक्ट आहे, जो **ASP.NET Core 8**, **Entity Framework Core**, **SQL Server**, **ASP.NET Core Identity**, आणि **JWT Bearer Authentication** चा वापर करून तयार करण्यात आला आहे.

---

## 🌟 वैशिष्ट्ये (Key Features)

1. **Clean Architecture (3-Tier Structure)**:
   - **Controllers**: API Endpoints आणि HTTP Request/Response व्यवस्थापन.
   - **Services**: Business Logic, Validation, आणि DTO Mapping.
   - **Repositories**: Database Data Access Logic (EF Core सह).
   - **Data Transfer Objects (DTOs)**: सुरक्षित डेटा देवाणघेवाण, Entities थेट बाहेरील युजर्सना दाखवले जात नाहीत.

2. **Security & Authentication**:
   - **ASP.NET Core Identity**: युजर रजिस्ट्रेशन, पासवर्ड हॅशिंग, आणि लॉकआउट पॉलिसी (5 अयशस्वी प्रयत्नांनंतर 5 मिनिटे लॉक).
   - **JWT Authentication**: JSON Web Tokens वापरून सुरक्षित ऑथेंटिकेशन.
   - **Role-Based Access Control (RBAC)**:
     - `Admin`: सर्व अधिकार (विद्यार्थी Delete करणे, नवीन Role देणे, सर्व माहिती पाहणे).
     - `Teacher`: विद्यार्थी Add व Update करण्याचे अधिकार.
     - `Student`: विद्यार्थी माहिती व प्रोफाइल पाहण्याचे अधिकार.

3. **Student Management CRUD**:
   - सर्व विद्यार्थी पाहणे (Pagination, Search, Course Filter, Sorting सह).
   - एका विद्यार्थ्याची माहिती ID द्वारे मिळवणे (`GET /api/student/{id}`).
   - नवीन विद्यार्थी जोडणे (`POST /api/student`) - युनिक ईमेल व्हेरिफिकेशन सह.
   - विद्यार्थ्याची माहिती अपडेट करणे (`PUT /api/student/{id}`).
   - विद्यार्थी डिलीट करणे (`DELETE /api/student/{id}`).
   - कोर्सनुसार विद्यार्थ्यांची आकडेवारी (`GET /api/student/statistics`).

4. **Global Exception Handling**:
   - `ExceptionMiddleware` द्वारे कोणत्याही अनपेक्षित त्रुटीचे सुरक्षित आणि व्यवस्थित JSON एरर फॉरमॅटमध्ये रूपांतर.

5. **Swagger / OpenAPI Documentation**:
   - रूट URL वर (`http://localhost:5211/`) संपूर्ण स्वॅगर UI उपलब्ध, जिथे 'Authorize' बटण वापरून थेट JWT टोकन टेस्ट करता येते.

---

## 🔑 Default Admin Credentials (डिफ़ॉल्ट लॉगिन)

प्रोजेक्ट पहिल्यांदा रन होताच सिस्टीममध्ये डिफ़ॉल्ट Admin युजर आपोआप तयार केला जातो:

| Parameter | Value |
|-----------|-------|
| **Email** | `admin@studentapi.com` |
| **Password** | `Admin@123456` |
| **Role** | `Admin` |

---

## 📁 प्रोजेक्ट स्ट्रक्चर (Project Structure)

```
StudentManagementAPI/
│
├── StudentManagementSystem/
│   ├── Controllers/
│   │   ├── AuthController.cs          # Register, Login, Me, Roles, Assign-Role
│   │   └── StudentController.cs       # Student CRUD & Stats
│   ├── Data/
│   │   ├── AppDbContext.cs            # IdentityDbContext & EF Core Mapping
│   │   └── DbInitializer.cs           # Database Seeder (Roles, Admin, Sample Data)
│   ├── DTOs/
│   │   ├── Common/                    # ApiResponse<T>, PagedResult<T>
│   │   ├── Students/                  # CreateDto, UpdateDto, ResponseDto, QueryParams
│   │   └── Auth/                      # AuthResponseDto, AssignRoleDto
│   ├── Middleware/
│   │   └── ExceptionMiddleware.cs     # Centralized Exception Handler
│   ├── Migrations/                    # EF Core Migrations
│   ├── Models/
│   │   ├── Student.cs                 # Student Entity
│   │   ├── LoginRequest.cs            # Login Model
│   │   └── RegisterRequest.cs         # Register Model
│   ├── Repositories/
│   │   ├── IStudentRepository.cs
│   │   └── StudentRepository.cs
│   ├── Services/
│   │   ├── IStudentService.cs
│   │   └── StudentService.cs
│   ├── appsettings.json               # Database Connection & JWT Configuration
│   ├── Program.cs                     # App Startup & Dependency Injection
│   ├── StudentManagementSystem.csproj
│   └── StudentManagementSystem.http   # Ready-to-use HTTP Requests for Testing
└── README.md
```

---

## 🚀 हा प्रोजेक्ट कसा चालवावा (How to Run)

### 1. Requirements:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server किंवा SQL Server LocalDB (`(localdb)\mssqllocaldb`)

### 2. Run Database Migrations:
डेटाबेस व टेबल्स आधीच तयार करण्यात आले आहेत. तरीही मॅन्युअली अपडेट करायचे असल्यास:
```powershell
cd "StudentManagementSystem"
dotnet ef database update
```

### 3. Run Application:
```powershell
dotnet run --project "StudentManagementSystem"
```
किंवा Visual Studio / VS Code मध्ये `F5` दाबा.

### 4. Swagger UI ओपन करा:
ब्राउझरमध्ये उघडा:
👉 **http://localhost:5211/** (किंवा https://localhost:7199/)

---

## 📡 मुख्य API Endpoints

### 🔐 1. Authentication (`/api/auth`)
| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `POST` | `/api/auth/register` | नवीन युजर नोंदणी करा | सार्वजनिक (Public) |
| `POST` | `/api/auth/login` | लॉगिन करा आणि JWT Token मिळवा | सार्वजनिक (Public) |
| `GET`  | `/api/auth/me` | लॉगिन केलेल्या युजरचे प्रोफाइल पहा | `[Authorize]` |
| `GET`  | `/api/auth/roles` | सर्व उपलब्ध Roles ची यादी पहा | `[Authorize(Roles="Admin")]` |
| `POST` | `/api/auth/assign-role`| युजरला नवीन Role द्या | `[Authorize(Roles="Admin")]` |

### 👨‍🎓 2. Student Management (`/api/student`)
| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET`  | `/api/student` | विद्यार्थ्यांची यादी (Paging, Filter, Search) | `[Authorize]` |
| `GET`  | `/api/student/{id}` | विशिष्ट विद्यार्थ्याची माहिती मिळवा | `[Authorize]` |
| `GET`  | `/api/student/statistics` | कोर्सनिहाय विद्यार्थ्यांची संख्या | `[Authorize]` |
| `POST` | `/api/student` | नवीन विद्यार्थी ॲड करा | `[Authorize(Roles="Admin,Teacher")]` |
| `PUT`  | `/api/student/{id}` | विद्यार्थ्यांची माहिती अपडेट करा | `[Authorize(Roles="Admin,Teacher")]` |
| `DELETE`| `/api/student/{id}`| विद्यार्थी डिलीट करा | `[Authorize(Roles="Admin")]` |

---

## 🧪 Testing with HTTP File
प्रोजेक्टमधील `StudentManagementSystem.http` फाइल वापरून तुम्ही VS Code किंवा Visual Studio च्या REST Client द्वारे सर्व Endpoints थेट टेस्ट करू शकता.

