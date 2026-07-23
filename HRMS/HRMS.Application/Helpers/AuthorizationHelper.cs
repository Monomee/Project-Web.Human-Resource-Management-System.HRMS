using HRMS.Application.DTOs;

namespace HRMS.Application.Helpers;

/// <summary>
/// Helper class for account authorization logic
/// Role Hierarchy: Admin > HRM > HR > Manager/Executive > Employee
/// </summary>
public static class AuthorizationHelper
{
    // Role hierarchy levels
    private static readonly Dictionary<string, int> RoleHierarchy = new()
    {
        { "Admin", 5 },
        { "HRM", 4 },
        { "HR", 3 },
        { "Manager", 2 },
        { "Executive", 2 },
        { "Employee", 1 }
    };

    /// <summary>
    /// Check if current user can edit target account
    /// Rules:
    /// - Admin: Can edit all
    /// - HRM: Can edit HR, Manager, Executive, Employee
    /// - HR: Can edit Manager, Executive, Employee
    /// - Manager: Can edit Employee in same department only
    /// - Executive: Cannot edit anyone (read-only)
    /// - Employee: Cannot edit anyone
    /// </summary>
    public static bool CanEditAccount(AccountDto currentUser, AccountDto targetAccount)
    {
        // Cannot edit yourself
        if (currentUser.Id == targetAccount.Id)
            return false;

        var currentRole = GetHighestRole(currentUser.RoleNames);
        var targetRole = GetHighestRole(targetAccount.RoleNames);

        // Executive can only view, cannot edit
        if (currentRole == "Executive")
            return false;

        // Admin can edit all (except self)
        if (currentRole == "Admin")
            return true;

        // Manager special rule: can only edit Employee in same department
        if (currentRole == "Manager")
        {
            return targetRole == "Employee" && 
                   currentUser.DepartmentId == targetAccount.DepartmentId;
        }

        // For HRM and HR: can edit if target is lower in hierarchy
        int currentLevel = GetRoleLevel(currentRole);
        int targetLevel = GetRoleLevel(targetRole);

        return currentLevel > targetLevel;
    }

    /// <summary>
    /// Check if current user can view target account
    /// Rules:
    /// - Admin, HRM, HR: Can view all
    /// - Executive: Can view all (but not edit)
    /// - Manager: Can view Employee in same department
    /// </summary>
    public static bool CanViewAccount(AccountDto currentUser, AccountDto targetAccount)
    {
        var currentRole = GetHighestRole(currentUser.RoleNames);

        // Admin, HRM, HR, Executive can view all
        if (currentRole == "Admin" || currentRole == "HRM" || 
            currentRole == "HR" || currentRole == "Executive")
            return true;

        // Manager can view employees in same department
        if (currentRole == "Manager")
        {
            var targetRole = GetHighestRole(targetAccount.RoleNames);
            return targetRole == "Employee" && 
                   currentUser.DepartmentId == targetAccount.DepartmentId;
        }

        // Employee cannot view accounts list (should not reach here due to page authorization)
        return false;
    }

    /// <summary>
    /// Filter accounts list based on user's viewing permissions
    /// </summary>
    public static List<AccountDto> FilterAccountsByPermission(
        AccountDto currentUser, 
        List<AccountDto> allAccounts)
    {
        var currentRole = GetHighestRole(currentUser.RoleNames);

        // Admin, HRM, HR, Executive: see all
        if (currentRole == "Admin" || currentRole == "HRM" || 
            currentRole == "HR" || currentRole == "Executive")
        {
            return allAccounts;
        }

        // Manager: see only employees in same department
        if (currentRole == "Manager")
        {
            return allAccounts
                .Where(acc => {
                    var targetRole = GetHighestRole(acc.RoleNames);
                    return targetRole == "Employee" && 
                           acc.DepartmentId == currentUser.DepartmentId;
                })
                .ToList();
        }

        // Employee: no access (should not reach here)
        return new List<AccountDto>();
    }

    /// <summary>
    /// Get the highest role from a list of roles
    /// </summary>
    private static string GetHighestRole(List<string> roles)
    {
        if (roles == null || !roles.Any())
            return "Employee";

        return roles
            .OrderByDescending(r => GetRoleLevel(r))
            .First();
    }

    /// <summary>
    /// Get role level from hierarchy
    /// </summary>
    private static int GetRoleLevel(string role)
    {
        return RoleHierarchy.TryGetValue(role, out int level) ? level : 0;
    }

    /// <summary>
    /// Check if user can create accounts
    /// Only Admin can create accounts
    /// </summary>
    public static bool CanCreateAccount(AccountDto currentUser)
    {
        var currentRole = GetHighestRole(currentUser.RoleNames);
        return currentRole == "Admin";
    }

    /// <summary>
    /// Check if user can delete accounts
    /// Only Admin can delete accounts
    /// </summary>
    public static bool CanDeleteAccount(AccountDto currentUser)
    {
        var currentRole = GetHighestRole(currentUser.RoleNames);
        return currentRole == "Admin";
    }

    /// <summary>
    /// Check if user can reset passwords
    /// Only Admin can reset passwords
    /// </summary>
    public static bool CanResetPassword(AccountDto currentUser)
    {
        var currentRole = GetHighestRole(currentUser.RoleNames);
        return currentRole == "Admin";
    }
}
