using System;

namespace HRMS.Domain.Entities;

public partial class ShiftAssignment
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int ShiftId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int? AssignedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User Employee { get; set; } = null!;

    public virtual Shift Shift { get; set; } = null!;

    public virtual Account? AssignedByAccount { get; set; }
}
