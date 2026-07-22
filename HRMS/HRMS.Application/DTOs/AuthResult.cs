using System.Collections.Generic;

namespace HRMS.Application.DTOs;
//
public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
