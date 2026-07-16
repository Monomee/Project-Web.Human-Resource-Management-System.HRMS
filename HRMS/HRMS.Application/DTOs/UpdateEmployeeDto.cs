using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class UpdateEmployeeDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string EmailCompany { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Giới tính là bắt buộc")]
    public bool? Gender { get; set; }

    [Required(ErrorMessage = "Phòng ban là bắt buộc")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Chức vụ là bắt buộc")]
    public int? PositionId { get; set; }

    [Required(ErrorMessage = "Vai trò hệ thống là bắt buộc")]
    public int? RoleId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public bool Status { get; set; }
}
