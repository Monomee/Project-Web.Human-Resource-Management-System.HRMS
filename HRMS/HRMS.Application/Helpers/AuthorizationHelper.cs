using HRMS.Application.DTOs;

namespace HRMS.Application.Helpers;


public static class AuthorizationHelper
{
    
    private static readonly Dictionary<string, int> RoleHierarchy = new()
    {
        { "Admin", 5 },
        { "HRM", 4 },
        { "HR", 3 },
        { "Manager", 2 },
        { "Executive", 2 },
        { "Employee", 1 }
    };

    
    public static bool CanEditAccount(AccountDto currentUser, AccountDto targetAccount)
    {
        
        if (currentUser.Id == -1)
        {
            return targetAccount.Id != -1; 
        }
        
       
        if (currentUser.Id == targetAccount.Id)
            return false;

        var currentRole = GetHighestRole(currentUser.RoleNames);
        var targetRole = GetHighestRole(targetAccount.RoleNames);

       
        if (currentRole == "Executive")
            return false;

        
        if (currentRole == "Admin")
            return true;

        
        if (currentRole == "Manager")
        {
            return targetRole == "Employee" && 
                   currentUser.DepartmentId == targetAccount.DepartmentId;
        }

        
        int currentLevel = GetRoleLevel(currentRole);
        int targetLevel = GetRoleLevel(targetRole);

        return currentLevel > targetLevel;
    }

    public static bool CanViewAccount(AccountDto currentUser, AccountDto targetAccount)
    {
       
        if (currentUser.Id == -1)
        {
            return true; 
        }
        
        var currentRole = GetHighestRole(currentUser.RoleNames);

        
        if (currentRole == "Admin" || currentRole == "HRM" || 
            currentRole == "HR" || currentRole == "Executive")
            return true;

        
        if (currentRole == "Manager")
        {
            var targetRole = GetHighestRole(targetAccount.RoleNames);
            return targetRole == "Employee" && 
                   currentUser.DepartmentId == targetAccount.DepartmentId;
        }

        
        return false;
    }

    
    public static List<AccountDto> FilterAccountsByPermission(
        AccountDto currentUser, 
        List<AccountDto> allAccounts)
    {
        
        if (currentUser.Id == -1)
        {
            return allAccounts; 
        }
        
        var currentRole = GetHighestRole(currentUser.RoleNames);

       
        if (currentRole == "Admin" || currentRole == "HRM" || 
            currentRole == "HR" || currentRole == "Executive")
        {
            return allAccounts;
        }

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

       
        return new List<AccountDto>();
    }

  
    private static string GetHighestRole(List<string> roles)
    {
        if (roles == null || !roles.Any())
            return "Employee";

        return roles
            .OrderByDescending(r => GetRoleLevel(r))
            .First();
    }

   
    private static int GetRoleLevel(string role)
    {
        return RoleHierarchy.TryGetValue(role, out int level) ? level : 0;
    }

    
    public static bool CanCreateAccount(AccountDto currentUser)
    {
        
        if (currentUser.Id == -1)
        {
            return true;
        }
        
        var currentRole = GetHighestRole(currentUser.RoleNames);
        return currentRole == "Admin";
    }

    
    public static bool CanDeleteAccount(AccountDto currentUser, AccountDto targetAccount)
    {

        if (currentUser.Id == -1)
        {
            return targetAccount.Id != -1;
        }
        
        
        if (currentUser.Id == targetAccount.Id)
            return false;
        
        var currentRole = GetHighestRole(currentUser.RoleNames);
        var targetRole = GetHighestRole(targetAccount.RoleNames);
        
       
        if (currentRole == "Admin")
            return true;
        
      
        if (currentRole == "HRM")
        {
            return targetRole == "HR" || targetRole == "Manager" || 
                   targetRole == "Executive" || targetRole == "Employee";
        }
        
        
        if (currentRole == "HR")
        {
            return targetRole == "Manager" || targetRole == "Executive" || 
                   targetRole == "Employee";
        }
        
        
        return false;
    }

   
    public static bool CanResetPassword(AccountDto currentUser, AccountDto targetAccount)
    {
       
        if (currentUser.Id == -1)
        {
            return targetAccount.Id != -1; 
        }
        
        
        if (currentUser.Id == targetAccount.Id)
            return false;
        
        var currentRole = GetHighestRole(currentUser.RoleNames);
        var targetRole = GetHighestRole(targetAccount.RoleNames);
        
       
        if (currentRole == "Admin")
            return true;
        
        
        if (currentRole == "HRM")
        {
            return targetRole == "HR" || targetRole == "Manager" || 
                   targetRole == "Executive" || targetRole == "Employee";
        }
        
       
        if (currentRole == "HR")
        {
            return targetRole == "Manager" || targetRole == "Executive" || 
                   targetRole == "Employee";
        }
        
       
        return false;
    }
}
