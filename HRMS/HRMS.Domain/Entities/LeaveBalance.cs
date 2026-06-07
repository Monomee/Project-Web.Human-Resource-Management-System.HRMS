using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class LeaveBalance
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int TotalDays { get; set; }

    public int UsedDays { get; set; }

    public int RemainingDays { get; set; }

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
