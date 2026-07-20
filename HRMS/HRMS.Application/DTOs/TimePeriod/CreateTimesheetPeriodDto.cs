using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.TimePeriod;

/// <summary>
/// DTO dùng để tạo mới một kỳ công.
/// DataAnnotations trên DTO này được Blazor EditForm dùng để validate phía client/server.
/// </summary>
public class CreateTimesheetPeriodDto : IValidatableObject
{
    /// <summary>Tên kỳ công. Ví dụ: "Tháng 07/2026".</summary>
    [Required(ErrorMessage = "Tên kỳ công không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên kỳ công tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Ngày bắt đầu của kỳ công.</summary>
    [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
    public DateOnly? StartDate { get; set; }

    /// <summary>Ngày kết thúc của kỳ công.</summary>
    [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Validate liên trường: EndDate phải sau StartDate.
    /// IValidatableObject cho phép viết logic validation phức tạp hơn DataAnnotations đơn thuần.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue)
        {
            if (EndDate.Value <= StartDate.Value)
            {
                yield return new ValidationResult(
                    "Ngày kết thúc phải sau ngày bắt đầu.",
                    new[] { nameof(EndDate) });
            }

            // Giới hạn kỳ công tối đa 3 tháng (92 ngày) để tránh nhập sai
            int days = EndDate.Value.DayNumber - StartDate.Value.DayNumber;
            if (days > 92)
            {
                yield return new ValidationResult(
                    "Kỳ công không được dài quá 92 ngày (~3 tháng).",
                    new[] { nameof(EndDate) });
            }
        }
    }
}
