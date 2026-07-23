using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class EmploymentContract
{
    public int Id { get; set; }

    public string ContractNo { get; set; } = null!;

    public string ContractType { get; set; } = null!;

    public decimal BaseSalary { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public int UserId { get; set; }

    public string? Reason { get; set; }

    public virtual User User { get; set; } = null!;
}
