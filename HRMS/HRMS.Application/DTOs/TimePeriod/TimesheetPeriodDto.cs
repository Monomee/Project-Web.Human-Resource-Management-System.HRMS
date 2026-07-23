using System;

namespace HRMS.Application.DTOs.TimePeriod;

public class TimesheetPeriodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsLocked { get; set; }
}
