using System.Threading.Tasks;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
}
