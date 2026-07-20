namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for creating a new Account
/// </summary>
public class CreateAccountDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public bool Status { get; set; } = true;
}
