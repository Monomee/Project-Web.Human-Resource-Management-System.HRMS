using System;

namespace HRMS.Application.DTOs
{
    /// <summary>
    /// Dữ liệu đầu vào khi nhân viên tạo/gửi một đơn (Nghỉ phép / OT / Khiếu nại công).
    /// Các field theo loại đơn (Leave/Overtime/Complaint) dùng riêng cho UI/validate,
    /// khi lưu sẽ được map vào 3 cột chung của bảng Requests: StartDate, EndDate, Value.
    /// </summary>
    public class RequestDto
    {
        /// <summary>Accounts.Id của người tạo đơn (không phải Users.Id).</summary>
        public int AccountId { get; set; }

        /// <summary>Id tham chiếu tới bảng RequestTypes có sẵn trong DB.</summary>
        public int RequestTypeId { get; set; }

        /// <summary>Tuỳ chọn - nếu để trống hệ thống tự sinh Title (cột NOT NULL trong DB).</summary>
        public string? Title { get; set; }

        public string? Reason { get; set; }

        // Nghỉ phép
        public DateOnly? LeaveStartDate { get; set; }
        public DateOnly? LeaveEndDate { get; set; }

        // OT
        public DateOnly? OvertimeDate { get; set; }
        public decimal? OvertimeHours { get; set; }

        // Khiếu nại công
        public DateOnly? ComplaintWorkDate { get; set; }
        public decimal? ComplaintProposedHours { get; set; }

        /// <summary>
        /// true = gửi thẳng cho quản lý duyệt (Pending); false = chỉ lưu Draft.
        /// </summary>
        public bool SubmitImmediately { get; set; } = true;
    }

    /// <summary>Dùng để hiển thị dropdown chọn loại đơn trên UI, load từ bảng RequestTypes.</summary>
    public class RequestTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class RequestListItemDto
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;

        public int RequestTypeId { get; set; }
        public string RequestTypeCode { get; set; } = string.Empty;
        public string RequestTypeName { get; set; } = string.Empty;

        /// <summary>Chuỗi trạng thái - so sánh với hằng số RequestStatuses (Draft/Pending/Approved/Rejected/Cancelled).</summary>
        public string Status { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string Summary { get; set; } = string.Empty; // mô tả ngắn hiển thị trên UI

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Value { get; set; }

        public int? ApproverAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}