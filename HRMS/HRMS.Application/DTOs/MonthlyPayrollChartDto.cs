namespace HRMS.Application.DTOs;

public class MonthlyPayrollChartDto
{
    public string PeriodName { get; set; } = string.Empty;
    public decimal TotalNetSalary { get; set; }
    public double PercentageHeight { get; set; }
}
