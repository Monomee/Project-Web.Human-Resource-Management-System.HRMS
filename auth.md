# Kiến Trúc & Logic Nghiệp Vụ - Module Authentication (Xác thực)

Tài liệu này mô tả chi tiết cách thức thiết kế, luồng xử lý và cách triển khai code qua các tầng đối với tính năng **Authentication (Xác thực và Phân quyền)** trong hệ thống HRMS.

---

## 1. Nghiệp Vụ & Logic Xác Thực

Hệ thống sử dụng cơ chế **Cookie-based Authentication** kết hợp phân quyền qua vai trò (**Role-based Authorization**). Do ứng dụng Blazor Server chạy qua kết nối thời gian thực SignalR (WebSockets), HTTP Context không thể ghi trực tiếp Header (`Set-Cookie`) từ bên trong trang tương tác mà phải qua một luồng chuyển tiếp thông minh thông qua bộ lưu trữ token tạm thời.

### Quy trình nghiệp vụ:
1. **Kiểm tra trạng thái:** Chỉ cho phép các tài khoản có trạng thái kích hoạt (`Status == true` hay `1` trong Database) đăng nhập vào hệ thống.
2. **Mã hóa và so khớp:** Mật khẩu của người dùng được mã hóa bằng thuật toán băm bảo mật **BCrypt**. Khi đăng nhập, mật khẩu nhập vào được băm và so khớp với mã băm trong Database.
3. **Thu thập quyền:** Nếu tài khoản hợp lệ, thu thập toàn bộ các Role được liên kết với Account đó (Ví dụ: `Admin`, `HRM`, `HR`, `Employee`) và gán vào danh sách Claims.
4. **Thiết lập phiên làm việc (Cookie):**
   * Do Blazor Server sử dụng SignalR, hệ thống dùng một lớp trung gian `TempTokenStore` lưu giữ `ClaimsPrincipal` tạm thời và sinh ra một mã token dạng GUID có hiệu lực trong 30 giây.
   * Trình duyệt thực hiện chuyển hướng bắt buộc (force load) đến endpoint HTTP `/auth/signin?token={tempToken}`.
   * Endpoint này đọc token từ memory store, lấy `ClaimsPrincipal`, gọi `HttpContext.SignInAsync` để ghi Cookie `HRMS_AuthCookie` xuống trình duyệt và redirect người dùng về trang chủ.

---

## 2. Chi Tiết Triển Khai Code Theo Từng Tầng

### 🔑 Tầng 1: Domain (`HRMS.Domain`)
Chứa các lớp thực thể thuần (POCO) biểu diễn cấu trúc dữ liệu của tài khoản và quyền hạn, không phụ thuộc vào cơ chế lưu trữ.

* **File:** [Account.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Domain/Entities/Account.cs)
  * **Nhiệm vụ:** Đại diện cho bảng `Accounts`, lưu thông tin đăng nhập: `Username`, `PasswordHash`, trạng thái kích hoạt `Status` và liên kết với thông tin người dùng `User`.
* **File:** [Role.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Domain/Entities/Role.cs)
  * **Nhiệm vụ:** Đại diện cho bảng `Roles`, chứa thông tin vai trò như `Admin`, `HRM`, `HR`, `Employee`.
* **Mối liên hệ:** Một tài khoản (`Account`) có thể có nhiều vai trò (`Role`) thông qua thực thể liên kết nhiều-nhiều được định nghĩa bằng Navigation Property `Roles` trong `Account`.

---

### ⚙️ Tầng 2: Application (`HRMS.Application`)
Khai báo các giao thức (Interfaces) và cấu trúc dữ liệu chuyển đổi (DTOs) dùng để giao tiếp giữa UI và logic xử lý chính.

* **File:** [IAuthService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Application/Interfaces/IAuthService.cs)
  * **Nhiệm vụ:** Khai báo các phương thức nghiệp vụ xác thực chính bao gồm:
    ```csharp
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword);
        Task<string> ResetPasswordAsync(int accountId);
    }
    ```
* **File:** `AuthResult.cs` (DTO)
  * **Nhiệm vụ:** Định nghĩa cấu trúc trả về sau khi xác thực thành công hay thất bại:
    * `Success`: Đăng nhập thành công hay không.
    * `ErrorMessage`: Thông báo lỗi chi tiết khi thất bại.
    * `AccountId`: Mã tài khoản đã xác thực.
    * `FullName`: Tên đầy đủ của nhân sự kết nối với tài khoản này.
    * `Roles`: Danh sách các quyền của tài khoản.

---

### 💾 Tầng 3: Infrastructure (`HRMS.Infrastructure`)
Cài đặt chi tiết giao diện xác thực bằng việc kết nối trực tiếp đến cơ sở dữ liệu thông qua EF Core DbContext và xử lý mã hóa mật khẩu.

* **File:** [AuthService.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.Infrastructure/Services/AuthService.cs)
  * **Nhiệm vụ:** Triển khai logic từ `IAuthService`:
    1. Truy vấn tài khoản kèm thông tin `User` và `Roles` bằng `.Include` trong EF Core.
    2. Kiểm tra `Status` của tài khoản, nếu là `false` lập tức trả về lỗi tài khoản bị khóa.
    3. Sử dụng `BCrypt.Net.BCrypt.Verify(password, account.PasswordHash)` để so khớp mật khẩu.
    4. Gom danh sách quyền: `account.Roles.Select(r => r.Name).ToList()`.
    5. Đổi mật khẩu: Mã hóa mật khẩu mới bằng `BCrypt.Net.BCrypt.HashPassword(newPassword)` và lưu xuống Database.

---

### 🖥️ Tầng 4: WebUI (`HRMS.WebUI`)
Nhận thông tin từ người dùng qua biểu mẫu giao diện, xử lý luồng ghi cookie và cấu hình Middleware bảo mật.

* **File:** [Login.razor](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Components/Pages/Auth/Login.razor)
  * **Nhiệm vụ:** Cung cấp biểu mẫu đăng nhập đẹp mắt (sử dụng kính mờ - Glassmorphic design). Khi submit biểu mẫu thành công:
    1. Gọi `AuthService.LoginAsync` để nhận kết quả.
    2. Chuyển thông tin đăng nhập thành các `Claim` (ID, FullName, Roles).
    3. Đẩy `ClaimsPrincipal` vào bộ nhớ tạm `TempTokenStore` và nhận về mã token ngẫu nhiên.
    4. Gọi `NavigationManager.NavigateTo("/auth/signin?token=...", forceLoad: true)` để tải lại trình duyệt sang endpoint ghi cookie.
* **File:** [TempTokenStore.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Services/TempTokenStore.cs)
  * **Nhiệm vụ:** Lưu trữ tạm thời danh sách các `ClaimsPrincipal` đã được xác thực bằng `ConcurrentDictionary<string, ClaimsPrincipal>`. Tự động xóa token sau 30 giây để tránh rò rỉ bộ nhớ.
* **File:** [Program.cs](file:///d:/Dev_Web/HRMS/HRMS/HRMS.WebUI/Program.cs)
  * **Nhiệm vụ:** 
    * Cấu hình Cookie Authentication dịch vụ trong ứng dụng với tên cookie `HRMS_AuthCookie`, đường dẫn trang đăng nhập mặc định là `/login`, thời gian sống là 8 tiếng.
    * Đăng ký dịch vụ `TempTokenStore` dưới dạng Singleton.
    * Thiết lập 2 API endpoint để ghi/xóa cookie:
      * `/auth/signin`: Nhận token, lấy `ClaimsPrincipal` từ `TempTokenStore` và gọi `httpContext.SignInAsync(...)` để ghi Cookie thực xuống trình duyệt, sau đó redirect về `/`.
      * `/auth/logout`: Gọi `httpContext.SignOutAsync(...)` để xóa Cookie đăng nhập và redirect về `/login`.

---

## 3. Liên Kết Luồng & Cách Các Tầng Phối Hợp

Sơ đồ tuần tự thể hiện sự liên kết giữa các tầng khi người dùng đăng nhập:

```
[Trình duyệt]         [WebUI Layer]         [Application]         [Infrastructure]         [Database]
      |                     |                     |                      |                     |
      |-- 1. Nhập U/P ----->|                     |                      |                     |
      |   Bấm đăng nhập     |                     |                      |                     |
      |                     |-- 2. Gọi Login ---->|                      |                     |
      |                     |   (IAuthService)    |-- 3. Gọi DbContext ->|                     |
      |                     |                     |                      |-- 4. Query Account->|
      |                     |                     |                      |<-- 5. Trả về Account|
      |                     |                     |<-- 6. Trả kết quả ---|                     |
      |                     |                     |   (AuthResult DTO)   |                     |
      |                     |<-- 7. Trả AuthResult|                      |                     |
      |                     |                     |                      |                     |
      |                     |-- 8. Lưu Principal  |                      |                     |
      |                     |   vào TempTokenStore|                      |                     |
      |                     |<-- Trả về tempToken |                      |                     |
      |<-- 9. Redirect -----|                     |                      |                     |
      |   /auth/signin?token|                     |                      |                     |
      |                     |                     |                      |                     |
      |-- 10. GET Signin -->|                     |                      |                     |
      |   (HTTP Endpoint)   |-- 11. Đọc Principal |                      |                     |
      |                     |   từ TempTokenStore |                      |                     |
      |                     |-- 12. Ghi Cookie ---|                      |                     |
      |                     |   (SignInAsync)     |                      |                     |
      |<-- 13. Redirect / --|                     |                      |                     |
```
