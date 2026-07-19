using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for creating new employment contract
/// </summary>
public class CreateContractDto
{
    [Required(ErrorMessage = "Loại hợp đồng là bắt buộc")]
    public string ContractType { get; set; } = null!;

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    public DateOnly EndDate { get; set; }

    [Required(ErrorMessage = "Lương cơ bản là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Lương cơ bản phải lớn hơn 0")]
    public decimal BaseSalary { get; set; }

    [Required]
    public int UserId { get; set; }
}
