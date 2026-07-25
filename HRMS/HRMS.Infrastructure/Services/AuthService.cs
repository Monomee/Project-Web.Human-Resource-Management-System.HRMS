using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Tên đăng nhập và mật khẩu không được để trống."
            };
        }

        var adminUsername = _configuration["AdminAccount:Username"];
        var adminPassword = _configuration["AdminAccount:Password"];
        var adminFullName = _configuration["AdminAccount:FullName"] ?? "System Administrator";

        if (!string.IsNullOrEmpty(adminUsername) && 
            !string.IsNullOrEmpty(adminPassword) &&
            username == adminUsername && 
            password == adminPassword)
        {
            return new AuthResult
            {
                Success = true,
                AccountId = -1, 
                FullName = adminFullName,
                Roles = new List<string> { "Admin" }
            };
        }

        var account = await _dbContext.Accounts
            .Include(a => a.User)
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác."
            };
        }

        if (!account.Status)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Tài khoản đã bị khóa hoặc ngừng hoạt động."
            };
        }

        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
        }
        catch (Exception)
        {
            isPasswordValid = false;
        }

        if (!isPasswordValid)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác."
            };
        }

        var roles = account.Roles.Select(r => r.Name).ToList();

        return new AuthResult
        {
            Success = true,
            AccountId = account.Id,
            FullName = account.User?.FullName ?? account.Username,
            Roles = roles
        };
    }

    public async Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        var account = await _dbContext.Accounts.FindAsync(accountId);
        if (account == null)
            return false;

        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, account.PasswordHash);
        }
        catch (Exception)
        {
            isPasswordValid = false;
        }

        if (!isPasswordValid)
            return false;

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<string> ResetPasswordAsync(int accountId)
    {
        var account = await _dbContext.Accounts.FindAsync(accountId);
        if (account == null)
        {
            throw new InvalidOperationException($"Không tìm thấy tài khoản với ID={accountId}");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
        await _dbContext.SaveChangesAsync();

        return "Password123";
    }
}
