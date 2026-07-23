using System;

namespace HRMS.Domain.Entities;

public partial class Attendance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public TimeOnly? CheckInTime { get; set; }

    public TimeOnly? CheckOutTime { get; set; }

    public int? PeriodId { get; set; }

    public virtual User Employee { get; set; } = null!;

    public virtual TimesheetPeriod? Period { get; set; }
}
