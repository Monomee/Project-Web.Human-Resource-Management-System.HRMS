using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _db;

    public PayrollService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CalculateMonthlyPayrollAsync(int periodId)
    {
        var period = await _db.TimesheetPeriods.FirstOrDefaultAsync(p => p.Id == periodId);
        if (period == null)
        {
            throw new InvalidOperationException($"Kỳ công ID={periodId} không tồn tại.");
        }

        if (!period.IsLocked)
        {
            throw new InvalidOperationException("Kỳ công này chưa được khóa sổ! Vui lòng khóa kỳ công trước khi tính lương.");
        }

        var users = await _db.Users.Where(u => u.Status).ToListAsync();
        if (!users.Any())
        {
            return true;
        }

        var activeContracts = await _db.EmploymentContracts
            .Where(c => c.Status == "Active")
            .ToDictionaryAsync(c => c.UserId);

        var attendances = await _db.Attendances
            .Where(a => a.PeriodId == periodId && a.CheckInTime != null)
            .ToListAsync();

        var attendanceDaysDict = attendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.AttendanceDate).Distinct().Count()
            );

        var periodStart = period.StartDate.ToDateTime(TimeOnly.MinValue);
        var periodEnd = period.EndDate.ToDateTime(TimeOnly.MaxValue);

        var approvedRequests = await _db.Requests
            .Include(r => r.RequestType)
            .Include(r => r.CreatedByAccount)
            .Where(r => r.Status == "Approved" && r.StartDate >= periodStart && r.EndDate <= periodEnd)
            .ToListAsync();

        var leaveDaysDict = approvedRequests
            .Where(r => r.RequestType.Code == "LEAVE")
            .GroupBy(r => r.CreatedByAccount.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r => r.Value)
            );

        var otHoursDict = approvedRequests
            .Where(r => r.RequestType.Code == "OT")
            .GroupBy(r => r.CreatedByAccount.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r => r.Value)
            );

        int standardWorkingDays = 0;
        for (var date = period.StartDate; date <= period.EndDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Sunday)
            {
                standardWorkingDays++;
            }
        }
        if (standardWorkingDays <= 0) standardWorkingDays = 26; 

        var payslips = new List<Payslip>();

        foreach (var user in users)
        {
            decimal baseSalary = 0m;
            if (activeContracts.TryGetValue(user.Id, out var contract))
            {
                baseSalary = contract.BaseSalary;
            }

            attendanceDaysDict.TryGetValue(user.Id, out int dActualInt);
            decimal dActual = (decimal)dActualInt;

            leaveDaysDict.TryGetValue(user.Id, out decimal dLeavePaid);

            otHoursDict.TryGetValue(user.Id, out decimal hOt);

            // Đơn giá ngày công = BaseSalary / StandardWorkingDays
            decimal dailyRate = Math.Round(baseSalary / (decimal)standardWorkingDays, 0, MidpointRounding.AwayFromZero);

            // Lương thực tế theo ngày công = Đơn giá ngày công * (D_actual + D_leave_paid)
            decimal actualDaysSalary = Math.Round(dailyRate * (dActual + dLeavePaid), 0, MidpointRounding.AwayFromZero);

            // Lương một giờ cơ bản = BaseSalary / (StandardWorkingDays * 8)
            decimal hourlyRate = Math.Round(baseSalary / ((decimal)standardWorkingDays * 8m), 0, MidpointRounding.AwayFromZero);

            // Tiền lương OT = Giờ OT * Lương một giờ cơ bản * 1.5
            decimal otSalary = Math.Round(hOt * hourlyRate * 1.5m, 0, MidpointRounding.AwayFromZero);

            // Phụ cấp mặc định = 0
            decimal allowances = 0m;

            // Tổng thu nhập (Gross) = Lương thực tế theo ngày công + Tiền lương OT + Các khoản phụ cấp
            decimal grossAmount = Math.Round(actualDaysSalary + otSalary + allowances, 0, MidpointRounding.AwayFromZero);

            // Khấu trừ bảo hiểm xã hội bắt buộc = BaseSalary * 10.5% (Tối đa tính trên trần 20 lần lương cơ sở: 2,340,000 * 20 = 46,800,000 VNĐ)
            decimal maxInsuranceBase = 46800000m;
            decimal insuranceBase = Math.Min(baseSalary, maxInsuranceBase);
            decimal insuranceDeduction = Math.Round(insuranceBase * 0.105m, 0, MidpointRounding.AwayFromZero);

            // Thu nhập chịu thuế TNCN = Gross - Khấu trừ bảo hiểm - 11,000,000
            decimal taxableIncome = grossAmount - insuranceDeduction - 11000000m;
            if (taxableIncome < 0m)
            {
                taxableIncome = 0m;
            }
            taxableIncome = Math.Round(taxableIncome, 0, MidpointRounding.AwayFromZero);

            // Tính thuế TNCN lũy tiến từng phần theo quy định Việt Nam
            decimal taxDeduction = 0m;
            if (taxableIncome > 80000000m)
            {
                taxDeduction = taxableIncome * 0.35m - 9850000m;
            }
            else if (taxableIncome > 52000000m)
            {
                taxDeduction = taxableIncome * 0.30m - 5850000m;
            }
            else if (taxableIncome > 32000000m)
            {
                taxDeduction = taxableIncome * 0.25m - 3250000m;
            }
            else if (taxableIncome > 18000000m)
            {
                taxDeduction = taxableIncome * 0.20m - 1650000m;
            }
            else if (taxableIncome > 10000000m)
            {
                taxDeduction = taxableIncome * 0.15m - 750000m;
            }
            else if (taxableIncome > 5000000m)
            {
                taxDeduction = taxableIncome * 0.10m - 250000m;
            }
            else if (taxableIncome > 0m)
            {
                taxDeduction = taxableIncome * 0.05m;
            }
            taxDeduction = Math.Round(taxDeduction, 0, MidpointRounding.AwayFromZero);

            decimal netAmount = grossAmount - insuranceDeduction - taxDeduction;
            if (netAmount < 0m)
            {
                netAmount = 0m;
            }
            netAmount = Math.Round(netAmount, 0, MidpointRounding.AwayFromZero);

            var payslip = new Payslip
            {
                UserId = user.Id,
                PeriodId = periodId,
                BaseSalary = baseSalary,
                OtSalary = otSalary,
                Allowances = allowances,
                InsuranceDeduction = insuranceDeduction,
                TaxDeduction = taxDeduction,
                GrossAmount = grossAmount,
                NetAmount = netAmount,
                Status = "Draft",
                CreatedAt = DateTime.Now
            };

            payslips.Add(payslip);
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var oldPayslips = await _db.Payslips.Where(p => p.PeriodId == periodId).ToListAsync();
            if (oldPayslips.Any())
            {
                _db.Payslips.RemoveRange(oldPayslips);
                await _db.SaveChangesAsync();
            }

            await _db.Payslips.AddRangeAsync(payslips);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Payslip>> GetPayslipsByPeriodAsync(int periodId)
    {
        var payslips = await _db.Payslips
            .Include(p => p.User)
                .ThenInclude(u => u.Department)
            .Include(p => p.User)
                .ThenInclude(u => u.Position)
            .Include(p => p.Period)
            .Where(p => p.PeriodId == periodId)
            .ToListAsync();

        if (payslips.Any())
        {
            var period = payslips.First().Period;
            var periodStart = period.StartDate.ToDateTime(TimeOnly.MinValue);
            var periodEnd = period.EndDate.ToDateTime(TimeOnly.MaxValue);

            var attendances = await _db.Attendances
                .Where(a => a.PeriodId == periodId && a.CheckInTime != null)
                .ToListAsync();
            var attendanceDaysDict = attendances
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(a => a.AttendanceDate).Distinct().Count()
                );

            var approvedRequests = await _db.Requests
                .Include(r => r.RequestType)
                .Include(r => r.CreatedByAccount)
                .Where(r => r.Status == "Approved" && r.StartDate >= periodStart && r.EndDate <= periodEnd)
                .ToListAsync();

            var leaveDaysDict = approvedRequests
                .Where(r => r.RequestType.Code == "LEAVE")
                .GroupBy(r => r.CreatedByAccount.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Value));

            var otHoursDict = approvedRequests
                .Where(r => r.RequestType.Code == "OT")
                .GroupBy(r => r.CreatedByAccount.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Value));

            foreach (var slip in payslips)
            {
                attendanceDaysDict.TryGetValue(slip.UserId, out int actual);
                slip.ActualDays = actual;

                leaveDaysDict.TryGetValue(slip.UserId, out decimal leavePaid);
                slip.LeavePaidDays = leavePaid;

                otHoursDict.TryGetValue(slip.UserId, out decimal otHours);
                slip.OtHours = otHours;
            }
        }

        return payslips;
    }

    public async Task<Payslip?> GetMyPayslipAsync(int periodId, int userId)
    {
        var payslip = await _db.Payslips
            .Include(p => p.User)
                .ThenInclude(u => u.Department)
            .Include(p => p.User)
                .ThenInclude(u => u.Position)
            .Include(p => p.Period)
            .FirstOrDefaultAsync(p => p.PeriodId == periodId && p.UserId == userId);

        if (payslip != null)
        {
            var period = payslip.Period;
            var periodStart = period.StartDate.ToDateTime(TimeOnly.MinValue);
            var periodEnd = period.EndDate.ToDateTime(TimeOnly.MaxValue);

            payslip.ActualDays = await _db.Attendances
                .Where(a => a.EmployeeId == userId && a.PeriodId == periodId && a.CheckInTime != null)
                .Select(a => a.AttendanceDate)
                .Distinct()
                .CountAsync();

            var leaveRequests = await _db.Requests
                .Where(r => r.CreatedByAccount.UserId == userId
                         && r.RequestType.Code == "LEAVE"
                         && r.Status == "Approved"
                         && r.StartDate >= periodStart
                         && r.EndDate <= periodEnd)
                .ToListAsync();
            payslip.LeavePaidDays = leaveRequests.Sum(r => r.Value);

            var otRequests = await _db.Requests
                .Where(r => r.CreatedByAccount.UserId == userId
                         && r.RequestType.Code == "OT"
                         && r.Status == "Approved"
                         && r.StartDate >= periodStart
                         && r.EndDate <= periodEnd)
                .ToListAsync();
            payslip.OtHours = otRequests.Sum(r => r.Value);
        }

        return payslip;
    }

    public async Task<int> GetUserIdByAccountIdAsync(int accountId)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        return account?.UserId ?? 0;
    }
}
