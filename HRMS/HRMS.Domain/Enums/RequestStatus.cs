using System;
using System.Collections.Generic;
using System.Text;

namespace HRMS.Domain.Enums
{
    /// <summary>
    /// Trạng thái vòng đời của một đơn (Request).
    /// Draft -> Pending -> Approved / Rejected -> (Cancelled nếu nhân viên tự huỷ khi còn Draft/Pending)
    /// </summary>
    public enum RequestStatus
    {
        Draft = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4
    }
}
