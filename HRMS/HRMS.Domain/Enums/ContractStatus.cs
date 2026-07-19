namespace HRMS.Domain.Enums;

/// <summary>
/// Trạng thái của hợp đồng lao động trong hệ thống
/// </summary>
public enum ContractStatus
{
    /// <summary>
    /// Bản nháp (chưa hoàn thiện)
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Đang chờ duyệt (trạng thái mặc định khi tạo mới)
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Đang hiệu lực
    /// </summary>
    Active = 2,

    /// <summary>
    /// Bị từ chối
    /// </summary>
    Rejected = 3
}
