using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class Position
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int JobLevel { get; set; }

    public int? DefaultShiftId { get; set; }

    public virtual Shift? DefaultShift { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
