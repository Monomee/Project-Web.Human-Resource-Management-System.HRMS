using HRMS.Application.DTOs.Attendance;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Interface định nghĩa các nghiệp vụ xử lý chấm công.
/// Tầng WebUI sẽ gọi interface này — không cần biết cài đặt bên trong.
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Nhận file Excel từ HR, xử lý toàn bộ: parse → tính công → lưu DB.
    /// </summary>
    /// <param name="fileStream">Luồng byte của file Excel.</param>
    /// <param name="periodId">ID của kỳ công (TimesheetPeriod) mà HR đang import vào.</param>
    /// <returns>
    /// Danh sách kết quả để hiển thị lên UI, gồm cả các dòng lỗi (HasError = true)
    /// để HR biết dòng nào không xử lý được.
    /// </returns>
    Task<List<AttendanceImportResultDto>> ImportAndSaveAsync(Stream fileStream, int periodId);

    /// <summary>
    /// Lấy danh sách tất cả các kỳ công để hiển thị dropdown trên UI.
    /// </summary>
    Task<List<TimesheetPeriodDto>> GetPeriodsAsync();

    /// <summary>
    /// Khóa kỳ công: đổi IsLocked = true để không ai có thể import thêm vào kỳ này.
    /// Sau khi khóa, module Payroll mới được phép tính lương cho kỳ đó.
    /// </summary>
    /// <param name="periodId">ID của kỳ công cần khóa.</param>
    Task LockPeriodAsync(int periodId);

    /// <summary>
    /// Lấy danh sách tất cả bản ghi chấm công (AttendanceLogs) của một kỳ công.
    /// Kết quả bao gồm tên + mã nhân viên để hiển thị lên bảng.
    /// </summary>
    /// <param name="periodId">ID kỳ công cần lọc.</param>
    Task<List<AttendanceLogDto>> GetLogsAsync(int periodId);

    /// <summary>
    /// Cập nhật thủ công một bản ghi chấm công.
    /// Nghiệp vụ: không cho phép sửa nếu kỳ công tương ứng đã bị khóa.
    /// </summary>
    /// <param name="logId">ID bản ghi AttendanceLog cần sửa.</param>
    /// <param name="dto">Dữ liệu mới (giờ quẹt thẻ, loại IN/OUT).</param>
    Task UpdateLogAsync(int logId, UpdateAttendanceLogDto dto);

    /// <summary>
    /// Lấy chi tiết một bản ghi chấm công theo Id (để phục vụ trang sửa).
    /// </summary>
    /// <param name="logId">ID bản ghi chấm công cần lấy.</param>
    Task<AttendanceLogDto?> GetLogByIdAsync(int logId);
}

/// <summary>DTO đơn giản cho dropdown chọn kỳ công.</summary>
public class TimesheetPeriodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsLocked { get; set; }
}
