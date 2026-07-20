using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.TimePeriod;

/// <summary>
/// DTO dùng để cập nhật thông tin một kỳ công.
/// Lưu ý: không cho phép sửa nếu kỳ đã bị khóa (IsLocked = true) — logic này ở tầng Service.
/// </summary>
public class UpdateTimesheetPeriodDto : IValidatableObject
{
    /// <summary>Tên kỳ công mới.</summary>
    [Required(ErrorMessage = "Tên kỳ công không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên kỳ công tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Ngày bắt đầu mới.</summary>
    [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
    public DateOnly? StartDate { get; set; }

    /// <summary>Ngày kết thúc mới.</summary>
    [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Validate liên trường: EndDate phải sau StartDate.
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
