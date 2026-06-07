using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class AttendanceLog
{
    public int Id { get; set; }

    public DateTime CheckedAt { get; set; }

    public string CheckType { get; set; } = null!;

    public string Source { get; set; } = null!;

    public int UserId { get; set; }

    public int PeriodId { get; set; }

    public virtual TimesheetPeriod Period { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
