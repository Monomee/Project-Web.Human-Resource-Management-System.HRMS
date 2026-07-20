using System;

namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Ghi nhận số giờ OT thực tế của nhân viên theo ngày, dùng cho module tính lương.
    /// Được hệ thống tự sinh khi đơn OT được duyệt (Approved).
    /// </summary>
    public class OvertimeRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateOnly WorkDate { get; set; }
        public decimal Hours { get; set; }

        // Liên kết ngược lại đơn gốc để truy vết
        public int SourceRequestId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}