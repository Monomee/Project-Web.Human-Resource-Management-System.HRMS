namespace HRMS.Application.DTOs.Attendance;

/// <summary>
/// DTO ánh xạ 1:1 với từng dòng dữ liệu thô trong file Excel từ máy chấm công.
/// Mỗi dòng Excel = 1 lần quẹt thẻ của nhân viên.
/// </summary>
public class AttendanceRowDto
{
    /// <summary>Mã nhân viên (cột EmployeeCode trong Excel). Dùng để tra UserId trong DB.</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>Thời điểm quẹt thẻ đầy đủ (ngày + giờ phút giây).</summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>Loại quẹt thẻ: "IN" (vào) hoặc "OUT" (ra).</summary>
    public string CheckType { get; set; } = string.Empty;
}
