using System.Linq;
using System.Threading.Tasks;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services
{
    /// <summary>
    /// Cài đặt thật của IEmployeeLookup, thao tác trên AccountId (Accounts.Id).
    /// Dùng IDbContextFactory (không phải Scoped DbContext dùng chung) để tránh lỗi
    /// "A second operation was started on this context instance..." khi nhiều component
    /// Blazor Server chạy song song và cùng gọi tới đây.
    ///   Accounts:    Id, Username, ..., UserId (1-1 với Users)
    ///   Users:       Id, FullName, ..., DepartmentId
    ///   Departments: Id, Name, HeadAccountId (Id của Accounts - trưởng phòng)
    /// </summary>
    public class EmployeeLookup : IEmployeeLookup
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public EmployeeLookup(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<string> GetEmployeeNameAsync(int accountId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var fullName = await db.Accounts
                .AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => a.User.FullName)
                .FirstOrDefaultAsync();

            return fullName ?? $"TK#{accountId}";
        }

        public async Task<int?> GetManagerIdAsync(int accountId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var account = await db.Accounts
                .AsNoTracking()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account is null)
                return null;

            var department = await db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == account.User.DepartmentId);

            if (department?.HeadAccountId is null)
                return null;

            // Chính họ là trưởng phòng -> không có quản lý cấp trên trong mô hình phòng ban thông thường
            // (đơn của họ sẽ được Giám đốc xử lý - xem GetDirectorAccountIdAsync trong RequestService)
            if (department.HeadAccountId == accountId)
                return null;

            return department.HeadAccountId;
        }

        public async Task<int?> GetDirectorAccountIdAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Departments
                .AsNoTracking()
                .Where(d => d.Code == DepartmentCodes.Director)
                .Select(d => (int?)d.HeadAccountId)
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetHrApproverAccountIdAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Departments
                .AsNoTracking()
                .Where(d => d.Code == DepartmentCodes.Hr)
                .Select(d => (int?)d.HeadAccountId)
                .FirstOrDefaultAsync();
        }
    }
}