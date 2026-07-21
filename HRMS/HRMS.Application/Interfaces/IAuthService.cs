using System.Threading.Tasks;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword);
    Task<string> ResetPasswordAsync(int accountId);
}
