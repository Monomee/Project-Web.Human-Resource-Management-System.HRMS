namespace HRMS.Application.DTOs.Attendance;

/// <summary>
/// DTO chứa KẾT QUẢ tính toán sau khi xử lý 1 ngày làm việc của 1 nhân viên.
/// Đây là dữ liệu được hiển thị lên bảng kết quả trên giao diện ImportAttendance.razor.
/// </summary>
public class AttendanceImportResultDto
{
    /// <summary>Mã nhân viên.</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>Họ tên nhân viên (lấy từ bảng Users).</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Ngày làm việc được tính công.</summary>
    public DateOnly WorkDate { get; set; }

    /// <summary>Giờ vào đầu tiên trong ngày (null = không có dữ liệu quẹt vào).</summary>
    public TimeOnly? CheckIn { get; set; }

    /// <summary>Giờ ra cuối cùng trong ngày (null = không quẹt ra).</summary>
    public TimeOnly? CheckOut { get; set; }

    /// <summary>
    /// Số phút đi muộn so với 08:00.
    /// Ví dụ: vào lúc 08:15 → LateMinutes = 15.
    /// Nếu vào đúng giờ hoặc sớm hơn 08:00 → LateMinutes = 0.
    /// </summary>
    public int LateMinutes { get; set; }

    /// <summary>
    /// Giá trị ngày công được tính:
    /// - 1.0 = đủ ngày công
    /// - 0.5 = nửa ngày (về sớm, chỉ làm sáng, hoặc thiếu OUT)
    /// - 0.0 = vắng mặt (không có dữ liệu quẹt IN)
    /// </summary>
    public double WorkValue { get; set; }

    /// <summary>Ghi chú lý do tính công (ví dụ: "Đủ công", "Về sớm", "Đi muộn 15 phút").</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Cờ đánh dấu dòng này có lỗi không (ví dụ: mã nhân viên không tồn tại trong DB).
    /// Nếu HasError = true, dòng này sẽ KHÔNG được lưu vào AttendanceLogs.
    /// </summary>
    public bool HasError { get; set; }
}
