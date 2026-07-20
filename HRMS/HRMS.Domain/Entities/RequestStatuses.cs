namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Các giá trị chuẩn của cột Requests.Status (kiểu string trong DB thật).
    /// DB có DEFAULT 'Pending', và ràng buộc CHECK (nếu có) nên khớp đúng các chuỗi này.
    /// </summary>
    public static class RequestStatuses
    {
        public const string Draft = "Draft";
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }
}