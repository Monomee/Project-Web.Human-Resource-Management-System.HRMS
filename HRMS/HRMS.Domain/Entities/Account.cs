using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool Status { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Request> RequestCreatedByAccounts { get; set; } = new List<Request>();

    public virtual ICollection<Request> RequestCurrentApproverAccounts { get; set; } = new List<Request>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
