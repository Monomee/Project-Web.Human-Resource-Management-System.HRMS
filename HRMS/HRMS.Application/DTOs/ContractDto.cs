using System;

namespace HRMS.Application.DTOs
{
    public class ContractDto
    {
        public int Id { get; set; }
        public string ContractNo { get; set; } = null!;
        public string ContractType { get; set; } = null!;
        public decimal BaseSalary { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = null!;
        public bool IsActive => Status == "Active";
    }
}
