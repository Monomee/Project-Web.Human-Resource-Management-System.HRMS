namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for displaying Account information
/// </summary>
public class AccountDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public bool Status { get; set; }
    public int UserId { get; set; }
    
    // User information
    public string EmployeeCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string EmailCompany { get; set; } = null!;
    public int DepartmentId { get; set; }
    
    // Role information
    public List<int> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public string RolesDisplay => string.Join(", ", RoleNames);
}
