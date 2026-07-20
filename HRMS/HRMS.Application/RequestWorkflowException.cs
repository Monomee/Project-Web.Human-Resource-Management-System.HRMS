using System;

namespace HRMS.Application
{
    /// <summary>
    /// Exception nghiệp vụ dùng để UI hiển thị thông báo lỗi rõ ràng
    /// (ví dụ: không đủ phép, đơn không ở trạng thái hợp lệ để duyệt...).
    /// </summary>
    public class RequestWorkflowException : Exception
    {
        public RequestWorkflowException(string message) : base(message) { }
    }
}