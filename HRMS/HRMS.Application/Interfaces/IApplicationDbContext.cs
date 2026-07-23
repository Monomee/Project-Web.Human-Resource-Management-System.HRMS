using System.Threading;
using System.Threading.Tasks;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Interfaces
{
    /// <summary>
    /// Trừu tượng hoá DbContext để Application layer không phụ thuộc trực tiếp
    /// vào EF Core Infrastructure. Chỉ khai báo đúng các DbSet module Request Workflow cần dùng
    /// (Request/RequestType/LeaveBalance/Account đều là bảng có sẵn trong DB thật).
    ///
    /// Đăng ký Scoped (dùng chung trong 1 circuit Blazor Server). Vì vậy PHẢI dùng kèm
    /// DbConcurrencyGate để tránh lỗi "A second operation was started on this context instance..."
    /// khi nhiều component chạy song song.
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<Request> Requests { get; }
        DbSet<RequestType> RequestTypes { get; }
        DbSet<LeaveBalance> LeaveBalances { get; }
        DbSet<Account> Accounts { get; }
        DbSet<Shift> Shifts { get; }
        DbSet<ShiftAssignment> ShiftAssignments { get; }
        DbSet<Attendance> Attendances { get; }
        DbSet<Position> Positions { get; }
        DbSet<User> Users { get; }
        DbSet<Department> Departments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}