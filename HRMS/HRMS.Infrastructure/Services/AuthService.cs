using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;

    public AuthService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        // Tìm kiếm Account kết nối với bảng Users và Roles
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

        // Chỉ cho phép tài khoản có Status == true đăng nhập
        if (!account.Status)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Tài khoản đã bị khóa hoặc ngừng hoạt động."
            };
        }

        // Kiểm tra mật khẩu bằng BCrypt
        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
        }
        catch (Exception)
        {
            // Tránh văng lỗi nếu hash trong DB sai định dạng
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

        // Đăng nhập thành công, thu thập danh sách tên quyền (Roles)
        var roles = account.Roles.Select(r => r.Name).ToList();

        // Trả về kết quả
        return new AuthResult
        {
            Success = true,
            AccountId = account.Id,
            FullName = account.User?.FullName ?? account.Username,
            Roles = roles
        };
    }
}
