using System;
using System.Collections.Generic;

namespace HRMS.Domain.Entities;

public partial class User
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string EmailCompany { get; set; } = null!;

    public string? Phone { get; set; }

    public bool? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool Status { get; set; }

    public int DepartmentId { get; set; }

    public int PositionId { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<EmploymentContract> EmploymentContracts { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();

    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();

    public virtual Position Position { get; set; } = null!;
}
