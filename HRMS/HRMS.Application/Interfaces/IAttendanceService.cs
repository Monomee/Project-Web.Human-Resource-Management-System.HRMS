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
