# HRMS SYSTEM DATABASE SCHEMA & SEED DATA (UPDATED V2)

---

## 1. DDL SCRIPT - TẠO TOÀN BỘ CƠ SỞ DỮ LIỆU `HRMS_DB`

```sql
-- ============================================================================
-- 1. KHỞI TẠO DATABASE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HRMS_DB')
BEGIN
    CREATE DATABASE HRMS_DB;
END
GO

USE HRMS_DB;
GO

-- ============================================================================
-- 2. TẠO CÁC BẢNG ĐỘC LẬP / BẢNG CHA
-- ============================================================================

-- Bảng ca làm việc (Shifts)
CREATE TABLE Shifts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    BreakStart TIME NOT NULL,
    BreakEnd TIME NOT NULL,
    LateToleranceMinute INT NOT NULL DEFAULT 0,
    EarlyCheckInMinute INT NOT NULL DEFAULT 0,
    LateCheckOutMinute INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Bảng chức vụ (Positions)
CREATE TABLE Positions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    JobLevel INT NOT NULL,
    DefaultShiftId INT NULL,
    CONSTRAINT FK_Positions_Shifts FOREIGN KEY (DefaultShiftId) REFERENCES Shifts(Id)
);

-- Bảng vai trò hệ thống (Roles)
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE
);

-- Bảng kỳ công tháng (TimesheetPeriods)
CREATE TABLE TimesheetPeriods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    IsLocked BIT NOT NULL DEFAULT 0
);

-- Bảng phân loại đơn từ (RequestTypes)
CREATE TABLE RequestTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(50) NOT NULL
);

-- Bảng phòng ban (Departments)
CREATE TABLE Departments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    HeadAccountId INT NULL
);

-- ============================================================================
-- 3. TẠO CÁC BẢNG NHÂN SỰ & TÀI KHOẢN
-- ============================================================================

-- Bảng hồ sơ nhân viên (Users)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode VARCHAR(20) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    EmailCompany VARCHAR(100) NOT NULL UNIQUE,
    Phone VARCHAR(15) NULL,
    Gender BIT NULL, -- 1: Male, 0: Female
    DateOfBirth DATE NULL,
    Status BIT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
    DepartmentId INT NOT NULL,
    PositionId INT NOT NULL,
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT FK_Users_Positions FOREIGN KEY (PositionId) REFERENCES Positions(Id)
);

-- Bảng tài khoản đăng nhập (Accounts)
CREATE TABLE Accounts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Status BIT NOT NULL DEFAULT 1, -- 1: Active, 0: Locked
    UserId INT NOT NULL UNIQUE,
    CONSTRAINT FK_Accounts_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Ràng buộc vòng lặp cho Trưởng phòng ban
ALTER TABLE Departments
ADD CONSTRAINT FK_Departments_Accounts FOREIGN KEY (HeadAccountId) REFERENCES Accounts(Id);

-- Bảng trung gian Vai trò tài khoản (AccountRoles)
CREATE TABLE AccountRoles (
    AccountId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_AccountRoles PRIMARY KEY (AccountId, RoleId),
    CONSTRAINT FK_AccountRoles_Accounts FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AccountRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

-- ============================================================================
-- 4. TẠO CÁC BẢNG CHẤM CÔNG & NGHỈ PHÉP
-- ============================================================================

-- Bảng phân ca làm việc tạm thời (ShiftAssignments)
CREATE TABLE ShiftAssignments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    ShiftId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    AssignedBy INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ShiftAssignments_Users FOREIGN KEY (EmployeeId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ShiftAssignments_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ShiftAssignments_Accounts FOREIGN KEY (AssignedBy) REFERENCES Accounts(Id)
);

-- Bảng điểm danh hàng ngày (Attendance v2)
CREATE TABLE Attendance (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    AttendanceDate DATE NOT NULL,
    CheckInTime TIME NULL,
    CheckOutTime TIME NULL,
    PeriodId INT NULL,
    CONSTRAINT FK_Attendance_Users FOREIGN KEY (EmployeeId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Attendance_Periods FOREIGN KEY (PeriodId) REFERENCES TimesheetPeriods(Id),
    CONSTRAINT UQ_Attendance_Employee_Date UNIQUE (EmployeeId, AttendanceDate)
);

-- Bảng quản lý đơn từ (Requests)
CREATE TABLE Requests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Reason NVARCHAR(255) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Value DECIMAL(5,1) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    RequestTypeId INT NOT NULL,
    CreatedByAccountId INT NOT NULL,
    CurrentApproverAccountId INT NULL,
    CONSTRAINT FK_Requests_RequestTypes FOREIGN KEY (RequestTypeId) REFERENCES RequestTypes(Id),
    CONSTRAINT FK_Requests_Creator FOREIGN KEY (CreatedByAccountId) REFERENCES Accounts(Id),
    CONSTRAINT FK_Requests_Approver FOREIGN KEY (CurrentApproverAccountId) REFERENCES Accounts(Id)
);

-- Bảng quỹ phép năm (LeaveBalances)
CREATE TABLE LeaveBalances (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Year INT NOT NULL,
    TotalDays INT NOT NULL DEFAULT 12,
    UsedDays INT NOT NULL DEFAULT 0,
    RemainingDays INT NOT NULL DEFAULT 12,
    UserId INT NOT NULL,
    CONSTRAINT FK_LeaveBalances_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Bảng hợp đồng lao động (EmploymentContracts)
CREATE TABLE EmploymentContracts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ContractNo VARCHAR(50) NOT NULL UNIQUE,
    ContractType VARCHAR(30) NOT NULL,
    BaseSalary DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    UserId INT NOT NULL,
    CONSTRAINT FK_EmploymentContracts_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Bảng phiếu lương (Payslips)
CREATE TABLE Payslips (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BaseSalary DECIMAL(18,2) NOT NULL,
    OtSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
    Allowances DECIMAL(18,2) NOT NULL DEFAULT 0,
    InsuranceDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,
    GrossAmount DECIMAL(18,2) NOT NULL,
    NetAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Draft',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UserId INT NOT NULL,
    PeriodId INT NOT NULL,
    CONSTRAINT FK_Payslips_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Payslips_Periods FOREIGN KEY (PeriodId) REFERENCES TimesheetPeriods(Id)
);
GO
```

---

## 2. SEED DATA SCRIPT - NẠP DỮ LIỆU MẪU MỚI KHỞI TẠO HỆ THỐNG

```sql
USE HRMS_DB;
GO

-- Xóa dữ liệu cũ (nếu reset database)
TRUNCATE TABLE AccountRoles;
DELETE FROM Payslips;
DELETE FROM Attendance;
DELETE FROM ShiftAssignments;
DELETE FROM Requests;
DELETE FROM LeaveBalances;
DELETE FROM EmploymentContracts;

ALTER TABLE Departments DROP CONSTRAINT IF EXISTS FK_Departments_Accounts;

DELETE FROM Accounts;
DELETE FROM Users;
DELETE FROM Departments;
DELETE FROM Positions;
DELETE FROM Shifts;
DELETE FROM TimesheetPeriods;
DELETE FROM RequestTypes;
DELETE FROM Roles;

-- 1. Chèn Roles & RequestTypes
INSERT INTO Roles (Name) VALUES ('Admin'), ('HRM'), ('HR'), ('Employee');
INSERT INTO RequestTypes (Code, Name) VALUES ('LEAVE', N'Đơn xin nghỉ phép'), ('OT', N'Đơn làm thêm giờ');

-- 2. Chèn Ca làm việc mẫu (Shifts)
INSERT INTO Shifts (Name, StartTime, EndTime, BreakStart, BreakEnd, LateToleranceMinute, EarlyCheckInMinute, LateCheckOutMinute, IsActive)
VALUES (N'Ca Hành Chính Standard', '08:00:00', '17:30:00', '12:00:00', '13:30:00', 15, 30, 30, 1);
DECLARE @Shift_Admin INT = @@IDENTITY;

INSERT INTO Shifts (Name, StartTime, EndTime, BreakStart, BreakEnd, LateToleranceMinute, EarlyCheckInMinute, LateCheckOutMinute, IsActive)
VALUES (N'Ca Sáng', '07:30:00', '12:00:00', '10:00:00', '10:15:00', 10, 15, 15, 1);

-- 3. Chèn Chức vụ (Positions)
INSERT INTO Positions (Code, Name, JobLevel, DefaultShiftId) VALUES
('DIR', N'Giám đốc', 5, @Shift_Admin),
('HRM', N'Trưởng phòng nhân sự', 4, @Shift_Admin),
('SENIOR', N'Lập trình viên Senior', 3, @Shift_Admin),
('INTERN', N'Thực tập sinh', 1, @Shift_Admin);

-- 4. Chèn Kỳ công (TimesheetPeriods)
INSERT INTO TimesheetPeriods (Name, StartDate, EndDate, IsLocked) VALUES
(N'Kỳ công Tháng 05/2026', '2026-05-01', '2026-05-31', 1),
(N'Kỳ công Tháng 06/2026', '2026-06-01', '2026-06-30', 0),
(N'Kỳ công Tháng 07/2026', '2026-07-01', '2026-07-31', 0);

DECLARE @Period_Jun INT = (SELECT Id FROM TimesheetPeriods WHERE Name = N'Kỳ công Tháng 06/2026');
DECLARE @Period_May INT = (SELECT Id FROM TimesheetPeriods WHERE Name = N'Kỳ công Tháng 05/2026');

-- 5. Chèn Phòng ban (Departments)
INSERT INTO Departments (Code, Name, HeadAccountId) VALUES
('BOD', N'Ban Giám đốc', NULL),
('HR', N'Phòng Nhân sự', NULL),
('IT', N'Phòng Công nghệ thông tin', NULL);

-- 6. Chèn Nhân viên & Tài khoản
-- NV001 - Nguyễn Văn An (Giám đốc)
INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)
VALUES ('NV001', N'Nguyễn Văn An', 'an.nguyen@company.com', '0912345678', 1, '1985-05-12', 1,
(SELECT Id FROM Departments WHERE Code='BOD'), (SELECT Id FROM Positions WHERE Code='DIR'));

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)
VALUES ('an.nguyen', '$2a$11$dvhu.R6ZlciiNhIPMlYP.uaOoUYyDIMWAl7oyHkQwDVmpsiZbEbTu', 1, @@IDENTITY);
DECLARE @Acc_An INT = @@IDENTITY;

-- NV002 - Trần Thị Bình (Trưởng phòng HR)
INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)
VALUES ('NV002', N'Trần Thị Bình', 'binh.tran@company.com', '0987654321', 0, '1990-08-20', 1,
(SELECT Id FROM Departments WHERE Code='HR'), (SELECT Id FROM Positions WHERE Code='HRM'));

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)
VALUES ('binh.tran', '$2a$11$dvhu.R6ZlciiNhIPMlYP.uaOoUYyDIMWAl7oyHkQwDVmpsiZbEbTu', 1, @@IDENTITY);
DECLARE @Acc_Binh INT = @@IDENTITY;

-- NV003 - Lê Hoàng Long (Senior Dev)
INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)
VALUES ('NV003', N'Lê Hoàng Long', 'long.le@company.com', '0901234567', 1, '1995-11-05', 1,
(SELECT Id FROM Departments WHERE Code='IT'), (SELECT Id FROM Positions WHERE Code='SENIOR'));

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)
VALUES ('long.le', '$2a$11$dvhu.R6ZlciiNhIPMlYP.uaOoUYyDIMWAl7oyHkQwDVmpsiZbEbTu', 1, @@IDENTITY);
DECLARE @Acc_Long INT = @@IDENTITY;

-- NV004 - Phạm Minh Tuấn (Intern Dev)
INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)
VALUES ('NV004', N'Phạm Minh Tuấn', 'tuan.pham@company.com', '0934567890', 1, '2004-02-15', 1,
(SELECT Id FROM Departments WHERE Code='IT'), (SELECT Id FROM Positions WHERE Code='INTERN'));

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)
VALUES ('tuan.pham', '$2a$11$dvhu.R6ZlciiNhIPMlYP.uaOoUYyDIMWAl7oyHkQwDVmpsiZbEbTu', 1, @@IDENTITY);
DECLARE @Acc_Tuan INT = @@IDENTITY;

-- 7. Ràng buộc Trưởng phòng & Quyền hệ thống
ALTER TABLE Departments ADD CONSTRAINT FK_Departments_Accounts FOREIGN KEY (HeadAccountId) REFERENCES Accounts(Id);
UPDATE Departments SET HeadAccountId = @Acc_An WHERE Code='BOD';
UPDATE Departments SET HeadAccountId = @Acc_Binh WHERE Code='HR';
UPDATE Departments SET HeadAccountId = @Acc_Long WHERE Code='IT';

INSERT INTO AccountRoles (AccountId, RoleId) VALUES
(@Acc_An, (SELECT Id FROM Roles WHERE Name='Admin')),
(@Acc_An, (SELECT Id FROM Roles WHERE Name='HRM')),
(@Acc_Binh, (SELECT Id FROM Roles WHERE Name='HR')),
(@Acc_Long, (SELECT Id FROM Roles WHERE Name='Employee')),
(@Acc_Tuan, (SELECT Id FROM Roles WHERE Name='Employee'));

-- 8. Quỹ Phép & Hợp đồng
INSERT INTO LeaveBalances (Year, TotalDays, UsedDays, RemainingDays, UserId) VALUES
(2026, 12, 0, 12, (SELECT UserId FROM Accounts WHERE Id=@Acc_An)),
(2026, 12, 1, 11, (SELECT UserId FROM Accounts WHERE Id=@Acc_Binh)),
(2026, 12, 2, 10, (SELECT UserId FROM Accounts WHERE Id=@Acc_Long)),
(2026, 12, 0, 12, (SELECT UserId FROM Accounts WHERE Id=@Acc_Tuan));

INSERT INTO EmploymentContracts (ContractNo, ContractType, BaseSalary, StartDate, EndDate, Status, UserId) VALUES
('HD-001/BOD', 'Official', 50000000.00, '2026-01-01', NULL, 'Active', (SELECT UserId FROM Accounts WHERE Id=@Acc_An)),
('HD-002/HR', 'Official', 25000000.00, '2026-01-01', '2028-12-31', 'Active', (SELECT UserId FROM Accounts WHERE Id=@Acc_Binh)),
('HD-003/IT', 'Official', 35000000.00, '2026-02-01', '2027-01-31', 'Active', (SELECT UserId FROM Accounts WHERE Id=@Acc_Long)),
('HD-004/IT', 'Probational', 5000000.00, '2026-05-01', '2026-07-31', 'Active', (SELECT UserId FROM Accounts WHERE Id=@Acc_Tuan));

-- 9. Dữ liệu Chấm công mẫu (Attendance v2)
DECLARE @User_Long INT = (SELECT UserId FROM Accounts WHERE Id=@Acc_Long);
DECLARE @User_Tuan INT = (SELECT UserId FROM Accounts WHERE Id=@Acc_Tuan);

INSERT INTO Attendance (EmployeeId, AttendanceDate, CheckInTime, CheckOutTime, PeriodId) VALUES
(@User_Long, '2026-06-01', '07:55:00', '17:35:00', @Period_Jun),
(@User_Tuan, '2026-06-01', '08:15:00', '17:30:00', @Period_Jun),
(@User_Long, '2026-06-02', '08:00:00', '17:30:00', @Period_Jun);

-- 10. Phiếu lương lịch sử (Payslips)
INSERT INTO Payslips (BaseSalary, OtSalary, Allowances, InsuranceDeduction, TaxDeduction, GrossAmount, NetAmount, Status, UserId, PeriodId) VALUES
(35000000.00, 1500000.00, 500000.00, 3675000.00, 1200000.00, 37000000.00, 32125000.00, 'Paid', @User_Long, @Period_May),
(5000000.00, 0.00, 200000.00, 525000.00, 0.00, 5200000.00, 4675000.00, 'Paid', @User_Tuan, @Period_May);

GO
```
