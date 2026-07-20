using System.Collections.Generic;
using System.Threading.Tasks;
using HRMS.Domain.Entities;

namespace HRMS.Application.Interfaces;

public interface IPayrollService
{
    Task<bool> CalculateMonthlyPayrollAsync(int periodId);
    Task<List<Payslip>> GetPayslipsByPeriodAsync(int periodId);
    Task<Payslip?> GetMyPayslipAsync(int periodId, int userId);
    Task<int> GetUserIdByAccountIdAsync(int accountId);
}
