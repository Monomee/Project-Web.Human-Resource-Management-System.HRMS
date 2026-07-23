using System;

namespace HRMS.Application.DTOs.Attendance;

public class ShiftDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly BreakStart { get; set; }
    public TimeOnly BreakEnd { get; set; }
    public int LateToleranceMinute { get; set; }
    public int EarlyCheckInMinute { get; set; }
    public int LateCheckOutMinute { get; set; }
    public bool IsActive { get; set; }
}

public class CreateShiftDto
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly BreakStart { get; set; }
    public TimeOnly BreakEnd { get; set; }
    public int LateToleranceMinute { get; set; }
    public int EarlyCheckInMinute { get; set; }
    public int LateCheckOutMinute { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateShiftDto : CreateShiftDto
{
    public int Id { get; set; }
}

public class ShiftAssignmentDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShiftAssignmentDto
{
    public int EmployeeId { get; set; }
    public int ShiftId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class AttendanceDetailDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }
    public int? PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public int WorkingMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class UpdateAttendanceDto
{
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
}
