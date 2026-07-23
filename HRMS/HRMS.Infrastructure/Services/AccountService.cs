using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Exceptions;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public AccountService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<AccountDto>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Accounts
            .Include(a => a.User)
                .ThenInclude(u => u.Department)
            .Include(a => a.Roles)
            .OrderBy(a => a.Username)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                Username = a.Username,
                Status = a.Status,
                UserId = a.UserId,
                EmployeeCode = a.User.EmployeeCode,
                FullName = a.User.FullName,
                EmailCompany = a.User.EmailCompany,
                DepartmentId = a.User.DepartmentId,
                RoleIds = a.Roles.Select(r => r.Id).ToList(),
                RoleNames = a.Roles.Select(r => r.Name).ToList()
            })
            .ToListAsync();
    }

    public async Task<AccountDto?> GetByIdAsync(int accountId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var account = await context.Accounts
            .Include(a => a.User)
                .ThenInclude(u => u.Department)
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null)
            return null;

        return new AccountDto
        {
            Id = account.Id,
            Username = account.Username,
            Status = account.Status,
            UserId = account.UserId,
            EmployeeCode = account.User.EmployeeCode,
            FullName = account.User.FullName,
            EmailCompany = account.User.EmailCompany,
            DepartmentId = account.User.DepartmentId,
            RoleIds = account.Roles.Select(r => r.Id).ToList(),
            RoleNames = account.Roles.Select(r => r.Name).ToList()
        };
    }

    public async Task<AccountDto?> GetByUserIdAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var account = await context.Accounts
            .Include(a => a.User)
                .ThenInclude(u => u.Department)
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (account == null)
            return null;

        return new AccountDto
        {
            Id = account.Id,
            Username = account.Username,
            Status = account.Status,
            UserId = account.UserId,
            EmployeeCode = account.User.EmployeeCode,
            FullName = account.User.FullName,
            EmailCompany = account.User.EmailCompany,
            DepartmentId = account.User.DepartmentId,
            RoleIds = account.Roles.Select(r => r.Id).ToList(),
            RoleNames = account.Roles.Select(r => r.Name).ToList()
        };
    }

    public async Task<AccountDto> CreateAsync(CreateAccountDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Validate username uniqueness
        if (await UsernameExistsAsync(dto.Username))
        {
            throw new BusinessException($"Username '{dto.Username}' đã tồn tại trong hệ thống.");
        }

        // Validate user exists
        var user = await context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            throw new BusinessException($"Không tìm thấy nhân viên với ID = {dto.UserId}");
        }

        // Check if user already has an account
        var existingAccount = await context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == dto.UserId);
        
        if (existingAccount != null)
        {
            throw new BusinessException($"Nhân viên {user.FullName} đã có tài khoản.");
        }

        // Create new account
        var account = new Account
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = dto.Status,
            UserId = dto.UserId
        };

        // Add roles
        if (dto.RoleIds.Any())
        {
            var roles = await context.Roles
                .Where(r => dto.RoleIds.Contains(r.Id))
                .ToListAsync();

            foreach (var role in roles)
            {
                account.Roles.Add(role);
            }
        }

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Return created account DTO
        return new AccountDto
        {
            Id = account.Id,
            Username = account.Username,
            Status = account.Status,
            UserId = account.UserId,
            EmployeeCode = user.EmployeeCode,
            FullName = user.FullName,
            EmailCompany = user.EmailCompany,
            DepartmentId = user.DepartmentId,
            RoleIds = account.Roles.Select(r => r.Id).ToList(),
            RoleNames = account.Roles.Select(r => r.Name).ToList()
        };
    }

    public async Task<bool> UpdateAsync(UpdateAccountDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var account = await context.Accounts
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.Id == dto.Id);

        if (account == null)
            return false;

        // Validate username uniqueness (excluding current account)
        if (await UsernameExistsAsync(dto.Username, dto.Id))
        {
            throw new BusinessException($"Username '{dto.Username}' đã tồn tại trong hệ thống.");
        }

        // Update account fields
        account.Username = dto.Username;
        account.Status = dto.Status;

        // Update roles
        account.Roles.Clear();
        if (dto.RoleIds.Any())
        {
            var roles = await context.Roles
                .Where(r => dto.RoleIds.Contains(r.Id))
                .ToListAsync();

            foreach (var role in roles)
            {
                account.Roles.Add(role);
            }
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int accountId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var account = await context.Accounts.FindAsync(accountId);
        
        if (account == null)
            return false;

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(AdminChangePasswordDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var account = await context.Accounts.FindAsync(dto.AccountId);
        
        if (account == null)
            return false;

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int accountId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var account = await context.Accounts.FindAsync(accountId);
        
        if (account == null)
            return false;

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeAccountId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Accounts.Where(a => a.Username == username);
        
        if (excludeAccountId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAccountId.Value);
        }

        return await query.AnyAsync();
    }
}
