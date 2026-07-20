namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for changing Account password
/// </summary>
public class ChangePasswordDto
{
    public int AccountId { get; set; }
    public string NewPassword { get; set; } = null!;
}
