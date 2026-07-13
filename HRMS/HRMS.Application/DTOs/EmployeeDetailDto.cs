using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs
{
    public class EmployeeDetailDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string EmailCompany { get; set; } = null!;
        public string? Phone { get; set; }
        public bool? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool Status { get; set; }
        
        public string DepartmentName { get; set; } = null!;
        public string DepartmentCode { get; set; } = null!;
        public string PositionName { get; set; } = null!;
        public string PositionCode { get; set; } = null!;
        
        public List<ContractDto> Contracts { get; set; } = new List<ContractDto>();
    }
}
