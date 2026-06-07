# Tạo Database
```sql  
-- ============================================================================

-- 1. KHỞI TẠO DATABASE

-- ============================================================================

CREATE DATABASE HRMS_DB;

GO

USE HRMS_DB;

GO

  

-- ============================================================================

-- 2. TẠO CÁC BẢNG CHA (KHÔNG CHỨA KHÓA NGOẠI HOẶC XỬ LÝ VÒNG LẶP)

-- ============================================================================

  

-- Bảng chức vụ

CREATE TABLE Positions (

Id INT IDENTITY(1,1) PRIMARY KEY,

Code VARCHAR(20) NOT NULL UNIQUE,

Name NVARCHAR(100) NOT NULL,

JobLevel INT NOT NULL

);

  

-- Bảng vai trò hệ thống

CREATE TABLE Roles (

Id INT IDENTITY(1,1) PRIMARY KEY,

Name VARCHAR(50) NOT NULL UNIQUE

);

  

-- Bảng kỳ công tháng

CREATE TABLE TimesheetPeriods (

Id INT IDENTITY(1,1) PRIMARY KEY,

Name NVARCHAR(50) NOT NULL,

StartDate DATE NOT NULL,

EndDate DATE NOT NULL,

IsLocked BIT NOT NULL DEFAULT 0 -- 0: Chưa khóa, 1: Đã khóa sổ

);

  

-- Bảng phân loại đơn từ

CREATE TABLE RequestTypes (

Id INT IDENTITY(1,1) PRIMARY KEY,

Code VARCHAR(20) NOT NULL UNIQUE,

Name NVARCHAR(50) NOT NULL

);

  

-- Bảng phòng ban (Tạm thời bỏ trống khóa ngoại HeadAccountId để tránh lỗi vòng lặp khi tạo)

CREATE TABLE Departments (

Id INT IDENTITY(1,1) PRIMARY KEY,

Code VARCHAR(20) NOT NULL UNIQUE,

Name NVARCHAR(100) NOT NULL,

HeadAccountId INT NULL

);

  

-- ============================================================================

-- 3. TẠO CÁC BẢNG CON LEVEL 1 (PHỤ THUỘC VÀO CÁC BẢNG TRÊN)

-- ============================================================================

  

-- Bảng hồ sơ cá nhân nhân viên (Cập nhật Gender và Status sang kiểu BIT)

CREATE TABLE Users (

Id INT IDENTITY(1,1) PRIMARY KEY,

EmployeeCode VARCHAR(20) NOT NULL UNIQUE,

FullName NVARCHAR(100) NOT NULL,

EmailCompany VARCHAR(100) NOT NULL UNIQUE,

Phone VARCHAR(15) NULL,

Gender BIT NULL, -- Đã sửa: 1 = Male, 0 = Female

DateOfBirth DATE NULL,

Status BIT NOT NULL DEFAULT 1, -- Đã sửa: 1 = Active (Đang làm), 0 = Inactive (Nghỉ việc)

DepartmentId INT NOT NULL,

PositionId INT NOT NULL,

CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),

CONSTRAINT FK_Users_Positions FOREIGN KEY (PositionId) REFERENCES Positions(Id)

);

  

-- Bảng tài khoản đăng nhập (Quan hệ 1-1 với Users, Cập nhật Status sang kiểu BIT)

CREATE TABLE Accounts (

Id INT IDENTITY(1,1) PRIMARY KEY,

Username VARCHAR(50) NOT NULL UNIQUE,

PasswordHash VARCHAR(255) NOT NULL,

Status BIT NOT NULL DEFAULT 1, -- Đã sửa: 1 = Active (Hoạt động), 0 = Locked (Bị khóa)

UserId INT NOT NULL UNIQUE,

CONSTRAINT FK_Accounts_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE

);

  

-- ============================================================================

-- 4. RÀNG BUỘC KHÓA NGOẠI VÒNG LẶP CHO TRƯỞNG PHÒNG BAN

-- ============================================================================

ALTER TABLE Departments

ADD CONSTRAINT FK_Departments_Accounts FOREIGN KEY (HeadAccountId) REFERENCES Accounts(Id);

  

-- ============================================================================

-- 5. TẠO CÁC BẢNG CON LEVEL 2 (PHỤ THUỘC VÀO USERS HOẶC ACCOUNTS)

-- ============================================================================

  

-- Bảng trung gian Nhiều - Nhiều: Tài khoản và Quyền (Composite PK)

CREATE TABLE AccountRoles (

AccountId INT NOT NULL,

RoleId INT NOT NULL,

CONSTRAINT PK_AccountRoles PRIMARY KEY (AccountId, RoleId),

CONSTRAINT FK_AccountRoles_Accounts FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,

CONSTRAINT FK_AccountRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE

);

  

-- Bảng nhật ký chấm công thô

CREATE TABLE AttendanceLogs (

Id INT IDENTITY(1,1) PRIMARY KEY,

CheckedAt DATETIME NOT NULL,

CheckType VARCHAR(10) NOT NULL, -- IN hoặc OUT

Source VARCHAR(20) NOT NULL DEFAULT 'Excel', -- Excel hoặc Manual

UserId INT NOT NULL,

PeriodId INT NOT NULL,

CONSTRAINT FK_AttendanceLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id),

CONSTRAINT FK_AttendanceLogs_Periods FOREIGN KEY (PeriodId) REFERENCES TimesheetPeriods(Id)

);

  

-- Bảng quản lý đơn từ và quy trình phê duyệt

CREATE TABLE Requests (

Id INT IDENTITY(1,1) PRIMARY KEY,

Title NVARCHAR(100) NOT NULL,

Reason NVARCHAR(255) NULL,

Status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Draft, Pending, Approved, Rejected

StartDate DATETIME NOT NULL,

EndDate DATETIME NOT NULL,

Value DECIMAL(5,1) NOT NULL, -- Ví dụ: Nghỉ 1.0 ngày, làm OT 3.5 giờ

CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

RequestTypeId INT NOT NULL,

CreatedByAccountId INT NOT NULL,

CurrentApproverAccountId INT NULL,

CONSTRAINT FK_Requests_RequestTypes FOREIGN KEY (RequestTypeId) REFERENCES RequestTypes(Id),

CONSTRAINT FK_Requests_Creator FOREIGN KEY (CreatedByAccountId) REFERENCES Accounts(Id),

CONSTRAINT FK_Requests_Approver FOREIGN KEY (CurrentApproverAccountId) REFERENCES Accounts(Id)

);

  

-- Bảng quản lý quỹ phép năm của nhân viên

CREATE TABLE LeaveBalances (

Id INT IDENTITY(1,1) PRIMARY KEY,

Year INT NOT NULL,

TotalDays INT NOT NULL DEFAULT 12,

UsedDays INT NOT NULL DEFAULT 0,

RemainingDays INT NOT NULL DEFAULT 12,

UserId INT NOT NULL,

CONSTRAINT FK_LeaveBalances_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE

);

  

-- Bảng hợp đồng lao động

CREATE TABLE EmploymentContracts (

Id INT IDENTITY(1,1) PRIMARY KEY,

ContractNo VARCHAR(50) NOT NULL UNIQUE,

ContractType VARCHAR(30) NOT NULL, -- Probational hoặc Official

BaseSalary DECIMAL(18,2) NOT NULL,

StartDate DATE NOT NULL,

EndDate DATE NULL,

Status VARCHAR(20) NOT NULL DEFAULT 'Active', -- Active hoặc Expired

UserId INT NOT NULL,

CONSTRAINT FK_EmploymentContracts_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE

);

  

-- Bảng kết quả tính lương (Phiếu lương chi tiết)

CREATE TABLE Payslips (

Id INT IDENTITY(1,1) PRIMARY KEY,

BaseSalary DECIMAL(18,2) NOT NULL,

OtSalary DECIMAL(18,2) NOT NULL DEFAULT 0,

Allowances DECIMAL(18,2) NOT NULL DEFAULT 0,

InsuranceDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,

TaxDeduction DECIMAL(18,2) NOT NULL DEFAULT 0,

GrossAmount DECIMAL(18,2) NOT NULL,

NetAmount DECIMAL(18,2) NOT NULL,

Status VARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, Approved, Paid

CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

UserId INT NOT NULL,

PeriodId INT NOT NULL,

CONSTRAINT FK_Payslips_Users FOREIGN KEY (UserId) REFERENCES Users(Id),

CONSTRAINT FK_Payslips_Periods FOREIGN KEY (PeriodId) REFERENCES TimesheetPeriods(Id)

);

GO

  

-- ============================================================================

-- 6. SEED DATA CƠ BẢN (DỮ LIỆU ĐỂ HỆ THỐNG CHẠY ĐƯỢC LẦN ĐẦU)

-- ============================================================================

  

-- Chèn các quyền cố định

INSERT INTO Roles (Name) VALUES ('Admin'), ('HRM'), ('HR'), ('Employee');

  

-- Chèn phân loại đơn từ cốt lõi

INSERT INTO RequestTypes (Code, Name) VALUES

('LEAVE', N'Đơn xin nghỉ phép'),

('OT', N'Đơn làm thêm giờ');

GO 
```
# Thêm dữ liệu mẫu
```sql  
USE HRMS_DB;

GO

  

-- Xóa dữ liệu cũ nếu có (theo thứ tự từ con đến cha để tránh lỗi ràng buộc)

TRUNCATE TABLE AccountRoles;

DELETE FROM Payslips;

DELETE FROM AttendanceLogs;

DELETE FROM Requests;

DELETE FROM LeaveBalances;

DELETE FROM EmploymentContracts;

ALTER TABLE Departments DROP CONSTRAINT FK_Departments_Accounts; -- Tạm gỡ để xóa dữ liệu

DELETE FROM Accounts;

DELETE FROM Users;

DELETE FROM Departments;

DELETE FROM Positions;

DELETE FROM TimesheetPeriods;

DELETE FROM RequestTypes;

DELETE FROM Roles;

  

-- Nạp lại các quyền và loại đơn cơ bản (nếu chưa có)

INSERT INTO Roles (Name) VALUES ('Admin'), ('HRM'), ('HR'), ('Employee');

INSERT INTO RequestTypes (Code, Name) VALUES ('LEAVE', N'Đơn xin nghỉ phép'), ('OT', N'Đơn làm thêm giờ');

  

-- ============================================================================

-- STEP 1: CHÈN DỮ LIỆU BẢNG CHA ĐỘC LẬP

-- ============================================================================

  

-- 1. Chèn Chức vụ (Positions)

INSERT INTO Positions (Code, Name, JobLevel) VALUES

('DIR', N'Giám đốc', 5),

('HRM', N'Trưởng phòng nhân sự', 4),

('SENIOR', N'Lập trình viên Senior', 3),

('INTERN', N'Thực tập sinh', 1);

  

-- 2. Chèn Kỳ công (TimesheetPeriods)

-- Kỳ tháng 5/2026 đã khóa sổ (để làm mẫu lương lịch sử)

INSERT INTO TimesheetPeriods (Name, StartDate, EndDate, IsLocked)

VALUES (N'Kỳ công Tháng 05/2026', '2026-05-01', '2026-05-31', 1);

  

-- Kỳ tháng 6/2026 hiện tại đang mở

INSERT INTO TimesheetPeriods (Name, StartDate, EndDate, IsLocked)

VALUES (N'Kỳ công Tháng 06/2026', '2026-06-01', '2026-06-30', 0);

  

-- 3. Chèn Phòng ban (Departments) - Chưa điền Trưởng phòng (HeadAccountId = NULL)

INSERT INTO Departments (Code, Name, HeadAccountId) VALUES

('BOD', N'Ban Giám đốc', NULL),

('HR', N'Phòng Nhân sự', NULL),

('IT', N'Phòng Công nghệ thông tin', NULL);

  

-- ============================================================================

-- STEP 2: CHÈN HỒ SƠ NHÂN VIÊN (USERS) & TÀI KHOẢN (ACCOUNTS)

-- ============================================================================

  

-- Bật IDENTITY_INSERT nếu cần chèn cứng ID để dễ map khóa ngoại, hoặc dùng biến lưu ID.

-- Ở đây anh dùng cấu trúc lồng trực tiếp để lấy ID tự động hoặc chèn theo thứ tự an toàn:

  

-- Nhân sự 1: Nguyễn Văn An (Giám đốc - Ban Giám đốc)

INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)

VALUES ('NV001', N'Nguyễn Văn An', 'an.nguyen@company.com', '0912345678', 1, '1985-05-12', 1,

(SELECT Id FROM Departments WHERE Code='BOD'), (SELECT Id FROM Positions WHERE Code='DIR'));

  

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)

VALUES ('an.nguyen', '$2a$11$EvX7J1lWJbO8vRE11xQvO.7Mv.M3pI/tS.XG6B6Z3Z2mGx6eX8X2W', 1, @@IDENTITY); -- Mật khẩu mẫu: 'Password123'

DECLARE @Id_Acc_An INT = @@IDENTITY;

  

-- Nhân sự 2: Trần Thị Bình (Trưởng phòng HR - Phòng Nhân sự)

INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)

VALUES ('NV002', N'Trần Thị Bình', 'binh.tran@company.com', '0987654321', 0, '1990-08-20', 1,

(SELECT Id FROM Departments WHERE Code='HR'), (SELECT Id FROM Positions WHERE Code='HRM'));

  

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)

VALUES ('binh.tran', '$2a$11$EvX7J1lWJbO8vRE11xQvO.7Mv.M3pI/tS.XG6B6Z3Z2mGx6eX8X2W', 1, @@IDENTITY);

DECLARE @Id_Acc_Binh INT = @@IDENTITY;

  

-- Nhân sự 3: Lê Hoàng Long (Senior Dev - Phòng IT)

INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)

VALUES ('NV003', N'Lê Hoàng Long', 'long.le@company.com', '0901234567', 1, '1995-11-05', 1,

(SELECT Id FROM Departments WHERE Code='IT'), (SELECT Id FROM Positions WHERE Code='SENIOR'));

  

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)

VALUES ('long.le', '$2a$11$EvX7J1lWJbO8vRE11xQvO.7Mv.M3pI/tS.XG6B6Z3Z2mGx6eX8X2W', 1, @@IDENTITY);

DECLARE @Id_Acc_Long INT = @@IDENTITY;

  

-- Nhân sự 4: Phạm Minh Tuấn (Intern Dev - Phòng IT)

INSERT INTO Users (EmployeeCode, FullName, EmailCompany, Phone, Gender, DateOfBirth, Status, DepartmentId, PositionId)

VALUES ('NV004', N'Phạm Minh Tuấn', 'tuan.pham@company.com', '0934567890', 1, '2004-02-15', 1,

(SELECT Id FROM Departments WHERE Code='IT'), (SELECT Id FROM Positions WHERE Code='INTERN'));

  

INSERT INTO Accounts (Username, PasswordHash, Status, UserId)

VALUES ('tuan.pham', '$2a$11$EvX7J1lWJbO8vRE11xQvO.7Mv.M3pI/tS.XG6B6Z3Z2mGx6eX8X2W', 1, @@IDENTITY);

DECLARE @Id_Acc_Tuan INT = @@IDENTITY;

  

-- ============================================================================

-- STEP 3: CẬP NHẬT TRƯỞNG PHÒNG VÀ PHÂN QUYỀN (ACCOUNT ROLES)

-- ============================================================================

  

-- Cập nhật Trưởng phòng ban (Giải quyết vòng lặp)

ALTER TABLE Departments ADD CONSTRAINT FK_Departments_Accounts FOREIGN KEY (HeadAccountId) REFERENCES Accounts(Id);

UPDATE Departments SET HeadAccountId = @Id_Acc_An WHERE Code='BOD';

UPDATE Departments SET HeadAccountId = @Id_Acc_Binh WHERE Code='HR';

UPDATE Departments SET HeadAccountId = @Id_Acc_Long WHERE Code='IT'; -- Senior tạm làm trưởng phòng IT

  

-- Phân quyền cho các tài khoản (AccountRoles)

INSERT INTO AccountRoles (AccountId, RoleId) VALUES

(@Id_Acc_An, (SELECT Id FROM Roles WHERE Name='Admin')),

(@Id_Acc_An, (SELECT Id FROM Roles WHERE Name='HRM')),

(@Id_Acc_Binh, (SELECT Id FROM Roles WHERE Name='HR')),

(@Id_Acc_Long, (SELECT Id FROM Roles WHERE Name='Employee')),

(@Id_Acc_Tuan, (SELECT Id FROM Roles WHERE Name='Employee'));

  

-- ============================================================================

-- STEP 4: CHÈN CÁC DỮ LIỆU NGHIỆP VỤ ĐI KÈM CỦA NHÂN VIÊN

-- ============================================================================

  

-- 1. Quỹ Phép Năm 2026 (LeaveBalances)

INSERT INTO LeaveBalances (Year, TotalDays, UsedDays, RemainingDays, UserId) VALUES

(2026, 12, 0, 12, (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_An)),

(2026, 12, 1, 11, (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Binh)),

(2026, 12, 2, 10, (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Long)),

(2026, 12, 0, 12, (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Tuan));

  

-- 2. Hợp đồng lao động (EmploymentContracts)

INSERT INTO EmploymentContracts (ContractNo, ContractType, BaseSalary, StartDate, EndDate, Status, UserId) VALUES

('HD-001/BOD', 'Official', 50000000.00, '2026-01-01', NULL, 'Active', (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_An)),

('HD-002/HR', 'Official', 25000000.00, '2026-01-01', '2028-12-31', 'Active', (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Binh)),

('HD-003/IT', 'Official', 35000000.00, '2026-02-01', '2027-01-31', 'Active', (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Long)),

('HD-004/IT', 'Probational', 5000000.00, '2026-05-01', '2026-07-31', 'Active', (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Tuan));

  

-- ============================================================================

-- STEP 5: MÔ PHỎNG LUỒNG NGHIỆP VỤ (ATTENDANCE, REQUEST, PAYROLL)

-- ============================================================================

  

-- 1. Nhật ký quẹt thẻ mẫu ngày 01/06/2026 cho Intern Tuấn và Senior Long

DECLARE @Period_Jun INT = (SELECT Id FROM TimesheetPeriods WHERE Name=N'Kỳ công Tháng 06/2026');

DECLARE @User_Long INT = (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Long);

DECLARE @User_Tuan INT = (SELECT UserId FROM Accounts WHERE Id=@Id_Acc_Tuan);

  

INSERT INTO AttendanceLogs (CheckedAt, CheckType, Source, UserId, PeriodId) VALUES

('2026-06-01 07:55:00', 'IN', 'Excel', @User_Long, @Period_Jun),

('2026-06-01 17:35:00', 'OUT', 'Excel', @User_Long, @Period_Jun),

('2026-06-01 08:15:00', 'IN', 'Excel', @User_Tuan, @Period_Jun), -- Đi muộn 15 phút

('2026-06-01 17:30:00', 'OUT', 'Excel', @User_Tuan, @Period_Jun);

  

-- 2. Đơn từ mẫu (Requests)

-- Đơn 1: Intern Tuấn xin Nghỉ phép ngày 05/06 -> Trạng thái: Chờ duyệt (Pending), Người duyệt tiếp theo là chị Bình HR

INSERT INTO Requests (Title, Reason, Status, StartDate, EndDate, Value, RequestTypeId, CreatedByAccountId, CurrentApproverAccountId)

VALUES (N'Đơn xin nghỉ phép giải quyết việc gia đình', N'Em xin nghỉ 1 ngày đưa mẹ đi khám bệnh.', 'Pending',

'2026-06-05 08:00:00', '2026-06-05 17:30:00', 1.0, (SELECT Id FROM RequestTypes WHERE Code='LEAVE'), @Id_Acc_Tuan, @Id_Acc_Binh);

  

-- Đơn 2: Senior Long làm thêm giờ OT ngày 02/06 -> Trạng thái: Đã duyệt (Approved) bởi Giám đốc An

INSERT INTO Requests (Title, Reason, Status, StartDate, EndDate, Value, RequestTypeId, CreatedByAccountId, CurrentApproverAccountId)

VALUES (N'Đơn xin xác nhận làm OT dự án hệ thống', N'Fix bug nghiêm trọng cho hệ thống HRMS.', 'Approved',

'2026-06-02 18:00:00', '2026-06-02 21:00:00', 3.0, (SELECT Id FROM RequestTypes WHERE Code='OT'), @Id_Acc_Long, @Id_Acc_An);

  

-- 3. Phiếu lương mẫu lịch sử (Payslips) - Gắn với kỳ tháng 05 đã khóa

DECLARE @Period_May INT = (SELECT Id FROM TimesheetPeriods WHERE Name=N'Kỳ công Tháng 05/2026');

INSERT INTO Payslips (BaseSalary, OtSalary, Allowances, InsuranceDeduction, TaxDeduction, GrossAmount, NetAmount, Status, UserId, PeriodId)

VALUES

(35000000.00, 1500000.00, 500000.00, 3675000.00, 1200000.00, 37000000.00, 32125000.00, 'Paid', @User_Long, @Period_May),

(5000000.00, 0.00, 200000.00, 525000.00, 0.00, 5200000.00, 4675000.00, 'Paid', @User_Tuan, @Period_May);

GO  
```