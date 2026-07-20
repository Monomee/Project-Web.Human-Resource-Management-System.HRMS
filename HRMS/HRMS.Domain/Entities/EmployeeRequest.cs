using System;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Bảng đơn dùng chung cho 3 loại: Nghỉ phép, OT, Khiếu nại công.
    /// Các trường riêng theo loại đơn để nullable, chỉ dùng field tương ứng với loại đơn (RequestType).
    /// </summary>
    public class EmployeeRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        /// <summary>FK tới bảng RequestTypes có sẵn trong DB.</summary>
        public int RequestTypeId { get; set; }

        public RequestType? RequestType { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Draft;

        // Lý do / ghi chú chung
        public string? Reason { get; set; }

        // ---- Dữ liệu riêng cho đơn NGHỈ PHÉP ----
        public DateOnly? LeaveStartDate { get; set; }
        public DateOnly? LeaveEndDate { get; set; }
        public decimal? LeaveDays { get; set; } // số ngày công nghỉ (hỗ trợ nghỉ nửa ngày = 0.5)

        // ---- Dữ liệu riêng cho đơn OT ----
        public DateOnly? OvertimeDate { get; set; }
        public decimal? OvertimeHours { get; set; }

        // ---- Dữ liệu riêng cho đơn KHIẾU NẠI CÔNG ----
        public DateOnly? ComplaintWorkDate { get; set; }
        public decimal? ComplaintProposedHours { get; set; } // số giờ công nhân viên đề nghị điều chỉnh lại

        // ---- Thông tin quy trình duyệt ----
        public int? ApproverId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApproverNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; } // thời điểm chuyển Draft -> Pending
        public DateTime? UpdatedAt { get; set; }
    }
}