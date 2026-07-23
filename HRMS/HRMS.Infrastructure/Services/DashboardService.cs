using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int accountId)
    {
        // 1. Tổng số nhân sự đang hoạt động
        int totalActiveEmployees = await _context.Users.CountAsync(u => u.Status);

        // 2. Tổng số đơn từ đang chờ duyệt
        int pendingRequestsCount = await _context.Requests.CountAsync(r => r.Status == "Pending");

        // 3. Tìm kỳ công gần nhất đã khóa sổ
        var latestLockedPeriod = await _context.TimesheetPeriods
            .Where(p => p.IsLocked)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        decimal latestPayrollExpense = 0m;
        string latestPeriodName = "Chưa chốt kỳ lương";

        if (latestLockedPeriod != null)
        {
            latestPeriodName = latestLockedPeriod.Name;
            latestPayrollExpense = await _context.Payslips
                .Where(ps => ps.PeriodId == latestLockedPeriod.Id)
                .SumAsync(ps => ps.NetAmount);
        }

        // 4. Lấy danh sách 5-6 kỳ công gần nhất (lấy tối đa 6 kỳ)
        var latestPeriods = await _context.TimesheetPeriods
            .OrderByDescending(p => p.StartDate)
            .Take(6)
            .ToListAsync();

        // Đảo ngược thứ tự để hiển thị theo thời gian tăng dần từ trái sang phải
        latestPeriods.Reverse();

        var monthlyPayrollHistory = new List<MonthlyPayrollChartDto>();
        foreach (var period in latestPeriods)
        {
            decimal totalNetSalary = await _context.Payslips
                .Where(ps => ps.PeriodId == period.Id)
                .SumAsync(ps => ps.NetAmount);

            monthlyPayrollHistory.Add(new MonthlyPayrollChartDto
            {
                PeriodName = period.Name,
                TotalNetSalary = totalNetSalary,
                PercentageHeight = 0.0 // Sẽ tính sau
            });
        }

        // Tính tỷ lệ phần trăm chiều cao biểu đồ
        if (monthlyPayrollHistory.Any())
        {
            decimal maxNetSalary = monthlyPayrollHistory.Max(x => x.TotalNetSalary);
            if (maxNetSalary > 0m)
            {
                foreach (var item in monthlyPayrollHistory)
                {
                    item.PercentageHeight = (double)(item.TotalNetSalary / maxNetSalary * 100m);
                }
            }
        }

        // 5. Thống kê cá nhân nhân viên
        int remainingLeaveDays = 0;
        int myPendingRequestsCount = 0;
        int myWorkDaysInLatestPeriod = 0;

        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account != null && account.User != null)
        {
            var userId = account.UserId;
            var currentYear = DateTime.Now.Year;

            // Số ngày phép còn lại
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.Year == currentYear);
            remainingLeaveDays = leaveBalance?.RemainingDays ?? 12;

            // Đơn từ cá nhân đang chờ duyệt
            myPendingRequestsCount = await _context.Requests
                .CountAsync(r => r.CreatedByAccountId == accountId && r.Status == "Pending");

            // Số ngày công trong kỳ gần nhất
            var latestPeriod = await _context.TimesheetPeriods
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();

            if (latestPeriod != null)
            {
                myWorkDaysInLatestPeriod = await _context.Attendances
                    .Where(a => a.EmployeeId == userId && a.PeriodId == latestPeriod.Id && a.CheckInTime != null)
                    .Select(a => a.AttendanceDate)
                    .Distinct()
                    .CountAsync();
            }
        }

        return new DashboardSummaryDto
        {
            TotalActiveEmployees = totalActiveEmployees,
            PendingRequestsCount = pendingRequestsCount,
            LatestPayrollExpense = latestPayrollExpense,
            LatestPeriodName = latestPeriodName,
            MonthlyPayrollHistory = monthlyPayrollHistory,
            RemainingLeaveDays = remainingLeaveDays,
            MyPendingRequestsCount = myPendingRequestsCount,
            MyWorkDaysInLatestPeriod = myWorkDaysInLatestPeriod
        };
    }
}
