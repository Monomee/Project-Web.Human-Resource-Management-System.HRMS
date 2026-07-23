using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class Shift
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public TimeOnly BreakStart { get; set; }

    public TimeOnly BreakEnd { get; set; }

    public int LateToleranceMinute { get; set; }

    public int EarlyCheckInMinute { get; set; }

    public int LateCheckOutMinute { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
