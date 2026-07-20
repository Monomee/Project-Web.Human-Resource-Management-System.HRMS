using System.Threading.Tasks;

namespace HRMS.Application.Interfaces
{
    /// <summary>
    /// Tra cứu thông tin nhân viên/tổ chức dựa trên AccountId (Accounts.Id) - vì các cột FK
    /// trong bảng Requests (CreatedByAccountId, CurrentApproverAccountId) và
    /// Departments.HeadAccountId đều trỏ tới Accounts.Id, không phải Users.Id.
    /// </summary>
    public interface IEmployeeLookup
    {
        Task<string> GetEmployeeNameAsync(int accountId);

        /// <summary>Trả về AccountId của quản lý trực tiếp (trưởng phòng), dùng để định tuyến duyệt đơn OT/Khiếu nại công.</summary>
        Task<int?> GetManagerIdAsync(int accountId);

        /// <summary>AccountId của Giám đốc (HeadAccountId của phòng ban Code=BOD) - được duyệt TẤT CẢ đơn.</summary>
        Task<int?> GetDirectorAccountIdAsync();

        /// <summary>AccountId của Trưởng phòng Nhân sự (HeadAccountId của phòng ban Code=HR) - được duyệt TẤT CẢ đơn nghỉ phép.</summary>
        Task<int?> GetHrApproverAccountIdAsync();
    }
}