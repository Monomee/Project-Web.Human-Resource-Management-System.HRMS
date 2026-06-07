using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class Department
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? HeadAccountId { get; set; }

    public virtual Account? HeadAccount { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
