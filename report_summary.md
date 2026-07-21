# Báo cáo Tổng hợp kết quả sửa lỗi & Tối ưu hóa hệ thống HRMS

Báo cáo này tổng hợp chi tiết các lỗi hệ thống, lỗ hổng bảo mật, sai sót nghiệp vụ lao động, và hiệu năng cơ sở dữ liệu đã được rà soát từ [review_report.md](file:///c:/Users/hoang/.gemini/antigravity-ide/brain/4161e703-f22b-4f96-b1ca-094fd1a9d085/review_report.md) (ngoại trừ Lỗi 2.2 về việc bổ sung trường người phụ thuộc), đi kèm các biện pháp kỹ thuật cụ thể đã triển khai thành công.

---

## 1. Lĩnh vực Bảo mật & An toàn thông tin (Security)

### 🚨 Lỗi 1.1: SignalR Hijacking - Rò rỉ thông tin đơn từ của toàn hệ thống
* **Vấn đề**: Client có thể tự gửi tham số ID tài khoản bất kỳ lên `RequestHub` để tham gia nghe lén thông báo của người khác mà không có sự kiểm tra hay xác thực chéo.
* **Cách giải quyết**:
  - Loại bỏ các tham số ID truyền từ phía client.
  - Sử dụng cơ chế xác thực kết nối SignalR an toàn thông qua `token` được gửi qua Query String trong quá trình bắt tay kết nối.
  - Token được kiểm tra đối chiếu qua tệp [TempTokenStore.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Services/TempTokenStore.cs) để trích xuất `ClaimsPrincipal` xác định ID tài khoản hiện hành lưu trữ trong `Context.Items["UserPrincipal"]`.
  - Chỉ cho phép người dùng lắng nghe thông báo của chính họ.

### 🚨 Lỗi 1.2: Mật khẩu mặc định cố định & Thiếu tính năng đổi mật khẩu
* **Vấn đề**: Nhân viên mới được tạo với mật khẩu mặc định cố định, hệ thống thiếu trang đổi mật khẩu và nút đặt lại mật khẩu của quản lý/HR khi nhân viên quên.
* **Cách giải quyết**:
  - Đổi mật khẩu mặc định khi tạo nhân viên mới từ `Password@123` sang `Password123` băm bảo mật.
  - Thiết lập trang [ChangePassword.razor](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Components/Pages/Auth/ChangePassword.razor) với giao diện kính mờ cao cấp, hiển thị banner cảnh báo nhắc nhở người dùng chủ động đổi mật khẩu mặc định.
  - Cài đặt phương thức `ResetPasswordAsync` trong [AuthService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Infrastructure/Services/AuthService.cs) đặt lại mật khẩu nhân viên về `Password123` đã băm.
  - Bổ sung nút **Đặt lại mật khẩu** tại [EmployeeList.razor](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Components/Pages/Employee/EmployeeList.razor) kèm modal xác nhận và modal thành công hỗ trợ sao chép mật khẩu bằng 1 cú nhấp vào Clipboard qua JS.

### 🚨 Lỗi 1.3: Thiếu giới hạn kích thước tập tin tải lên (DoS / Memory Exhaustion)
* **Vấn đề**: Không kiểm soát độ lớn và định dạng tệp Excel quẹt thẻ tải lên dẫn tới rủi ro tràn bộ nhớ RAM (Out of Memory) gây sập ứng dụng.
* **Cách giải quyết**:
  - Tại [ImportAttendance.razor](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Components/Pages/Attendance/ImportAttendance.razor), kiểm tra chặt chẽ đuôi tệp mở rộng là `.xlsx`.
  - Giới hạn dung lượng tệp tải lên: Nếu vượt quá `10MB` (`file.Size > 10 * 1024 * 1024`), hệ thống lập tức chặn lại và cảnh báo: *"Dung lượng tệp vượt quá giới hạn cho phép (Tối đa 10MB)."*

---

## 2. Lĩnh vực Nghiệp vụ & Pháp lý Lao động (Business Logic & Compliance)

### ⚠️ Lỗi 2.1: Tính toán bảo hiểm xã hội (BHXH) sai luật lao động Việt Nam
* **Vấn đề**: Thuế BHXH bắt buộc bị trừ cứng bằng `10.5% * Lương cơ bản` bất kể mức lương cao, gây sai luật do thiếu áp trần đóng.
* **Cách giải quyết**:
  - Cập nhật công thức tính bảo hiểm trong [PayrollService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Infrastructure/Services/PayrollService.cs).
  - Áp dụng mức trần đóng BHXH bắt buộc tối đa bằng **20 lần mức lương cơ sở** hiện hành (tương ứng **46,800,000 VNĐ**). Với nhân sự có lương lớn hơn 46.8M, mức khấu trừ bảo hiểm tối đa luôn bằng **4,914,000 VNĐ**.

### ⚠️ Lỗi 2.3: Race Condition (Tranh chấp) khi phê duyệt đơn xin nghỉ phép
* **Vấn đề**: Người dùng có thể lách luật gửi nhiều đơn nghỉ phép cùng lúc để vượt quá số ngày phép thực tế mà hệ thống không kiểm soát trước khi duyệt.
* **Cách giải quyết**:
  - Điều chỉnh logic kiểm tra số dư phép trong [RequestService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Application/Services/RequestService.cs).
  - Khi tính toán số ngày phép khả dụng thực tế để quyết định cho tạo đơn phép mới hay không, hệ thống sẽ tự động trừ đi tổng số ngày nghỉ của tất cả các đơn xin nghỉ phép đang ở trạng thái chờ duyệt (`Pending`) của năm đó:
    `AvailableDays = RemainingDays - Sum(PendingRequestDays)`
  - Từ chối tạo đơn mới nếu không đủ ngày phép khả dụng thực tế.

### ⚠️ Lỗi 2.4: Chia đều 26 ngày công cố định (Bất hợp lý thực tế)
* **Vấn đề**: Việc áp dụng công chuẩn cố định 26 ngày công làm hụt lương hoặc tăng lương bất hợp lý đối với các tháng có số ngày làm việc thực tế khác nhau (ví dụ: tháng 24 ngày công, tháng 27 ngày công).
* **Cách giải quyết**:
  - Cập nhật logic tính đơn giá ngày công trong [PayrollService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Infrastructure/Services/PayrollService.cs) để tính toán số ngày công chuẩn thực tế của từng tháng cụ thể dựa trên số ngày đi làm lý thuyết trừ đi các ngày thứ Bảy, Chủ Nhật thuộc kỳ lương đó.

---

## 3. Lĩnh vực Hiệu năng & Khả năng mở rộng (Performance & Scalability)

### 🐢 Vấn đề 3.1: Thiếu Index vật lý trên các bảng dữ liệu tần suất cao
* **Vấn đề**: Các bảng lớn như `AttendanceLogs` và `Requests` bị chậm chạp (Table Scan) do thiếu chỉ mục vật lý khi dữ liệu phình to.
* **Cách giải quyết**:
  - Viết script và thực thi các Non-Clustered Indexes tối ưu:
    1. Chỉ mục composite `IX_AttendanceLogs_UserPeriod` trên bảng `AttendanceLogs` cho các khóa `(UserId, PeriodId)` để tối ưu hóa việc lấy log chấm công theo kỳ của nhân viên.
    2. Chỉ mục composite `IX_Requests_Approver` trên bảng `Requests` cho các khóa `(CurrentApproverAccountId, Status)` để tối ưu màn hình duyệt đơn.
    3. Chỉ mục `IX_Requests_Creator` trên bảng `Requests` cho khóa `CreatedByAccountId` để tăng tốc độ tải lịch sử đơn từ cá nhân.

### 🐢 Vấn đề 3.2: Xung đột tính toán lương song song (Cross-circuit Concurrency)
* **Vấn đề**: Hai HR thực hiện tính toán lương cùng một lúc cho một kỳ lương dẫn tới việc ghi đè, trùng lặp khoá ngoại hoặc lỗi khóa chết (Deadlock) ở tầng cơ sở dữ liệu.
* **Cách giải quyết**:
  - Trong [PayrollService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Infrastructure/Services/PayrollService.cs), bọc toàn bộ chuỗi thao tác *"Xóa Payslip cũ -> Tính lương -> Lưu Payslip mới"* bên trong một Database Transaction an toàn:
    ```csharp
    using var transaction = await _context.Database.BeginTransactionAsync();
    // Thực hiện tính lương và lưu
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    ```
  - Cơ chế này đảm bảo tính nhất quán (Atomicity) và ngăn chặn việc tính toán trùng lặp khi chạy song song.
