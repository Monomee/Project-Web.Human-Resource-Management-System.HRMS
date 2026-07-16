using System;

namespace HRMS.Application.DTOs
{
    public class EmployeeDto
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
        public string DepartmentName { get; set; } = null!;
        
        public int PositionId { get; set; }
        public string PositionName { get; set; } = null!;
    }
}
