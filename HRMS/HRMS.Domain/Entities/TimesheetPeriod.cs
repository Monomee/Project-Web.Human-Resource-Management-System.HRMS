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

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
