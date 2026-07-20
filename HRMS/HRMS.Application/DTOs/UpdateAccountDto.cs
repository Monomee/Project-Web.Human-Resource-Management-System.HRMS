namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for updating an existing Account
/// </summary>
public class UpdateAccountDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public bool Status { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
