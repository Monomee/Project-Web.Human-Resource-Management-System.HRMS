using System;

namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Ghi nhận điều chỉnh bảng công khi đơn khiếu nại công được duyệt.
    /// </summary>
    public class TimesheetAdjustment
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateOnly WorkDate { get; set; }
        public decimal AdjustedHours { get; set; }
        public string? Reason { get; set; }

        public int SourceRequestId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}