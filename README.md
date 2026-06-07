
# Project-Web.Human-Resource-Management-System.HRMS

  

### Chào mấy con vợ: Khi vào dự án này, hãy ĐẶC BIỆT chú ý tới những file sau:

1. Docs: https://docs.google.com/document/d/1dT4ZyOBEbMBzGvgAXXYbpmLpOL4IR4lEg6m9SkzpQ1Y/edit?usp=sharing

-> Đây là file document dự án, nơi mà phác thảo soạn lên, nơi mà các tính năng sẽ làm.

2. structure.md

-> Cấu trúc dự án

3. guideline.md

-> Quy trình và ma thuật để làm dự án

4. Agent.md

-> Bộ quy tắc cho agent để code

5. DatabaseInfo.md

-> Khởi tạo database cho dự án

6. HRMS.md

-> File tasks của dự án

  

### Và đây là mấy thứ cũng ĐẶC BIỆT cần để ý:

1. <strong>Về git/commit và mấy thứ liên quan</strong>

- Pull project về và <strong>TẠO</strong> nhánh mới theo cấu trúc: \<tên\>/\<feature\>. Ví dụ: hoangnv/auth

- <strong>Quy trình</strong> để làm việc với nhau thông qua git (yêu cầu mọi người làm theo để thuận lợi nhé):

Muốn làm cái gì mới -> Pull từ main về nhánh local của mình -> Do stuff -> Test and confirm -> Pull tiếp từ main về nhánh local của mình -> Resolve conflict (nếu có) -> Push lên nhánh của mình trên github -> Request Leader/other member to merge -> Merge vào main -> Delete branch của mình (nếu muốn hoặc nếu không dùng nữa)

- Commit convention: ghi theo cấu trúc \<type\>: \<description\>. Ví dụ: feat: add authentication

  + feat: tính năng

  + docs: tài liệu

  + fix: sửa lỗi

  + refactor: sửa mà không sửa logic code/nghiệp vụ (như là đổi tên biến, hàm...)

  + chore: những thứ nhỏ nhặt không liên quan code

  + vendor: cập nhật version cho các dependencies, packages

2. <strong>Về nhiệm vụ và nghiệp vụ</strong>

- <strong>Hãy khởi tạo database và thêm mẫu</strong> theo những gì đã có trong DatabaseInfo.md

- <strong>Thay đổi phần thông tin kết nối trong appsetting.json</strong> để có thể sử dụng và code

- Đọc kĩ tasks, <strong>đừng "mù quáng"</strong> copy & paste prompt, làm ơn hiểu nghiệp vụ để thay đổi prompt cho đúng với nghiệp vụ, <strong>ĐỪNG</strong> bảo xong rồi mà nghiệp vụ SAI

- Hãy luôn <strong>test kĩ trước khi đóng tasks</strong>, nhớ lấy trách nhiệm của mình  

- Có bất cứ vấn đề gì, làm ơn hãy nhắn vào nhóm chat và @người cần nói hoặc @all, <strong>đừng tự nghĩ và tự làm trong tình trạng không chắc chắn</strong>