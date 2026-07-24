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
        // Special handling for hardcoded admin (Id = -1)
        if (currentUser.Id == -1)
        {
            return targetAccount.Id != -1; // Admin can edit all except itself
        }
        
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
        // Special handling for hardcoded admin (Id = -1)
        if (currentUser.Id == -1)
        {
            return true; // Admin can view all
        }
        
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
        // Special handling for hardcoded admin (Id = -1)
        if (currentUser.Id == -1)
        {
            return allAccounts; // Admin sees all
        }
        
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
        // Special handling for hardcoded admin (Id = -1)
        if (currentUser.Id == -1)
        {
            return true;
        }
        
        var currentRole = GetHighestRole(currentUser.RoleNames);
        return currentRole == "Admin";
    }

    /// <summary>
    /// Check if user can delete accounts
    /// Admin: Can delete all (except self)
    /// HRM: Can delete HR, Manager, Executive, Employee (except self)
    /// HR: Can delete Manager, Executive, Employee (except self)
    /// </summary>
    public static bool CanDeleteAccount(AccountDto currentUser, AccountDto targetAccount)
    {
        // Special handling for hardcoded admin (Id = -1)
        if (currentUser.Id == -1)
        {
            return targetAccount.Id != -1; // Admin can delete all except itself
        }
        
        // Cannot delete yourself
        if (currentUser.Id == targetAccount.Id)
            return false;
        
        var currentRole = GetHighestRole(currentUser.RoleNames);
        var targetRole = GetHighestRole(targetAccount.RoleNames);
        
        // Admin can delete all (except self)
        if (currentRole == "Admin")
            return true;
        
        // HRM can delete HR, Manager, Executive, Employee (except self)
        if (currentRole == "HRM")
        {
            return targetRole == "HR" || targetRole == "Manager" || 
                   targetRole == "Executive" || targetRole == "Employee";
        }
        
        // HR can delete Manager, Executive, Employee (except self)
        if (currentRole == "HR")
        {
            return targetRole == "Manager" || targetRole == "Executive" || 
                   targetRole == "Employee";
        }
        
        // Others cannot delete
        return false;
    }

    /// <summary>
    /// Check if user can reset passwords
    /// Admin: Can reset all (except self)
    /// HRM: Can reset HR, Manager, Executive, Employee (except self)
    /// HR: Can reset Manager, Executive, Employee (except self)
    /// </summary>
    public static bool CanResetPassword(AccountDto currentUser, AccountDto targetAccount)
    {
        // Special handling for hardcoded admin (Id = -1)
        if (currentUser.Id == -1)
        {
            return targetAccount.Id != -1; // Admin can reset all except itself
        }
        
        // Cannot reset your own password through admin function
        if (currentUser.Id == targetAccount.Id)
            return false;
        
        var currentRole = GetHighestRole(currentUser.RoleNames);
        var targetRole = GetHighestRole(targetAccount.RoleNames);
        
        // Admin can reset all (except self)
        if (currentRole == "Admin")
            return true;
        
        // HRM can reset HR, Manager, Executive, Employee (except self)
        if (currentRole == "HRM")
        {
            return targetRole == "HR" || targetRole == "Manager" || 
                   targetRole == "Executive" || targetRole == "Employee";
        }
        
        // HR can reset Manager, Executive, Employee (except self)
        if (currentRole == "HR")
        {
            return targetRole == "Manager" || targetRole == "Executive" || 
                   targetRole == "Employee";
        }
        
        // Others cannot reset
        return false;
    }
}
