namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Các mã (Code) chuẩn dùng để nhận diện loại đơn khi xử lý logic nghiệp vụ,
    /// phải khớp với dữ liệu cột Code trong bảng RequestTypes.
    /// So sánh nên dùng ToUpperInvariant() để tránh sai khác hoa/thường.
    /// </summary>
    public static class RequestTypeCodes
    {
        public const string Leave = "LEAVE";
        public const string Overtime = "OT";
        public const string Complaint = "COMPLAINT";
    }
}