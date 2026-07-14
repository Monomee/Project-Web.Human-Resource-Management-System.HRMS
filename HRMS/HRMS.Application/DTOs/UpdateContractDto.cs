using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for updating existing employment contract
/// Only HRM role can update contracts
/// </summary>
public class UpdateContractDto
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Loại hợp đồng là bắt buộc")]
    public string ContractType { get; set; } = null!;

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    public DateOnly EndDate { get; set; }

    [Required(ErrorMessage = "Lương cơ bản là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Lương cơ bản phải lớn hơn 0")]
    public decimal BaseSalary { get; set; }

    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public string Status { get; set; } = null!;
}
