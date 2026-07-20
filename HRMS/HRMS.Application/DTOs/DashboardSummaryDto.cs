using System.Collections.Generic;

namespace HRMS.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalActiveEmployees { get; set; }
    public int PendingRequestsCount { get; set; }
    public decimal LatestPayrollExpense { get; set; }
    public string LatestPeriodName { get; set; } = string.Empty;
    public List<MonthlyPayrollChartDto> MonthlyPayrollHistory { get; set; } = new();
}
