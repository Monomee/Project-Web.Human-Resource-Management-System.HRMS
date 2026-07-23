using HRMS.Application.DTOs.TimePeriod;
using HRMS.Application.DTOs.Attendance;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Interface quản lý kỳ công (TimesheetPeriod).
///
/// Tách riêng khỏi IAttendanceService vì đây là nghiệp vụ độc lập:
///   - IAttendanceService = xử lý dữ liệu chấm công (import, tính công)
///   - ITimePeriodService = quản lý kỳ công (CRUD kỳ công)
/// </summary>
public interface ITimePeriodService
{
    /// <summary>
    /// Lấy toàn bộ danh sách kỳ công, sắp xếp theo StartDate giảm dần (mới nhất trước).
    /// </summary>
    Task<List<TimesheetPeriodDto>> GetAllAsync();

    /// <summary>
    /// Lấy thông tin chi tiết một kỳ công theo Id.
    /// Trả về null nếu không tìm thấy.
    /// </summary>
    Task<TimesheetPeriodDto?> GetByIdAsync(int id);

    /// <summary>
    /// Tạo mới một kỳ công.
    /// Nghiệp vụ: validate tên không trùng và khoảng ngày không chồng lấp với kỳ khác.
    /// </summary>
    /// <param name="dto">Dữ liệu kỳ công cần tạo.</param>
    /// <returns>Id của kỳ công vừa tạo.</returns>
    Task<int> CreateAsync(CreateTimesheetPeriodDto dto);

    /// <summary>
    /// Cập nhật thông tin kỳ công.
    /// Nghiệp vụ: không cho phép sửa nếu kỳ đã bị khóa (IsLocked = true).
    /// </summary>
    /// <param name="id">Id của kỳ công cần sửa.</param>
    /// <param name="dto">Dữ liệu mới.</param>
    Task UpdateAsync(int id, UpdateTimesheetPeriodDto dto);

    /// <summary>
    /// Khóa kỳ công (IsLocked = true).
    /// </summary>
    Task LockPeriodAsync(int id);

    /// <summary>
    /// Mở khóa kỳ công (IsLocked = false).
    /// </summary>
    Task UnlockPeriodAsync(int id);
}
