using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class TimesheetPeriod
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsLocked { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
