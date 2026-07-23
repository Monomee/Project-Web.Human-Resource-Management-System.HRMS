# TÀI LIỆU PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG HRMS (HUMAN RESOURCE MANAGEMENT SYSTEM)
> **Phương pháp thiết kế**: Hassan Gomaa Design Method (COMET - Concurrent Object-Oriented Software Design)  
> **Phong cách**: Thiết kế khái niệm trước khi lập trình (Design-Before-Code), tập trung vào phân loại đối tượng (Stereotypes: `<<boundary>>`, `<<control>>`, `<<entity>>`) và rõ ràng luồng dữ liệu.

---

## MỤC LỤC
1. [Tổng quan Phương pháp Thiết kế Hassan Gomaa](#1-tổng-quan-phương-pháp-thiết-kế-hassan-gomaa)
2. [Sơ đồ Bối cảnh Hệ thống (System Context Diagram)](#2-sơ-đồ-bối-cảnh-hệ-thống-system-context-diagram)
3. [Sơ đồ Lớp các Tính năng Chính (Class Diagrams)](#3-sơ-đồ-lớp-các-tính-năng-chính-class-diagrams)
   - [Feature 1: Quản lý Xác thực & Tài khoản Nhân sự](#feature-1-quản-lý-xác-thực--tài-khoản-nhân-sự)
   - [Feature 2: Quản lý Chấm công & Ca làm việc](#feature-2-quản-lý-chấm-công--ca-làm-việc)
   - [Feature 3: Quy trình Gửi & Phê duyệt Đơn từ](#feature-3-quy-trình-gửi--phê-duyệt-đơn-từ)
   - [Feature 4: Quản lý Hợp đồng Lao động](#feature-4-quản-lý-hợp-đồng-lao-động)
   - [Feature 5: Quản lý & Tính Lương Hàng tháng](#feature-5-quản-lý--tính-lương-hàng-tháng)
4. [Sơ đồ Tuần tự các Tính năng Chính (Sequence Diagrams)](#4-sơ-đồ-tuần-tự-các-tính-năng-chính-sequence-diagrams)
   - [Sequence 1: Luồng Đăng nhập & Xác thực](#sequence-1-luồng-đăng-nhập--xác-thực)
   - [Sequence 2: Luồng Chấm công Hàng ngày & Nhập dữ liệu Excel](#sequence-2-luồng-chấm-công-hàng-ngày--nhập-dữ-liệu-excel)
   - [Sequence 3: Luồng Gửi & Duyệt Đơn (Nghỉ phép / OT / Khiếu nại công)](#sequence-3-luồng-gửi--duyệt-đơn-nghỉ-phép--ot--khiếu-nại-công)
   - [Sequence 4: Luồng Tạo & Phê duyệt Hợp đồng Lao động](#sequence-4-luồng-tạo--phê-duyệt-hợp-đồng-lao-động)
   - [Sequence 5: Luồng Tính Lương & Xuất Phiếu Lương Hàng tháng](#sequence-5-luồng-tính-lương--xuất-phiếu-lương-hàng-tháng)

---

## 1. TỔNG QUAN PHƯƠNG PHÁP THIẾT KẾ HASSAN GOMAA

Phương pháp **Hassan Gomaa** trong thiết kế hướng đối tượng chú trọng phân tách hệ thống thành các nhóm đối tượng có vai trò rõ ràng (Stereotypes):
1. **`<<boundary>>` (Đối tượng Biên / Giao diện)**: Đóng vai trò trung gian tương tác với tác nhân bên ngoài (Người dùng, Hệ thống ngoài). Nhận yêu cầu và hiển thị kết quả.
2. **`<<control>>` (Đối tượng Điều khiển / Điều phối)**: Quản lý luồng nghiệp vụ, thực thi logic trung gian, phối hợp tương tác giữa đối tượng giao diện và các thực thể dữ liệu.
3. **`<<entity>>` (Đối tượng Thực thể / Dữ liệu)**: Đại diện cho các khái niệm nghiệp vụ lõi và lưu trữ trạng thái của hệ thống (User, Attendance, Request, Contract, Payslip,...).

---

## 2. SƠ ĐỒ BỐI CẢNH HỆ THỐNG (SYSTEM CONTEXT DIAGRAM)

Sơ đồ bối cảnh thể hiện ranh giới của hệ thống **HRMS System** cùng với các tác nhân (Actors) và hệ thống bên ngoài tương tác qua lại.

```mermaid
graph TB
    subgraph External_Actors ["Tác nhân bên ngoài (External Actors)"]
        NV["👨‍💼 Nhân viên (Employee)"]
        QL["👨‍💻 Quản lý trực tiếp (Manager / Approver)"]
        HR["👩‍💼 Quản trị HR (HR Admin / Manager)"]
    end

    subgraph System_Boundary ["Ranh giới Hệ thống (System Boundary)"]
        HRMS["🏢 HỆ THỐNG QUẢN TRỊ NHÂN SỰ (HRMS CORE SYSTEM)"]
    end

    subgraph External_Systems ["Hệ thống & Tệp tin bên ngoài (External Systems)"]
        Excel["📊 Tệp dữ liệu chấm công Excel / Thiết bị chấm công"]
        Notif["🔔 Hệ thống Thông báo (Notification Service)"]
        DB[(🗄️ Cơ sở dữ liệu Postgres / SQL Server)]
    end

    %% Tương tác Nhân viên
    NV -->|"1. Đăng nhập / Đổi mật khẩu"| HRMS
    NV -->|"2. Thực hiện Check-in / Check-out hàng ngày"| HRMS
    NV -->|"3. Gửi Đơn nghỉ phép / OT / Khiếu nại công"| HRMS
    NV -->|"4. Xem bảng công & Phiếu lương cá nhân"| HRMS

    %% Tương tác Quản lý
    QL -->|"5. Xem danh sách đơn chờ duyệt"| HRMS
    QL -->|"6. Phê duyệt / Từ chối đơn của cấp dưới"| HRMS

    %% Tương tác HR Admin
    HR -->|"7. Quản lý Hồ sơ Nhân viên & Phòng ban"| HRMS
    HR -->|"8. Tạo & Duyệt Hợp đồng Lao động"| HRMS
    HR -->|"9. Nhập file chấm công & Khóa kỳ công"| HRMS
    HR -->|"10. Khởi chạy tính lương hàng tháng"| HRMS

    %% Tương tác với Hệ thống ngoài
    Excel -->|"Tải lên dữ liệu chấm công hàng loạt"| HRMS
    HRMS -->|"Gửi email / thông báo trạng thái đơn"| Notif
    HRMS <-->|"Lưu trữ & Truy vấn dữ liệu thực thể"| DB
```

---

## 3. SƠ ĐỒ LỚP CÁC TÍNH NĂNG CHÍNH (CLASS DIAGRAMS)

Các sơ đồ lớp được thiết kế theo phong cách Hassan Gomaa, chia rõ thành 3 tầng: `<<boundary>>`, `<<control>>`, và `<<entity>>`.

### Feature 1: Quản lý Xác thực & Tài khoản Nhân sự

Quản lý đăng nhập, phân quyền người dùng (`Admin`, `HR`, `Manager`, `Employee`), thông tin phòng ban và chức danh.

```mermaid
classDiagram
    class LoginView {
        <<boundary>>
        +Username: string
        +Password: string
        +OnSubmitLogin()
    }

    class EmployeeMgmtView {
        <<boundary>>
        +SearchQuery: string
        +SelectedDepartmentId: int
        +OnCreateEmployee()
        +OnUpdateEmployee()
    }

    class AuthControl {
        <<control>>
        +AuthenticateUser(username, password) Account
        +GenerateToken(account) string
        +ValidatePermission(accountId, roleName) bool
    }

    class EmployeeControl {
        <<control>>
        +CreateEmployee(userDto) User
        +UpdateEmployeeProfile(userDto) bool
        +GetEmployeeList(deptId, keyword) List~User~
    }

    class User {
        <<entity>>
        +Id: int
        +EmployeeCode: string
        +FullName: string
        +EmailCompany: string
        +Phone: string
        +Gender: bool
        +DateOfBirth: DateOnly
        +Status: bool
        +DepartmentId: int
        +PositionId: int
    }

    class Account {
        <<entity>>
        +Id: int
        +Username: string
        +PasswordHash: string
        +Status: bool
        +UserId: int
    }

    class Role {
        <<entity>>
        +Id: int
        +RoleName: string
        +Description: string
    }

    class Department {
        <<entity>>
        +Id: int
        +DepartmentCode: string
        +DepartmentName: string
        +ManagerAccountId: int
    }

    class Position {
        <<entity>>
        +Id: int
        +PositionCode: string
        +PositionName: string
    }

    LoginView ..> AuthControl : gởi yêu cầu đăng nhập
    EmployeeMgmtView ..> EmployeeControl : quản lý nhân sự
    AuthControl --> Account : xác thực
    AuthControl --> Role : kiểm tra quyền
    EmployeeControl --> User : quản lý
    User "1" <-- "1" Account : sở hữu
    User "*" --> "1" Department : thuộc phòng
    User "*" --> "1" Position : giữ chức vụ
    Account "*" <--> "*" Role : gán quyền
```

---

### Feature 2: Quản lý Chấm công & Ca làm việc

Quản lý lịch trình làm việc, phân ca, ghi nhận check-in/check-out hàng ngày và nhập dữ liệu bảng công từ file Excel.

```mermaid
classDiagram
    class AttendanceView {
        <<boundary>>
        +CheckInTime: TimeOnly
        +CheckOutTime: TimeOnly
        +OnCheckIn()
        +OnCheckOut()
        +OnUploadExcelFile()
    }

    class ShiftMgmtView {
        <<boundary>>
        +ShiftName: string
        +StartTime: TimeOnly
        +EndTime: TimeOnly
        +OnAssignShift()
    }

    class AttendanceControl {
        <<control>>
        +RecordCheckIn(userId, date, time)
        +RecordCheckOut(userId, date, time)
        +ImportAttendanceExcel(stream, periodId) List
        +LockTimesheetPeriod(periodId)
    }

    class ShiftControl {
        <<control>>
        +CreateShift(shiftDto) Shift
        +AssignShiftToEmployee(userId, shiftId, date)
    }

    class Attendance {
        <<entity>>
        +Id: int
        +EmployeeId: int
        +AttendanceDate: DateOnly
        +CheckInTime: TimeOnly
        +CheckOutTime: TimeOnly
        +PeriodId: int
    }

    class Shift {
        <<entity>>
        +Id: int
        +ShiftCode: string
        +ShiftName: string
        +StartTime: TimeOnly
        +EndTime: TimeOnly
    }

    class ShiftAssignment {
        <<entity>>
        +Id: int
        +EmployeeId: int
        +ShiftId: int
        +WorkDate: DateOnly
    }

    class TimesheetPeriod {
        <<entity>>
        +Id: int
        +Name: string
        +StartDate: DateOnly
        +EndDate: DateOnly
        +IsLocked: bool
    }

    AttendanceView ..> AttendanceControl : thực hiện điểm danh
    ShiftMgmtView ..> ShiftControl : phân ca làm việc
    AttendanceControl --> Attendance : ghi nhận dữ liệu
    AttendanceControl --> TimesheetPeriod : liên kết kỳ công
    ShiftControl --> Shift : quản lý ca
    ShiftControl --> ShiftAssignment : gán lịch
    Attendance "*" --> "1" TimesheetPeriod : thuộc kỳ
    ShiftAssignment "*" --> "1" Shift : áp dụng ca
```

---

### Feature 3: Quy trình Gửi & Phê duyệt Đơn từ

Quản lý vòng đời đơn của nhân viên (Nghỉ phép, Làm thêm giờ OT, Khiếu nại điều chỉnh công) qua các trạng thái: `Draft` -> `Pending` -> `Approved` / `Rejected` / `Cancelled`.

```mermaid
classDiagram
    class RequestSubmissionView {
        <<boundary>>
        +RequestTypeCode: string
        +Reason: string
        +LeaveStartDate: DateOnly
        +LeaveEndDate: DateOnly
        +OvertimeHours: decimal
        +OnSubmitRequest()
    }

    class ApprovalView {
        <<boundary>>
        +PendingRequestsList: List
        +ApproverNote: string
        +OnApprove()
        +OnReject()
    }

    class RequestWorkflowControl {
        <<control>>
        +SubmitRequest(requestDto) int
        +ApproveRequest(requestId, approverId, note)
        +RejectRequest(requestId, approverId, note)
        +CancelRequest(requestId, accountId)
        +UpdateLeaveBalance(employeeId, days)
    }

    class EmployeeRequest {
        <<entity>>
        +Id: int
        +EmployeeId: int
        +RequestTypeId: int
        +Status: RequestStatus
        +Reason: string
        +LeaveStartDate: DateOnly
        +LeaveEndDate: DateOnly
        +LeaveDays: decimal
        +OvertimeDate: DateOnly
        +OvertimeHours: decimal
        +ComplaintWorkDate: DateOnly
        +ComplaintProposedHours: decimal
        +ApproverId: int
        +ApprovedAt: DateTime
    }

    class RequestType {
        <<entity>>
        +Id: int
        +Code: string
        +Name: string
        +Description: string
    }

    class LeaveBalance {
        <<entity>>
        +Id: int
        +EmployeeId: int
        +Year: int
        +TotalDays: decimal
        +UsedDays: decimal
        +RemainingDays: decimal
    }

    RequestSubmissionView ..> RequestWorkflowControl : khởi tạo đơn
    ApprovalView ..> RequestWorkflowControl : phê duyệt đơn
    RequestWorkflowControl --> EmployeeRequest : cập nhật trạng thái
    RequestWorkflowControl --> LeaveBalance : trừ ngày phép khi duyệt
    EmployeeRequest "*" --> "1" RequestType : phân loại đơn
```

---

### Feature 4: Quản lý Hợp đồng Lao động

Tạo lập, phê duyệt, gia hạn và chấm dứt hợp đồng lao động giữa công ty và nhân viên.

```mermaid
classDiagram
    class ContractView {
        <<boundary>>
        +ContractNo: string
        +ContractType: string
        +BaseSalary: decimal
        +StartDate: DateOnly
        +EndDate: DateOnly
        +OnCreateContract()
        +OnApproveContract()
        +OnTerminateContract()
    }

    class ContractControl {
        <<control>>
        +CreateContract(dto) ContractDto
        +ApproveContract(contractId) bool
        +RejectContract(contractId, reason) bool
        +TerminateContract(contractId, reason) bool
        +GetActiveContract(userId) EmploymentContract
    }

    class EmploymentContract {
        <<entity>>
        +Id: int
        +ContractNo: string
        +ContractType: string
        +BaseSalary: decimal
        +StartDate: DateOnly
        +EndDate: DateOnly
        +Status: string
        +UserId: int
        +Reason: string
    }

    class User {
        <<entity>>
        +Id: int
        +EmployeeCode: string
        +FullName: string
    }

    ContractView ..> ContractControl : thao tác hợp đồng
    ContractControl --> EmploymentContract : quản lý vòng đời
    EmploymentContract "*" --> "1" User : ký với nhân viên
```

---

### Feature 5: Quản lý & Tính Lương Hàng tháng

Tổng hợp công thực tế, giờ OT duyệt, ngày nghỉ phép hưởng lương, tính bảo hiểm (BHXH 10.5%), thuế TNCN lũy tiến từng phần để xuất phiếu lương (`Payslip`).

```mermaid
classDiagram
    class PayrollView {
        <<boundary>>
        +SelectedPeriodId: int
        +PayslipList: List
        +OnCalculatePayroll()
        +OnExportPayslipPdf()
    }

    class PayrollControl {
        <<control>>
        +CalculateMonthlyPayroll(periodId) bool
        +CalculateInsuranceDeduction(baseSalary) decimal
        +CalculatePersonalIncomeTax(taxableIncome) decimal
        +GeneratePayslip(userId, periodId) Payslip
        +GetPayslipsByPeriod(periodId) List~Payslip~
    }

    class Payslip {
        <<entity>>
        +Id: int
        +UserId: int
        +PeriodId: int
        +BaseSalary: decimal
        +OtSalary: decimal
        +Allowances: decimal
        +InsuranceDeduction: decimal
        +TaxDeduction: decimal
        +GrossAmount: decimal
        +NetAmount: decimal
        +Status: string
        +ActualDays: decimal
        +LeavePaidDays: decimal
        +OtHours: decimal
    }

    class TimesheetPeriod {
        <<entity>>
        +Id: int
        +Name: string
        +IsLocked: bool
    }

    PayrollView ..> PayrollControl : yêu cầu tính lương
    PayrollControl --> Payslip : tạo & lưu bảng lương
    PayrollControl ..> TimesheetPeriod : kiểm tra trạng thái khóa sổ
    Payslip "*" --> "1" TimesheetPeriod : thuộc kỳ lương
```

---

## 4. SƠ ĐỒ TUẦN TỰ CÁC TÍNH NĂNG CHÍNH (SEQUENCE DIAGRAMS)

Các sơ đồ tuần tự thể hiện rõ nét sự tương tác giữa các lớp `<<boundary>>`, `<<control>>`, `<<entity>>` và `Database` theo thời gian.

### Sequence 1: Luồng Đăng nhập & Xác thực

```mermaid
sequenceDiagram
    autonumber
    actor NV as 👨‍💼 Nhân viên / NĐT
    participant View as 🖥️ LoginView <<boundary>>
    participant Ctrl as ⚙️ AuthControl <<control>>
    participant AccEntity as 📦 Account <<entity>>
    participant DB as 🗄️ Database

    NV->>View: Nhập Username & Password -> Nhấn "Đăng nhập"
    View->>Ctrl: AuthenticateUser(username, password)
    Ctrl->>DB: Query Account theo Username
    DB-->>Ctrl: Trả về thông tin Account & PasswordHash
    Ctrl->>AccEntity: Kiểm tra mật khẩu (VerifyHash) & Trạng thái Active
    alt Mật khẩu đúng & Tài khoản Active
        Ctrl-->>View: Trả về thành công & Session/Token
        View-->>NV: Chuyển hướng vào trang Dashboard
    else Mật khẩu sai hoặc Tài khoản bị khóa
        Ctrl-->>View: Trả về lỗi "Thông tin đăng nhập không hợp lệ"
        View-->>NV: Hiển thị thông báo lỗi trên màn hình
    end
```

---

### Sequence 2: Luồng Chấm công Hàng ngày & Nhập dữ liệu Excel

```mermaid
sequenceDiagram
    autonumber
    actor NV as 👨‍💼 Nhân viên / HR
    participant View as 🖥️ AttendanceView <<boundary>>
    participant Ctrl as ⚙️ AttendanceControl <<control>>
    participant AttEntity as 📦 Attendance <<entity>>
    participant PeriodEntity as 📦 TimesheetPeriod <<entity>>
    participant DB as 🗄️ Database

    rect rgb(240, 248, 255)
        note over NV, DB: KỊCH BẢN A: Nhân viên tự Check-in / Check-out trên web
        NV->>View: Nhấn nút "Check-in" / "Check-out"
        View->>Ctrl: RecordCheckIn(userId, currentDate, currentTime)
        Ctrl->>DB: Kiểm tra xem đã có bản ghi Attendance hôm nay chưa
        alt Chưa có bản ghi
            Ctrl->>AttEntity: Khởi tạo bản ghi Attendance mới (CheckInTime)
            Ctrl->>DB: Insert bản ghi Attendance mới
        else Đã có bản ghi Check-in
            Ctrl->>AttEntity: Cập nhật CheckOutTime
            Ctrl->>DB: Update bản ghi Attendance
        end
        DB-->>Ctrl: Xác nhận lưu thành công
        Ctrl-->>View: Trả về kết quả ghi nhận thành công
        View-->>NV: Hiển thị thời gian điểm danh thực tế
    end

    rect rgb(255, 245, 238)
        note over NV, DB: KỊCH BẢN B: HR Nhập file chấm công Excel hàng loạt
        NV->>View: Tải tệp Excel chấm công (.xlsx) & Chọn Kỳ công
        View->>Ctrl: ImportAttendanceExcel(fileStream, periodId)
        Ctrl->>PeriodEntity: Kiểm tra kỳ công có bị khóa sổ (IsLocked) không
        alt Kỳ công đã khóa (IsLocked = true)
            Ctrl-->>View: Ném ngoại lệ "Kỳ công đã khóa sổ!"
            View-->>NV: Hiển thị lỗi không thể nhập dữ liệu
        else Kỳ công đang mở (IsLocked = false)
            Ctrl->>Ctrl: Đọc & Parse dữ liệu từ file Excel
            Ctrl->>DB: Lưu hàng loạt (Bulk Insert/Update) danh sách Attendance
            DB-->>Ctrl: Xác nhận lưu thành công
            Ctrl-->>View: Trả về số lượng bản ghi đã nhập thành công
            View-->>NV: Hiển thị thông báo nhập dữ liệu hoàn tất
        end
    end
```

---

### Sequence 3: Luồng Gửi & Duyệt Đơn (Nghỉ phép / OT / Khiếu nại công)

```mermaid
sequenceDiagram
    autonumber
    actor NV as 👨‍💼 Nhân viên (Người gửi)
    participant SubView as 🖥️ RequestSubmissionView <<boundary>>
    participant Ctrl as ⚙️ RequestWorkflowControl <<control>>
    participant ReqEntity as 📦 EmployeeRequest <<entity>>
    participant BalEntity as 📦 LeaveBalance <<entity>>
    actor QL as 👨‍💻 Quản lý (Người duyệt)
    participant AppView as 🖥️ ApprovalView <<boundary>>
    participant DB as 🗄️ Database

    %% Bước 1: Tạo và Gửi đơn
    NV->>SubView: Điền thông tin (Loại đơn, Ngày nghỉ/Giờ OT, Lý do) -> Nhấn "Gửi đơn"
    SubView->>Ctrl: SubmitRequest(requestDto)
    Ctrl->>ReqEntity: Tạo mới bản ghi EmployeeRequest (Trạng thái = Pending)
    Ctrl->>DB: Lưu EmployeeRequest vào DB
    DB-->>Ctrl: Trả về RequestID
    Ctrl-->>SubView: Thông báo gửi đơn thành công
    SubView-->>NV: Hiển thị đơn ở trạng thái "Chờ duyệt (Pending)"

    %% Bước 2: Phê duyệt đơn
    QL->>AppView: Mở danh sách đơn chờ duyệt
    AppView->>Ctrl: GetPendingApprovals(approverId)
    Ctrl->>DB: Query các đơn Pending thuộc quyền quản lý
    DB-->>Ctrl: Danh sách đơn
    Ctrl-->>AppView: Hiển thị danh sách đơn
    
    QL->>AppView: Chọn đơn & Nhấn "Phê duyệt (Approve)"
    AppView->>Ctrl: ApproveRequest(requestId, approverId, note)
    Ctrl->>ReqEntity: Cập nhật Status = Approved, ApprovedAt, ApproverNote
    
    opt Nếu là Đơn Nghỉ Phép (LEAVE)
        Ctrl->>BalEntity: Trừ số ngày nghỉ vào LeaveBalance của nhân viên
        Ctrl->>DB: Cập nhật LeaveBalance
    end

    Ctrl->>DB: SaveChanges() lưu trạng thái Approved
    DB-->>Ctrl: Xác nhận thành công
    Ctrl-->>AppView: Trả về kết quả duyệt thành công
    AppView-->>QL: Cập nhật giao diện đơn đã duyệt
```

---

### Sequence 4: Luồng Tạo & Phê duyệt Hợp đồng Lao động

```mermaid
sequenceDiagram
    autonumber
    actor HR as 👩‍💼 Quản trị HR
    participant View as 🖥️ ContractView <<boundary>>
    participant Ctrl as ⚙️ ContractControl <<control>>
    participant ContractEntity as 📦 EmploymentContract <<entity>>
    participant DB as 🗄️ Database

    HR->>View: Điền thông tin hợp đồng (NV, Lương cơ bản, Ngày bắt đầu) -> Nhấn "Tạo hợp đồng"
    View->>Ctrl: CreateAsync(createContractDto)
    Ctrl->>ContractEntity: Khởi tạo EmploymentContract mới với Status = "Pending"
    Ctrl->>DB: Lưu bản ghi Hợp đồng
    DB-->>Ctrl: Trả về Hợp đồng kèm Mã hợp đồng tự động (ContractNo)
    Ctrl-->>View: Trả về kết quả tạo thành công
    View-->>HR: Hiển thị hợp đồng trong danh sách ở trạng thái "Pending"

    HR->>View: Kiểm tra chi tiết & Nhấn "Phê duyệt Hợp đồng"
    View->>Ctrl: ApproveContractAsync(contractId)
    Ctrl->>ContractEntity: Thay đổi Status từ "Pending" -> "Active"
    Ctrl->>DB: SaveChanges()
    DB-->>Ctrl: Xác nhận cập nhật
    Ctrl-->>View: Trả về trạng thái hợp đồng đã kích hoạt
    View-->>HR: Giao diện hiển thị Hợp đồng đang có hiệu lực ("Active")
```

---

### Sequence 5: Luồng Tính Lương & Xuất Phiếu Lương Hàng tháng

```mermaid
sequenceDiagram
    autonumber
    actor HR as 👩‍💼 Quản trị HR
    participant View as 🖥️ PayrollView <<boundary>>
    participant Ctrl as ⚙️ PayrollControl <<control>>
    participant PeriodEntity as 📦 TimesheetPeriod <<entity>>
    participant DB as 🗄️ Database
    participant SlipEntity as 📦 Payslip <<entity>>

    HR->>View: Chọn Kỳ công (Month/Year) & Nhấn "Tính lương hàng tháng"
    View->>Ctrl: CalculateMonthlyPayrollAsync(periodId)
    Ctrl->>PeriodEntity: Kiểm tra xem Kỳ công đã được khóa sổ (IsLocked) chưa?
    
    alt Kỳ công chưa khóa sổ
        Ctrl-->>View: Lỗi "Vui lòng khóa kỳ công trước khi tính lương!"
        View-->>HR: Thông báo yêu cầu khóa kỳ công trước
    else Kỳ công đã khóa sổ thành công
        Ctrl->>DB: Lấy danh sách Nhân viên Active
        Ctrl->>DB: Lấy danh sách Hợp đồng Active (để lấy BaseSalary)
        Ctrl->>DB: Lấy dữ liệu Chấm công thực tế trong kỳ (D_actual)
        Ctrl->>DB: Lấy các Đơn Nghỉ phép (D_leave) & OT (H_ot) đã Approved
        
        loop Đối với từng Nhân viên
            Ctrl->>Ctrl: 1. Đơn giá ngày công = BaseSalary / StandardWorkingDays
            Ctrl->>Ctrl: 2. Lương công thực tế = Đơn giá * (D_actual + D_leave)
            Ctrl->>Ctrl: 3. Tiền OT = H_ot * (HourlyRate * 1.5)
            Ctrl->>Ctrl: 4. Tổng thu nhập (Gross) = Lương công + Tiền OT + Phụ cấp
            Ctrl->>Ctrl: 5. Bảo hiểm bắt buộc = Min(BaseSalary, Trần BH) * 10.5%
            Ctrl->>Ctrl: 6. Thu nhập chịu thuế = Gross - Bảo hiểm - 11,000,000 VNĐ
            Ctrl->>Ctrl: 7. Thuế TNCN lũy tiến = Biểu thuế lũy tiến từng phần
            Ctrl->>Ctrl: 8. Lương thực lĩnh (Net) = Gross - Bảo hiểm - Thuế TNCN
            Ctrl->>SlipEntity: Khởi tạo bản ghi Payslip mới
        end

        Ctrl->>DB: Ghi đè / Xóa phiếu lương cũ của kỳ & Lưu danh sách Payslip mới
        DB-->>Ctrl: Xác nhận lưu bảng lương thành công
        Ctrl-->>View: Trả về kết quả hoàn tất tính lương
        View-->>HR: Hiển thị bảng danh sách Phiếu lương hàng tháng của toàn bộ công ty
    end
