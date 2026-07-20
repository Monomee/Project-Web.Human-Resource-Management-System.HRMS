using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for Account management operations
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Get all accounts with user and role information
    /// </summary>
    Task<List<AccountDto>> GetAllAsync();

    /// <summary>
    /// Get account by ID with full details
    /// </summary>
    Task<AccountDto?> GetByIdAsync(int accountId);

    /// <summary>
    /// Get account by user ID
    /// </summary>
    Task<AccountDto?> GetByUserIdAsync(int userId);

    /// <summary>
    /// Create a new account
    /// </summary>
    Task<AccountDto> CreateAsync(CreateAccountDto dto);

    /// <summary>
    /// Update an existing account
    /// </summary>
    Task<bool> UpdateAsync(UpdateAccountDto dto);

    /// <summary>
    /// Delete an account
    /// </summary>
    Task<bool> DeleteAsync(int accountId);

    /// <summary>
    /// Change account password
    /// </summary>
    Task<bool> ChangePasswordAsync(ChangePasswordDto dto);

    /// <summary>
    /// Check if username already exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, int? excludeAccountId = null);
}
