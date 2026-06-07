using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class RequestType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
