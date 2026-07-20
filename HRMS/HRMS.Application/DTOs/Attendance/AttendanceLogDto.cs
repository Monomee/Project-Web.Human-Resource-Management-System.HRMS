namespace HRMS.Application.DTOs.Attendance;

/// <summary>
/// DTO đọc thông tin một bản ghi chấm công (AttendanceLog).
/// Dùng để hiển thị danh sách lên UI — bao gồm tên nhân viên để dễ đọc.
/// </summary>
public class AttendanceLogDto
{
    /// <summary>Id của bản ghi AttendanceLog.</summary>
    public int Id { get; set; }

    /// <summary>Thời điểm quẹt thẻ (ngày + giờ).</summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>Loại quẹt: "IN" hoặc "OUT".</summary>
    public string CheckType { get; set; } = string.Empty;

    /// <summary>Nguồn dữ liệu: "Excel" hoặc "Manual" (nhập tay).</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Id của nhân viên.</summary>
    public int UserId { get; set; }

    /// <summary>Mã nhân viên (EmployeeCode từ bảng Users).</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>Họ tên nhân viên (FullName từ bảng Users).</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Id của kỳ công chứa bản ghi này.</summary>
    public int PeriodId { get; set; }
}
