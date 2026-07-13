using HRMS.Application.DTOs.Attendance;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Interface định nghĩa hợp đồng cho dịch vụ đọc file Excel.
/// - Interface này nằm ở tầng Application (không biết về EPPlus).
/// - Lớp ExcelParserService ở tầng Infrastructure sẽ cài đặt (implement) interface này.
/// → Đây là kỹ thuật "Dependency Inversion" trong Clean Architecture:
///   tầng Application chỉ phụ thuộc vào Interface, không phụ thuộc vào thư viện cụ thể.
/// </summary>
public interface IExcelParserService
{
    /// <summary>
    /// Đọc luồng Stream của file Excel và trả về danh sách các dòng dữ liệu thô.
    /// </summary>
    /// <param name="fileStream">Luồng byte của file .xlsx tải lên từ trình duyệt.</param>
    /// <returns>Danh sách AttendanceRowDto, mỗi phần tử tương ứng 1 dòng trong Excel.</returns>
    Task<List<AttendanceRowDto>> ParseAsync(Stream fileStream);
}
