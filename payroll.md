# Kiến Trúc & Logic Nghiệp Vụ - Module Payroll (Công Cụ Tính Lương)

Tài liệu này mô tả chi tiết cách thức thiết kế, các công thức tính toán và cách triển khai code qua các tầng đối với tính năng **Payroll & Payslips Engine (Tính lương hàng tháng)** trong hệ thống HRMS.

---

## 1. Nghiệp Vụ & Logic Tính Toán Lương

Module Payroll có vai trò tự động tính toán lương hàng tháng cho toàn bộ nhân sự dựa trên dữ liệu công đã được xác nhận (khóa sổ) trong kỳ.

### Các Quy Tắc Nghiệp Vụ Cốt Lõi:
1. **Ràng buộc khóa sổ:** Chỉ cho phép tính lương đối với kỳ công đã khóa (`TimesheetPeriod.IsLocked == true`). Nếu kỳ công đang mở, hệ thống sẽ từ chối xử lý để tránh sai sót dữ liệu.
2. **Tính đơn giá và ngày công:**
   * **Số ngày công chuẩn ($D_{std}$):** Đếm tất cả các ngày trong kỳ ngoại trừ các ngày Chủ nhật. Nếu không tính toán được, mặc định là $26$ ngày.
   * **Đơn giá ngày công ($R_{day}$):** Lương hợp đồng ($S_{base}$) / Số ngày công chuẩn ($D_{std}$).
   * **Lương thực tế theo ngày công ($S_{actual}$):** $R_{day} \times$ (Số ngày công thực tế $D_{actual}$ + Số ngày nghỉ phép hưởng lương $D_{leave}$).
3. **Tính toán làm thêm giờ (OT):**
   * **Lương một giờ cơ bản ($R_{hour}$):** $S_{base}$ / ($D_{std} \times 8$).
   * **Tiền lương OT ($S_{ot}$):** Số giờ OT được duyệt ($H_{ot}$) $\times R_{hour} \times 1.5$ (hệ số OT tiêu chuẩn).
4. **Các khoản khấu trừ:**
   * **Bảo hiểm xã hội bắt buộc ($I_{ded}$):** Bằng Lương hợp đồng $\times 10.5\%$. Áp dụng mức trần tối đa tính trên 20 lần mức lương cơ sở Việt Nam ($46,800,000$ VNĐ).
   * **Thu nhập tính thuế TNCN ($I_{taxable}$):** $\text{Gross} - I_{ded} - 11,000,000$ VNĐ (mức giảm trừ gia cảnh bản thân). Nếu kết quả $< 0$, thu nhập chịu thuế bằng $0$.
   * **Thuế thu nhập cá nhân ($T_{ded}$):** Áp dụng biểu thuế lũy tiến từng phần theo luật Việt Nam:
     * Dưới 5tr: $5\%$
     * Từ 5tr đến 10tr: $10\% - 250,000$ VNĐ
     * Từ 10tr đến 18tr: $15\% - 750,000$ VNĐ
     * Từ 18tr đến 32tr: $20\% - 1,650,000$ VNĐ
     * Từ 32tr đến 52tr: $25\% - 3,250,000$ VNĐ
     * Từ 52tr đến 80tr: $30\% - 5,850,000$ VNĐ
     * Trên 80tr: $35\% - 9,850,000$ VNĐ
5. **Thực lĩnh (Net):** $\text{Gross} - I_{ded} - T_{ded}$. (Gross = $S_{actual} + S_{ot}$ + Phụ cấp).
6. **Tính Idempotent (Không trùng lặp):** Khi tính lại lương cho một kỳ, hệ thống sẽ thực hiện xóa toàn bộ các phiếu lương (`Payslip`) cũ của kỳ đó trước khi chèn mới, toàn bộ quá trình được bọc trong một Database Transaction.

---

## 2. Chi Tiết Triển Khai Code Theo Từng Tầng

### 📂 Tầng 1: Domain (`HRMS.Domain`)
Chứa các lớp thực thể lưu trữ cấu trúc dữ liệu của lương và chấm công.

* **File:** [Payslip.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Domain/Entities/Payslip.cs)
  * **Nhiệm vụ:** Đại diện cho bảng `Payslips`, chứa các thông tin tính toán lương: `BaseSalary` (Lương hợp đồng), `OtSalary` (Lương OT), `InsuranceDeduction` (Khấu trừ bảo hiểm), `TaxDeduction` (Thuế TNCN), `GrossAmount` (Tổng thu nhập), `NetAmount` (Thực nhận), trạng thái phiếu lương `Status` (Draft, Approved, Paid).
* **File:** [TimesheetPeriod.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Domain/Entities/TimesheetPeriod.cs)
  * **Nhiệm vụ:** Lưu thông tin kỳ công: thời gian bắt đầu/kết thúc và trạng thái khóa kỳ công `IsLocked`.
* **File:** [AttendanceLog.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Domain/Entities/AttendanceLog.cs)
  * **Nhiệm vụ:** Lưu nhật ký quẹt thẻ của nhân viên hàng ngày để đếm số ngày công thực tế.

---

### ⚙️ Tầng 2: Application (`HRMS.Application`)
Khai báo các hợp thức giao dịch và điều phối luồng dữ liệu cho Module tính lương.

* **File:** [IPayrollService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Application/Interfaces/IPayrollService.cs)
  * **Nhiệm vụ:** Khai báo các phương thức giao tiếp tính toán lương:
    ```csharp
    public interface IPayrollService
    {
        Task<bool> CalculateMonthlyPayrollAsync(int periodId);
        Task<List<Payslip>> GetPayslipsByPeriodAsync(int periodId);
        Task<Payslip?> GetMyPayslipAsync(int periodId, int userId);
        Task<int> GetUserIdByAccountIdAsync(int accountId);
    }
    ```

---

### 💾 Tầng 3: Infrastructure (`HRMS.Infrastructure`)
Nơi cài đặt toàn bộ thuật toán tính toán lương phức tạp, thực hiện truy vấn cơ sở dữ liệu để tổng hợp công, đơn từ nghỉ phép, làm thêm giờ và ghi kết quả.

* **File:** [PayrollService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Infrastructure/Services/PayrollService.cs)
  * **Nhiệm vụ:** Triển khai phương thức `CalculateMonthlyPayrollAsync(int periodId)` theo các bước:
    1. Kiểm tra kỳ công có tồn tại và đã khóa (`IsLocked == true`) hay chưa.
    2. Lấy danh sách nhân viên đang hoạt động (`Status == true`).
    3. Lấy hợp đồng `Active` của từng nhân viên để lấy lương cơ bản (`BaseSalary`).
    4. Gom nhóm `AttendanceLogs` đếm số ngày công đi làm thực tế (`D_actual`) bằng cách đếm số ngày quẹt thẻ duy nhất (distinct date).
    5. Đọc các đơn nghỉ phép `LEAVE` được phê duyệt (`Status == "Approved"`) để tính ngày nghỉ hưởng lương (`D_leave_paid`).
    6. Đọc các đơn làm thêm giờ `OT` được phê duyệt để tính tổng giờ OT (`H_ot`).
    7. Tính ngày công chuẩn hệ thống (loại trừ các ngày Chủ nhật).
    8. Thực hiện vòng lặp qua từng nhân viên, thực hiện các công thức tính đơn giá, lương ngày công, lương OT, bảo hiểm, thuế TNCN lũy tiến và tiền thực nhận (Net).
    9. Mở Transaction: Xóa toàn bộ dữ liệu `Payslips` cũ của kỳ công này (nếu có), thêm mới danh sách `Payslip` với trạng thái mặc định là `"Draft"`, gọi `SaveChangesAsync` và commit transaction.

---

### 🖥️ Tầng 4: WebUI (`HRMS.WebUI`)
Chứa giao diện cho người quản trị thực hiện tính toán và xem bảng lương, cũng như giao diện xem/in phiếu lương cá nhân của nhân viên.

* **File:** [PayrollManagement.razor](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Components/Pages/Payroll/PayrollManagement.razor)
  * **Nhiệm vụ:** Giao diện quản lý lương dành cho `Admin, HRM`.
    * Cung cấp một hộp chọn kỳ công để xem dữ liệu bảng lương.
    * Nút bấm "Tính toán Bảng lương" sẽ gọi `PayrollService.CalculateMonthlyPayrollAsync(periodId)` rồi tải lại bảng lương.
    * Hiển thị danh sách lương chi tiết của toàn công ty với các chỉ số: Lương hợp đồng, lương OT, bảo hiểm, thuế và Net.
* **File:** [MyPayslip.razor](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Components/Pages/Payroll/MyPayslip.razor)
  * **Nhiệm vụ:** Giao diện dành cho nhân viên tự xem phiếu lương cá nhân của mình trong các kỳ công.
    * Sử dụng CSS in ấn `@media print` được định dạng sạch đẹp giúp nhân viên có thể nhấn tổ hợp phím `Ctrl + P` để in phiếu lương hoặc xuất sang PDF trực tiếp bằng trình duyệt.

---

## 3. Liên Kết Luồng & Cách Các Tầng Phối Hợp

Sơ đồ thể hiện luồng hoạt động khi người quản trị (HRM/Admin) nhấn nút **"Tính toán Bảng lương"**:

```
[HRM/Admin]          [PayrollManagement]          [PayrollService]          [ApplicationDbContext]
     |                       |                            |                           |
     |-- 1. Chọn Kỳ công ---->|                            |                           |
     |   Nhấn tính lương     |-- 2. Gọi hàm ------------->|                           |
     |                       |  (CalculateMonthlyPayroll) |                           |
     |                       |                            |-- 3. Check kỳ công khóa ->|
     |                       |                            |-- 4. Get Active Users ---->|
     |                       |                            |-- 5. Get Contracts ------>|
     |                       |                            |-- 6. Get AttendanceLogs ->|
     |                       |                            |-- 7. Get OT & Leave Req -->|
     |                       |                            |                           |
     |                       |                            |-- 8. Thực hiện tính toán  |
     |                       |                            |   lương chi tiết cho      |
     |                       |                            |   từng nhân viên          |
     |                       |                            |                           |
     |                       |                            |-- 9. Begin Transaction ->|
     |                       |                            |-- 10. Xóa Payslip cũ ---->|
     |                       |                            |-- 11. Lưu Payslip mới --->|
     |                       |                            |-- 12. Commit ------------>|
     |                       |<-- 13. Trả về True/False --|                           |
     |                       |-- 14. Gọi LoadPayslips() ->|                           |
     |                       |<-- 15. Hiển thị bảng lương-|                           |
     |<-- Xem kết quả -------|                            |                           |
```
