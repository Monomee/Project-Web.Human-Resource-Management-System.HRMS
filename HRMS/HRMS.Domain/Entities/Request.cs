using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class Request
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal Value { get; set; }

    public DateTime CreatedAt { get; set; }

    public int RequestTypeId { get; set; }

    public int CreatedByAccountId { get; set; }

    public int? CurrentApproverAccountId { get; set; }

    public virtual Account CreatedByAccount { get; set; } = null!;

    public virtual Account? CurrentApproverAccount { get; set; }

    public virtual RequestType RequestType { get; set; } = null!;
}
