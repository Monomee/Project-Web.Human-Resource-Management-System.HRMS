using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class Payslip
{
    public int Id { get; set; }

    public decimal BaseSalary { get; set; }

    public decimal OtSalary { get; set; }

    public decimal Allowances { get; set; }

    public decimal InsuranceDeduction { get; set; }

    public decimal TaxDeduction { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal NetAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }

    public int PeriodId { get; set; }

    public virtual TimesheetPeriod Period { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
