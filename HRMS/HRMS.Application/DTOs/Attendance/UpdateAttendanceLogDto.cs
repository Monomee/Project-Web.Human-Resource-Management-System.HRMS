using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.Attendance;

/// <summary>
/// DTO dùng khi HR muốn chỉnh sửa thủ công một bản ghi chấm công.
/// Chỉ cho phép sửa giờ quẹt thẻ và loại quẹt (IN/OUT).
/// Không cho phép đổi UserId hay PeriodId vì đó là dữ liệu định danh.
/// </summary>
public class UpdateAttendanceLogDto
{
    /// <summary>
    /// Thời điểm quẹt thẻ mới (ngày + giờ).
    /// Dùng DateTime thay vì DateOnly để giữ đủ thông tin giờ phút.
    /// </summary>
    [Required(ErrorMessage = "Thời điểm quẹt thẻ không được để trống.")]
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Loại quẹt: phải là "IN" hoặc "OUT".
    /// </summary>
    [Required(ErrorMessage = "Loại quẹt thẻ không được để trống.")]
    [RegularExpression("^(IN|OUT)$", ErrorMessage = "Loại quẹt thẻ chỉ được là 'IN' hoặc 'OUT'.")]
    public string CheckType { get; set; } = string.Empty;
}
