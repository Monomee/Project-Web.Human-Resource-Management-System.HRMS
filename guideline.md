Tuần 1: [Thành viên 1] Làm Auth & Layout ----> Cung cấp Móng Bảo mật cho cả nhóm  
        [Thành viên 2] Làm Module Employee ---> Tạo dữ liệu nhân sự gốc  
        [Thành viên 3] Làm Module Request ----> Viết luồng đơn từ nghỉ phép/OT  
        [Thành viên 4] Làm Module Attendance -> Viết bộ đọc file Excel chấm công  

Tuần 2: [Thành viên 1 & 2] Ghép nối tính toán làm Module Tính Lương (Payroll Engine)  
        [Thành viên 3 & 4] Hoàn thiện Dashboard Thống kê + Tối ưu Async/Migrations  

# Prompt 1: Dành cho Thành viên 1 (Làm Xác thực Auth & Giao diện Gốc)
HÃY ĐỌC KỸ FILE structure.md và Agent.md TRƯỚC KHI LÀM.  
Tôi là Intern Developer. Bạn là Senior Architect. Hãy giúp tôi hoàn thành TASK 2.1 và TASK 2.2: Cấu hình Cookie Authentication và thiết kế NavMenu.razor.  

Yêu cầu chi tiết:  
1. Hãy tạo Interface IAuthService ở tầng Application và class cài đặt AuthService ở tầng Infrastructure. AuthService sẽ kiểm tra tài khoản từ bảng Accounts (nối với bảng Users và Roles). Hãy dùng thư viện BCrypt.Net-Next để kiểm tra mật khẩu. Lưu ý: Chỉ cho phép tài khoản có Status == true đăng nhập. Nếu đăng nhập thành công, hãy tạo Claims (gồm Id, FullName, Role) và trả về.  
2. Hãy hướng dẫn tôi cấu hình Authentication Cookie trong file Program.cs ở tầng WebUI đúng chuẩn .NET 8/9 Blazor Server.  
3. Hãy viết file Login.razor tại HRMS.WebUI/Pages/Auth/ sử dụng EditForm của Blazor để thu thập dữ liệu đăng nhập và hiển thị thông báo lỗi mượt mà.  
4. Hãy cập nhật file NavMenu.razor sử dụng thẻ <AuthorizeView> và <AuthorizeView Roles="..."> để phân quyền menu theo đúng yêu cầu trong file structure.md.  

Hãy viết code theo cấu trúc Clean Architecture từng bước một, giải thích rõ ràng cho tôi hiểu để tôi không bị "vibe code" mù quáng.  
# Prompt 2: Dành cho Thành viên 2 (Làm Module Nhân sự & Hợp đồng)
HÃY ĐỌC KỸ FILE structure.md và Agent.md TRƯỚC KHI LÀM.  
Tôi cần hoàn thành TASK 3.1: Viết Module Quản lý Hồ sơ & Hợp đồng Nhân sự.  

Yêu cầu chi tiết:  
1. Tại tầng Application, hãy tạo thư mục Features/Employees/. Viết class EmployeeService.cs (và Interface tương ứng) chứa phương thức lấy danh sách nhân viên đầy đủ thông tin phòng ban (Department) và chức vụ (Position) từ EF Core.  
2. Đăng ký dịch vụ này vào file DependencyInjection.cs của tầng Application.  
3. Tại tầng WebUI, viết trang EmployeeList.razor trong thư mục Pages/Employee/. Thiết lập trang chạy ở chế độ @rendermode InteractiveServer.  
4. Hiển thị danh sách ra bảng HTML chỉn chu bằng Bootstrap. Xử lý hiển thị trường Gender (true -> Nam, false -> Nữ) và Status (true -> Đang làm, false -> Đã nghỉ).  
5. Thêm nút "Sửa trạng thái" để HR cập nhật Status của nhân viên trực tiếp trên giao diện mà không tải lại trang.  

Hãy sinh mã nguồn chuẩn chỉnh, chia nhỏ các bước thực hiện theo đúng cấu trúc thư mục Clean Architecture.  
# Prompt 3: Dành cho Thành viên 3 (Làm Module Luồng Duyệt Đơn Từ)
HÃY ĐỌC KỸ FILE structure.md và Agent.md TRƯỚC KHI LÀM.  
Tôi cần hoàn thành TASK 3.2: Module Gửi Đơn từ & Phê duyệt Workflow (Nghỉ phép & OT).  

Yêu cầu chi tiết:  
1. Tại tầng Application, tạo thư mục Features/Requests/. Viết RequestService.cs xử lý logic gửi đơn. Nếu là đơn LEAVE, phải kiểm tra trường Value với RemainingDays trong bảng LeaveBalances của User đó. Nếu không đủ ngày phép, chặn lại và quăng ra Exception thông báo lỗi.  
2. Viết logic phê duyệt đơn ApproveRequest(int requestId, int approverId). Nếu đơn được duyệt (Status đổi sang 'Approved') và loại đơn đó là 'OT', hãy viết code sẵn sàng ghi nhận số giờ OT để phục vụ tính lương.  
3. Tại tầng WebUI, viết giao diện CreateRequest.razor cho nhân viên tạo đơn và ApprovalList.razor cho quản lý duyệt đơn (Sử dụng InteractiveServer mode). Lọc danh sách đơn chờ duyệt dựa trên CurrentApproverAccountId của người đang đăng nhập.  

Hãy sinh mã nguồn sạch, xử lý bất đồng bộ (async/await) toàn bộ các câu lệnh gọi xuống Database.  
# Prompt 4: Dành cho Thành viên 4 (Làm Bộ đọc File Excel Chấm Công)
HÃY ĐỌC KỸ FILE structure.md và Agent.md TRƯỚC KHI LÀM.  
Tôi cần hoàn thành TASK 3.3: Module Import Excel Chấm công Công sở sử dụng thư viện EPPlus.  

Yêu cầu chi tiết:  
1. Viết một Interface IExcelParserService ở tầng Application và cài đặt lớp ExcelParserService ở tầng Infrastructure. Sử dụng EPPlus để đọc luồng Stream của file Excel tải lên (gồm các cột: EmployeeCode, CheckedAt, CheckType).  
2. Viết thuật toán tại Application đối chiếu với khung giờ cố định (Sáng: 08:00, Chiều: 17:30). Tính số phút đi muộn (nếu giờ IN > 08:00) và tính giá trị ngày công (1.0 hoặc 0.5 công). Sau đó lưu kết quả vào bảng AttendanceLogs.  
3. Viết giao diện ImportAttendance.razor ở tầng WebUI sử dụng component <InputFile> của Blazor để HR tải file lên xử lý real-time. Thêm nút bấm "Khóa kỳ công" để đổi trường IsLocked của bảng TimesheetPeriods sang true.  

Hãy viết mã nguồn rõ ràng, có chú thích (comment) thuật toán tính ngày công chi tiết để một Intern như tôi đọc cũng hiểu được bản chất.  
# Prompt 5: Dành cho Thành viên 1 & 2 (Làm Công Cụ Tính Lương)
HÃY ĐỌC KỸ FILE structure.md và Agent.md TRƯỚC KHI LÀM.  
Chúng tôi cần hoàn thành TASK 3.4: Module Tính Lương Tự Động & Xem Phiếu Lương (Payroll Engine). Đây là lõi quan trọng nhất của đồ án.  

Yêu cầu chi tiết:  
1. Tại tầng Application, viết hàm CalculateMonthlyPayroll(int periodId). Logic phải check: Nếu kỳ công chưa được khóa (IsLocked == false), không cho tính lương. Nếu đã khóa, quét danh sách toàn bộ nhân viên đang hoạt động (Status == true) để tính toán:  
   - Lương thực tế = (BaseSalary trong EmploymentContracts / 26) * Số ngày công trong kỳ từ AttendanceLogs.  
   - Tiền OT = Số giờ OT được duyệt ở bảng Requests * Lương một giờ cơ bản * 1.5.  
   - Khấu trừ bảo hiểm = Lương hợp đồng * 10.5%.  
   - Khấu trừ thuế TNCN = Tính theo công thức thuế lũy tiến từng phần Việt Nam sau khi trừ 11 triệu giảm trừ bản thân.  
   - Thực lĩnh (Net) = Gross (Lương thực tế + OT) - Bảo hiểm - Thuế.  
2. Ghi toàn bộ kết quả vào bảng Payslips.  
3. Viết màn hình MyPayslip.razor ở WebUI hiển thị bảng lương chi tiết cho nhân viên xem, định dạng CSS sạch đẹp.  

Hãy thiết kế thuật toán thật chuẩn xác, sử dụng kiểu dữ liệu decimal(18,2) cho tiền tệ, không để xảy ra sai số.  
# Prompt 6: Dành cho Cả nhóm (Làm Dashboard & Tối ưu Nghiệm thu)
HÃY ĐỌC KỸ FILE structure.md và Agent.md TRƯỚC KHI LÀM.  
Dự án của chúng tôi đã hoàn thành các module lõi. Bây giờ hãy giúp chúng tôi làm TASK 4.1 và TASK 4.3: Viết trang Dashboard thống kê và rà soát tối ưu mã nguồn để nghiệm thu đồ án.  

Yêu cầu chi tiết:  
1. Tại WebUI, viết file Dashboard.razor hiển thị 3 thẻ số liệu tổng quan: Tổng số nhân sự, Số đơn từ chờ duyệt, Tổng chi phí quỹ lương kỳ gần nhất. Dùng LINQ EF Core tinh gọn để đếm dữ liệu tốc độ cao.  
2. Vẽ một biểu đồ cột (Bar Chart) mô phỏng tổng chi phí lương của các tháng trước bằng thẻ HTML/CSS Div tỉ lệ linh hoạt.  
3. Hãy rà soát toàn bộ giải pháp, đảm bảo tất cả các hàm gọi xuống Database đều sử dụng các từ khóa async/await và các phương thức bất đồng bộ (như ToListAsync, SaveChangesAsync, FirstOrDefaultAsync) để tối ưu hiệu năng tối đa cho Blazor Server.  

Hãy đưa ra giải pháp sạch sẽ, gọn gàng để chúng tôi tự tin bảo vệ đồ án trước hội đồng.  