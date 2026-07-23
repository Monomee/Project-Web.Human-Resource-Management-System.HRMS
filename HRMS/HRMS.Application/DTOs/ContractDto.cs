using System;
using HRMS.Domain.Enums;

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
        public string? Reason { get; set; }
        
        /// <summary>
        /// Parsed status as enum
        /// </summary>
        public ContractStatus StatusEnum
        {
            get
            {
                return Status switch
                {
                    "Draft" => ContractStatus.Draft,
                    "Pending" => ContractStatus.Pending,
                    "Active" => ContractStatus.Active,
                    "Rejected" => ContractStatus.Rejected,
                    _ => ContractStatus.Draft
                };
            }
        }

        /// <summary>
        /// Display name for status in Vietnamese
        /// </summary>
        public string StatusDisplayName
        {
            get
            {
                return StatusEnum switch
                {
                    ContractStatus.Draft => "Nháp",
                    ContractStatus.Pending => "Chờ duyệt",
                    ContractStatus.Active => "Hiệu lực",
                    ContractStatus.Rejected => "Từ chối",
                    _ => "Không xác định"
                };
            }
        }

        /// <summary>
        /// CSS class for status badge
        /// </summary>
        public string StatusBadgeClass
        {
            get
            {
                return StatusEnum switch
                {
                    ContractStatus.Draft => "badge-draft",
                    ContractStatus.Pending => "badge-pending",
                    ContractStatus.Active => "badge-active",
                    ContractStatus.Rejected => "badge-rejected",
                    _ => "badge-secondary"
                };
            }
        }

        public bool IsActive => Status == "Active";
    }
}
