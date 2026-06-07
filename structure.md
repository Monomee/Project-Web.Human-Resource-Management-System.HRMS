# HRMS SYSTEM ARCHITECTURE & REQUIREMENT CONTEXT (MVP VERSION)

## 1. Project Overview
- **System**: Human Resource Management System (HRMS) for small companies (~40-50 employees).
- **Tech Stack**: .NET 8/9, Blazor Web App (Interactive Server Mode per page/component).
- **ORM**: Entity Framework Core (EF Core) SQL Server.
- **Architecture**: Clean Architecture (Onion Architecture).

## 2. Core Architecture Rules (STRICT)
- **1. Core Layer (`HRMS.Domain`)**: Contains pure POCO Entities and Enums. NO dependencies on EF Core, Web, or external packages.
- **2. Application Layer (`HRMS.Application`)**: Contains Business Logic Services, Interfaces, and DTOs. Depends ONLY on `Domain`.
- **3. Infrastructure Layer (`HRMS.Infrastructure`)**: Contains `ApplicationDbContext`, Repositories, Data Access implementations, and external services (Excel parser). Depends on `Application` and `Domain`.
- **4. Presentation Layer (`HRMS.WebUI`)**: Blazor Web App. Contains UI Components and Pages. Depends on `Application` and `Infrastructure` (via Dependency Injection configuration).
- **Dependency Injection Rule**: All services must be registered via `DependencyInjection.cs` extension methods in their respective layers, then called in `Program.cs` of `HRMS.WebUI`.

## 3. Database Schema (13 Final Tables)
- **Org & Identity**: `Positions` (Id, Code, Name, JobLevel), `Roles` (Id, Name), `Departments` (Id, Code, Name, HeadAccountId), `Users` (Id, EmployeeCode, FullName, EmailCompany, Phone, Gender (BIT), DateOfBirth, Status (BIT), DepartmentId, PositionId), `Accounts` (Id, Username, PasswordHash, Status (BIT), UserId), `AccountRoles` (AccountId, RoleId - Composite PK).
- **Attendance & Requests**: `TimesheetPeriods` (Id, Name, StartDate, EndDate, IsLocked (BIT)), `AttendanceLogs` (Id, CheckedAt, CheckType, Source, UserId, PeriodId), `RequestTypes` (Id, Code, Name), `Requests` (Id, Title, Reason, Status, StartDate, EndDate, Value, CreatedAt, RequestTypeId, CreatedByAccountId, CurrentApproverAccountId), `LeaveBalances` (Id, Year, TotalDays, UsedDays, RemainingDays, UserId).
- **Payroll & Contracts**: `EmploymentContracts` (Id, ContractNo, ContractType, BaseSalary, StartDate, EndDate, Status, UserId), `Payslips` (Id, BaseSalary, OtSalary, Allowances, InsuranceDeduction, TaxDeduction, GrossAmount, NetAmount, Status, CreatedAt, UserId, PeriodId).

## 4. Blazor Render Mode Guideline
- Default state: Static SSR for public or lightweight views.
- Active state: Use `@rendermode InteractiveServer` at the top of internal admin/HR pages to handle real-time events, data filtering, and modal dialogs via SignalR.
- NEVER inject `ApplicationDbContext` directly into Razor Components. Always go through Application Services.