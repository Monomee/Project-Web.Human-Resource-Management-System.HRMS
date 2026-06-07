# **🏗️ PHẦN 1: KHỞI TẠO NỀN MÓNG (FOUNDATION PHASE)**

### **TASK 1.1: Tạo Solution và Cấu trúc dự án Clean Architecture**

* **Mục tiêu:** Tạo ra một "bộ khung" thư mục chuẩn chỉnh để code không bị rối và tách biệt hoàn toàn trách nhiệm giữa các tầng.  
* **Công nghệ sử dụng:** Visual Studio 2022 (hoặc VS Code), .NET 8.0/9.0 SDK.  
* **Cấu trúc thư mục:** Tạo 1 Blank Solution tên là HRMS.Solution, bên trong tạo các thư mục vật lý và các Project con như sau:  
  1. 1.Core/HRMS.Domain (Class Library)  
  2. 1.Core/HRMS.Application (Class Library)  
  3. 2.Infrastructure/HRMS.Infrastructure (Class Library)  
  4. 3.Presentation/HRMS.WebUI (Blazor Web App)  
* **Quy trình các bước thực hiện:**  
  1. Mở Visual Studio, chọn **Create a new project** \-\> Chọn **Blank Solution**, đặt tên là HRMS.Solution.  
  2. Chuột phải vào Solution \-\> **Add** \-\> **New Project** \-\> Chọn **Class Library** (.NET 8/9) để tạo lần lượt 3 dự án: HRMS.Domain, HRMS.Application, HRMS.Infrastructure.  
  3. Chuột phải vào Solution \-\> **Add** \-\> **New Project** \-\> Chọn **Blazor Web App**. Cấu hình chính xác: *Interactive render mode \= Server*, *Interactivity location \= Per page/component*, *Authentication type \= None*. Đặt tên là HRMS.WebUI.  
  4. **Thiết lập tham chiếu (Project Reference) theo đúng quy tắc một chiều:**  
     * Chuột phải vào HRMS.Application \-\> Add Reference đến HRMS.Domain.  
     * Chuột phải vào HRMS.Infrastructure \-\> Add Reference đến HRMS.Application và HRMS.Domain.  
     * Chuột phải vào HRMS.WebUI \-\> Add Reference đến HRMS.Infrastructure và HRMS.Application.

### **TASK 1.2: Kỹ thuật Đảo ngược Database (EF Core Database First Scaffold)**

* **Mục tiêu:** Tự động sinh ra các bảng dữ liệu cũ thành code C\# trong 5 giây mà không phải viết tay.  
* **Công nghệ sử dụng:** Entity Framework Core, Thư viện Nuget Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Tools.  
* **Cấu trúc tác động:** Chạy từ HRMS.Infrastructure, đầu ra sẽ nằm ở HRMS.Domain và HRMS.Infrastructure.  
* **Quy trình các bước thực hiện:**  
  1. Mở **Nuget Package Manager Console** trong Visual Studio.  
  2. Cài đặt các thư viện cần thiết vào tầng HRMS.Infrastructure và HRMS.WebUI.  
  3. Chạy lệnh Scaffold để quét toàn bộ Database cũ thành Code (Thay chuỗi ConnectionString bằng DB thực tế):

Bash

dotnet ef dbcontext scaffold "Server=YOUR\_SERVER;Database=HRMS\_DB;Trusted\_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer \--project 2.Infrastructure/HRMS.Infrastructure \--startup-project 3.Presentation/HRMS.WebUI \--output-dir Persistence/ScaffoldedModels \--context ApplicationDbContext \--context-dir Persistence \--force

4. **Tái cấu trúc (Refactoring) theo chuẩn Clean Architecture:**  
   * Vào thư mục HRMS.Infrastructure/Persistence/ScaffoldedModels, quét chọn toàn bộ các file thực thể (ví dụ: Employee.cs, Request.cs...) và **Kéo thả (Move)** chúng sang thư mục 1.Core/HRMS.Domain/Entities/.  
     * Mở các file Entity đó ra, đổi lại dòng trên cùng từ namespace HRMS.Infrastructure.Persistence.ScaffoldedModels; thành namespace HRMS.Domain.Entities;.  
     * Mở file ApplicationDbContext.cs (vẫn ở tầng Infra) ra, thêm using HRMS.Domain.Entities; và sửa lại các đường dẫn bị báo lỗi đỏ.

### **TASK 1.3: Cấu hình Tập trung Dependency Injection (DI) & AppSettings**

* **Mục tiêu:** Cấu hình toàn bộ hệ thống để các tầng có thể "gọi" được nhau qua cơ chế DI của .NET, chuẩn bị các thông số cấu hình.  
* **Công nghệ sử dụng:** C\# Extension Methods, Microsoft.Extensions.DependencyInjection.  
* **Cấu trúc tác động:** HRMS.Infrastructure, HRMS.Application, và file Program.cs của HRMS.WebUI.  
* **Quy trình các bước thực hiện:**  
  1. Trong HRMS.WebUI, mở file appsettings.json, điền thông tin chuỗi kết nối Database vào mục "ConnectionStrings": { "DefaultConnection": "..." }.  
  2. Tại tầng HRMS.Infrastructure, tạo file DependencyInjection.cs, viết một hàm static mở rộng AddInfrastructureServices để đăng ký ApplicationDbContext và các Repository (Mẫu code đã được Senior cung cấp ở buổi thảo luận trước).  
  3. Tại tầng HRMS.Application, tạo file DependencyInjection.cs, viết hàm mở rộng AddApplicationServices để đăng ký các Service xử lý nghiệp vụ sau này.  
  4. Mở file Program.cs tại tầng HRMS.WebUI, gọi hai hàm mở rộng này ra bằng lệnh: builder.Services.AddInfrastructureServices(builder.Configuration); và builder.Services.AddApplicationServices();.

# **🔐 PHẦN 2: BẢO MẬT VÀ GIAO DIỆN CƠ SỞ (SECURITY & CORE UI)**

### **TASK 2.1: Cấu hình Hệ thống Xác thực Cookie & Phân quyền (Authentication & Authorization)**

* **Mục tiêu:** Thay thế cho hệ thống HttpSession thủ công của Java Servlet bằng hệ thống Bảo mật Cookie an toàn của .NET để đăng nhập và phân quyền (Admin, HRM, HR, Employee).  
* **Công nghệ sử dụng:** Microsoft.AspNetCore.Authentication.Cookies, Thư viện mã hóa mật khẩu BCrypt.Net-Next.  
* **Cấu trúc tác động:** HRMS.Application (Logic Login), HRMS.WebUI (Giao diện màn hình Login, Middleware).  
* **Quy trình các bước thực hiện:**  
  1. Mở file Program.cs ở HRMS.WebUI, thêm đoạn cấu hình builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...) để định nghĩa trang /login mặc định và thời gian hết hạn cookie.  
  2. Thêm app.UseAuthentication(); và app.UseAuthorization(); vào pipeline (phải đặt trước app.MapRazorComponents).  
  3. Tại tầng HRMS.Application, viết AuthService.cs thực hiện kiểm tra Username từ Database, dùng BCrypt.Net để so khớp mật khẩu đã băm (hashed password). Nếu đúng, tạo ra các Claims (gồm UserId, FullName, Role) và đóng gói vào Cookie.  
  4. Tạo trang Login.razor trong HRMS.WebUI/Pages/Auth/. Sử dụng EditForm của Blazor để thu thập tài khoản, gọi AuthService để xử lý đăng nhập.

### **TASK 2.2: Xây dựng Giao diện Gốc (Main Layout & Navigation) và Chế độ Render**

* **Mục tiêu:** Tạo menu điều hướng thông minh. Nhân viên thường chỉ thấy phần gửi đơn, xem lương; HR/Admin thấy toàn bộ hệ thống quản trị.  
* **Công nghệ sử dụng:** Blazor Razor Components, Bootstrap (hoặc thư viện UI như MudBlazor/Radzen nếu có sử dụng).  
* **Cấu trúc tác động:** HRMS.WebUI/Components/Layout/  
* **Quy trình các bước thực hiện:**  
  1. Mở file NavMenu.razor. Sử dụng thẻ \<AuthorizeView Roles="Admin,HRM,HR"\> để bao bọc các tính năng quản lý cao cấp như "Tính lương", "Cấu hình hệ thống".  
  2. Sử dụng \<AuthorizeView\> (mặc định cho tất cả user đã đăng nhập) để hiển thị các menu cá nhân: "Xem phiếu lương", "Tạo yêu cầu nghỉ phép".  
  3. Thiết lập file App.razor để làm gốc cho toàn bộ ứng dụng, đảm bảo thẻ \<Routes /\> hoạt động đúng.

# **⚙️ PHẦN 3: TRIỂN KHAI CÁC MODULE NGHIỆP VỤ CHÍNH (CORE MODULES \- FEATURE-BASED)**

*Mẹo từ Senior:* Với cách làm Clean Architecture, mỗi module dưới đây em hãy làm theo chu trình: **Tạo Interface ở Domain \-\> Viết logic ở Application \-\> Cài đặt Repository ở Infra \-\> Viết UI ở WebUI**.

### **TASK 3.1: Module Quản lý Hồ sơ & Hợp đồng (Employee & Contract)**

* **Mục tiêu:** Xem, sửa thông tin nhân sự và theo dõi thời hạn hợp đồng (Thử việc, Chính thức).  
* **Cấu trúc tác động:** Toàn bộ 4 tầng. Trang UI đặt tại HRMS.WebUI/Pages/Employee/.  
* **Quy trình các bước thực hiện:**  
  1. Tại HRMS.Application, tạo thư mục Features/Employees/. Viết class EmployeeService.cs chứa các hàm: GetEmployeeProfile(int id), UpdateEmployee(...).  
  2. Tại HRMS.WebUI, viết trang EmployeeList.razor (Chỉ dành cho HR). Sử dụng các bảng dữ liệu để hiển thị danh sách.  
  3. Áp dụng Render Mode cho trang quản trị: Thêm dòng @rendermode InteractiveServer ở đầu trang để các tính năng tìm kiếm, lọc dữ liệu, bấm nút Sửa/Xóa hoạt động mượt mà không bị tải lại trang.

### **TASK 3.2: Module Yêu cầu & Quy trình Phê duyệt (Request Workflow)**

* **Mục tiêu:** Nhân viên gửi đơn (Nghỉ phép, Làm thêm giờ OT, Khiếu nại công). Quản lý duyệt đơn, trạng thái đơn tự động chuyển đổi (Draft \-\> Pending \-\> Approved/Rejected).  
* **Cấu trúc tác động:** HRMS.Domain (chứa Enum trạng thái), HRMS.Application (chứa logic chuyển trạng thái), HRMS.WebUI/Pages/Requests/.  
* **Quy trình các bước thực hiện:**  
  1. Trong HRMS.Application, thiết kế hàm SubmitRequest(RequestDto model). Logic phải kiểm tra: Nếu là đơn xin nghỉ phép, phải check xem nhân viên đó còn đủ số ngày nghỉ phép trong năm không.  
  2. Viết hàm ApproveRequest(int requestId, int approverId). Khi sếp bấm Duyệt, trạng thái đơn chuyển thành Approved, đồng thời hệ thống phải tự động bắn một bản ghi cập nhật vào bảng dữ liệu tương ứng (Ví dụ: nếu duyệt đơn OT, hệ thống sẽ ghi nhận số giờ OT vào ngày đó để cuối tháng tính lương).  
  3. Trên UI, tạo trang MyRequests.razor cho nhân viên tự theo dõi và ApprovalList.razor cho quản lý bấm nút Duyệt/Từ chối real-time.

### **TASK 3.3: Module Chấm công & Đọc file Excel từ máy chấm công (Attendance Engine)**

* **Mục tiêu:** Cho phép HR tải file Excel thô kết xuất từ máy quẹt thẻ lên hệ thống, tính toán số ngày công, đi muộn về sớm, và Khóa kỳ công (Lock period).  
* **Công nghệ sử dụng:** Thư viện Nuget xử lý Excel EPPlus (hoặc ClosedXML).  
* **Cấu trúc tác động:** HRMS.Infrastructure (Nơi viết code đọc file Excel), HRMS.Application (Logic tính toán công).  
* **Quy trình các bước thực hiện:**  
  1. Tại HRMS.Infrastructure, cài đặt thư viện EPPlus. Viết class ExcelParserService.cs triển khai một Interface từ tầng Application. Hàm này có nhiệm vụ nhận vào một Stream file, duyệt từng dòng trong Excel, đọc các cột (Mã nhân viên, Ngày, Giờ check-in, Giờ check-out) để map vào Class DTO C\#.  
  2. Tại HRMS.Application, viết logic so khớp giờ check-in/out với khung giờ làm việc quy định của công ty để tính ra: Hôm đó được 1 công hay 0.5 công, nhân viên có đi muộn bao nhiêu phút hay không.  
  3. Tại HRMS.WebUI, tạo trang ImportAttendance.razor. Sử dụng component \<InputFile\> của Blazor để HR chọn file Excel từ máy tính, bấm "Xử lý" \-\> File đẩy thẳng lên Server, chạy ngầm tính toán và hiển thị kết quả trực quan lên màn hình.

### **TASK 3.4: Module Tính lương & Phiếu lương tự động (Payroll & Payslips Engine)**

* **Mục tiêu:** Tự động tính toán bảng lương tổng dựa trên kỳ công đã được Khóa. Áp các công thức Thuế TNCN, Bảo hiểm bắt buộc theo luật Việt Nam. Xuất phiếu lương (Payslip) cá nhân.  
* **Cấu trúc tác động:** HRMS.Application/Features/Payroll/, HRMS.WebUI/Pages/Payroll/.  
* **Quy trình các bước thực hiện:**  
  1. Tại HRMS.Application, viết hàm CalculateMonthlyPayroll(int periodId). Logic: Lấy toàn bộ dữ liệu chấm công đã khóa của kỳ đó \-\> Nhân với Lương cơ bản trong Hợp đồng \-\> Cộng lương OT (đã nhân hệ số 1.5 hoặc 2.0) \-\> Trừ tiền đi muộn/nghỉ không phép \-\> Áp công thức lũy tiến Thuế thu nhập cá nhân và trừ 10.5% bảo hiểm xã hội của người lao động.  
  2. Sau khi bảng lương tổng được HRM duyệt, hệ thống tự động sinh ra các bản ghi trong bảng Payslip cho từng nhân sự.  
  3. Tại HRMS.WebUI, tạo trang MyPayslip.razor cho nhân viên. Sử dụng CSS @media print để định dạng trang này đẹp mắt, giúp nhân viên có thể bấm Ctrl \+ P để tải về hoặc in file PDF phiếu lương của mình trực tiếp từ trình duyệt.

# **🚀 PHẦN 4: TÍNH NĂNG NÂNG CAO VÀ MỞ RỘNG (ADVANCED FEATURES)**

### **TASK 4.1: Trang Tuyển dụng Công khai (Public Job Site với Static SSR)**

* **Mục tiêu:** Hiển thị danh sách tin tuyển dụng của công ty ra ngoài cho ứng viên vãng lai xem và nộp hồ sơ mà không làm tốn tài nguyên Server.  
* **Cấu trúc tác động:** HRMS.WebUI/Pages/Public/  
* **Quy trình các bước thực hiện:**  
  1. Tạo trang Jobs.razor và JobDetail.razor nằm ngoài phân vùng bảo mật (không yêu cầu Đăng nhập).  
  2. **Rất quan trọng:** Đối với các trang này, em **KHÔNG THÊM** dòng @rendermode InteractiveServer ở đầu file. Hãy để nó chạy ở chế độ mặc định là **Static SSR (Server-Side Rendering)**.  
  3. Khi ứng viên truy cập, Server chỉ render mã HTML thuần túy rồi gửi về trình duyệt. Trang tải cực kỳ nhanh, chuẩn SEO (Google Search có thể quét được tin tuyển dụng của công ty) và không duy trì kết nối SignalR, giúp Server chịu tải được hàng ngàn ứng viên cùng lúc.

### **TASK 4.2: Tích hợp Trợ lý ảo Chatbot AI hướng dẫn hệ thống**

* **Mục tiêu:** Nhân viên có thể chat với AI để hỏi về quy định công ty hoặc cách sử dụng phần mềm.  
* **Công nghệ sử dụng:** OpenAI SDK cho .NET (hoặc thư viện Microsoft.SemanticKernel).  
* **Cấu trúc tác động:** HRMS.Infrastructure (Kết nối API OpenAI), HRMS.WebUI/Pages/Chat/.  
* **Quy trình các bước thực hiện:**  
  1. Tại HRMS.Infrastructure, viết OpenAIChatbotService.cs sử dụng OpenAI Client kết nối với mô hình (ví dụ gpt-4o-mini). Sử dụng kỹ thuật System Prompt để ép AI đóng vai làm "Trợ lý nhân sự nội bộ công ty".  
  2. Tại HRMS.WebUI, tạo một Component giao diện nhỏ tên là ChatWindow.razor đặt ở góc dưới màn hình. Trang này bắt buộc phải dùng @rendermode InteractiveServer.  
  3. Sử dụng luồng dữ liệu thời gian thực (SignalR có sẵn của Blazor Server) để khi AI trả lời đến đâu, các từ (tokens) sẽ được hiển thị hiệu ứng "gõ chữ" (streaming) mượt mà lên màn hình của người dùng đến đó, mang lại trải nghiệm rất hiện đại.

# **🛠️ PHẦN 5: ĐỔI CƠ CHẾ VÀ HOÀN THIỆN (MIGRATION & GO-LIVE)**

### **TASK 5.1: Chuyển dịch hệ thống hoàn toàn sang Code First Migrations**

* **Mục tiêu:** Sau khi đã chạy xong Database First lần đầu, từ bước này trở đi chúng ta đóng băng việc sửa DB thủ công trên SQL Server và chuyển hẳn sang quản lý bằng Code.  
* **Quy trình các bước thực hiện:**  
  1. Khi có yêu cầu mới (ví dụ: Thêm cột SkypeId vào bảng Employee), em mở file Employee.cs ở tầng HRMS.Domain/Entities/ ra và gõ thêm thuộc tính: public string? SkypeId { get; set; }.  
  2. Mở Terminal tại thư mục gốc Solution, chạy lệnh tạo file Migration mới:

Bash

dotnet ef migrations add AddSkypeToEmployee \--project 2.Infrastructure/HRMS.Infrastructure \--startup-project 3.Presentation/HRMS.WebUI

3. Chạy lệnh cập nhật cấu trúc đó lên SQL Server:

Bash

dotnet ef database update \--project 2.Infrastructure/HRMS.Infrastructure \--startup-project 3.Presentation/HRMS.WebUI

### **TASK 5.2: Tối ưu hiệu năng và Kiểm thử Tích hợp (Clean up & Go-live)**

* **Mục tiêu:** Đóng gói, dọn dẹp code rác và chuẩn bị chạy thực tế.  
* **Quy trình các bước thực hiện:**  
  1. Rà soát lại toàn bộ các câu lệnh gọi Database, đảm bảo có sử dụng các hàm Async (ví dụ: ToListAsync(), SaveChangesAsync()) để tránh làm nghẽn luồng xử lý của Server.  
  2. Cấu hình môi trường Production trong file appsettings.Production.json (tắt chế độ hiện lỗi chi tiết của Developer để bảo mật thông tin hệ thống).  
  3. Đóng gói ứng dụng thành Docker Image hoặc Deploy trực tiếp lên IIS/Cloud. Hệ thống chính thức đi vào hoạt động\!

