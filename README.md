# Campus & Corporate Job and Internship Placement Portal

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue?logo=microsoft)](https://learn.microsoft.com/aspnet/core/mvc)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-10.0-68217A?logo=csharp)](https://learn.microsoft.com/ef/core)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-CC292B?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Tests Passing](https://img.shields.io/badge/Automated%20Tests-17%2F17%20Passed-brightgreen)](https://github.com/)

A full-stack, enterprise-grade recruitment and placement management platform developed with **ASP.NET Core 10 MVC**, **Entity Framework Core**, and **ASP.NET Core Identity**. The portal manages the complete hiring pipeline from job posting and application with resume upload to interview scheduling, final hire selection, and institutional **Single Placement Policy** enforcement.

---

## Key Features

### 1. Multi-Tenant Role-Based Access Control (RBAC)
- **Job Seeker (Student):** Browse and search jobs, filter by employment type and location, bookmark/save jobs, submit applications with PDF resume upload, track status on a personal dashboard, view scheduled interviews.
- **Recruiter (Company):** Create and manage company profile, post and edit job openings, review applicants, download resumes, advance candidates through a structured workflow pipeline, schedule interviews, and select final hires.
- **Administrator:** System oversight, user and role management, job posting approvals and moderation, aggregate platform analytics.

### 2. Autonomous Recruitment Workflow Engine
Applications progress through a strict, non-reversible state machine:
$$\text{Applied} \longrightarrow \text{Under Review} \longrightarrow \text{Shortlisted} \longrightarrow \text{Interview} \longrightarrow \text{Selected / Rejected}$$

### 3. Institutional Single Placement Policy
Once a student is marked as **Selected** for any job, other companies are automatically restricted from selecting that candidate. Recruiters see a **Placed Elsewhere** badge and disabled selection action, preventing offer hoarding and ensuring fair placement distribution.

### 4. Robust Security & Data Protection
- Prevention of Insecure Direct Object Reference (IDOR) attacks across all controllers.
- Automatic Anti-CSRF token verification (`[ValidateAntiForgeryToken]`).
- Atomic database transactions during interview scheduling with automatic rollback on error.
- Secure password hashing with PBKDF2-HMAC-SHA256 via ASP.NET Core Identity.

---

## Clean Slate: Built-in Administrator

The portal initializes in a clean slate with zero dummy/raw data (0 companies, 0 jobs, 0 dummy applications). Only the built-in system administrator account exists:

| Role | Email | Password | Description |
| :--- | :--- | :--- | :--- |
| **System Administrator** | `admin@jobportal.com` | `Admin@12345` | Global oversight, job moderation, and user management |

*Recruiters and students can create accounts, post jobs, and apply by using the **Register** page.*

### Resetting to Clean State
To wipe all data and reset back to only the Admin user:
```bash
dotnet run --project "Job&InternshipPortal" -- --reset-db
```

---

## Technology Stack

- **Framework:** ASP.NET Core 10.0 (C# 13.0)
- **Architecture:** Model-View-Controller (MVC) with Service Layer
- **Data Access:** Entity Framework Core 10.0 (Code-First Migrations)
- **Database:** Microsoft SQL Server (LocalDB / Express / Azure SQL)
- **Identity:** ASP.NET Core Identity with Cookie Authentication
- **Front-End:** Razor Views, Bootstrap 5.3, Bootstrap Icons, jQuery Validation

---

## Getting Started

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB (included with Visual Studio) or SQL Server Express

### Running the Application
1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd "Job&InternshipPortal"
   ```

2. **Restore dependencies & build:**
   ```bash
   dotnet build "Job&InternshipPortal/Job&InternshipPortal.csproj"
   ```

3. **Run the application:**
   ```bash
   dotnet run --project "Job&InternshipPortal"
   ```

4. **Access the portal:**
   Open your browser and navigate to:
   - `http://localhost:5000` (or the port specified in terminal output)

### Running Automated Test Suite
To execute all 17 end-to-end recruitment lifecycle and regression tests:
```bash
dotnet run --project "Job&InternshipPortal" -- --run-tests
```

