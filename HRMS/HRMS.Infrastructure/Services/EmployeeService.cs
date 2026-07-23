using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Exceptions;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace HRMS.Infrastructure.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountService _accountService;

        public EmployeeService(ApplicationDbContext context, IAccountService accountService)
        {
            _context = context;
            _accountService = accountService;
        }

        public async Task<List<EmployeeDto>> GetEmployeesWithDetailsAsync()
        {
            return await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .OrderBy(u => u.EmployeeCode)
                .Select(u => new EmployeeDto
                {
                    Id = u.Id,
                    EmployeeCode = u.EmployeeCode,
                    FullName = u.FullName,
                    EmailCompany = u.EmailCompany,
                    Phone = u.Phone,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    Status = u.Status,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department.Name,
                    PositionId = u.PositionId,
                    PositionName = u.Position.Name
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateEmployeeStatusAsync(int employeeId, bool newStatus)
        {
            var user = await _context.Users
                .Include(u => u.EmploymentContracts)
                .FirstOrDefaultAsync(u => u.Id == employeeId);

            if (user == null)
                return false;

            // BUSINESS RULE 1: Chặn khóa nhân viên có hợp đồng đang active
            if (newStatus == false) // Muốn chuyển sang "Đã nghỉ"
            {
                var hasActiveContract = user.EmploymentContracts
                    .Any(c => c.Status == "Active");

                if (hasActiveContract)
                {
                    throw new BusinessException(
                        $"Không thể chuyển nhân viên {user.FullName} sang trạng thái Đã nghỉ vì còn hợp đồng đang hoạt động. " +
                        "Vui lòng kết thúc tất cả hợp đồng trước khi thực hiện thao tác này.");
                }
            }

            user.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<EmployeeDetailDto> GetEmployeeDetailByIdAsync(int employeeId)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Include(u => u.EmploymentContracts)
                .FirstOrDefaultAsync(u => u.Id == employeeId);

            if (user == null)
                throw new BusinessException($"Không tìm thấy nhân viên với ID = {employeeId}");

            return new EmployeeDetailDto
            {
                Id = user.Id,
                EmployeeCode = user.EmployeeCode,
                FullName = user.FullName,
                EmailCompany = user.EmailCompany,
                Phone = user.Phone,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Status = user.Status,
                DepartmentName = user.Department.Name,
                DepartmentCode = user.Department.Code,
                PositionName = user.Position.Name,
                PositionCode = user.Position.Code,
                Contracts = user.EmploymentContracts
                    .OrderByDescending(c => c.StartDate)
                    .Select(c => new ContractDto
                    {
                        Id = c.Id,
                        ContractNo = c.ContractNo,
                        ContractType = c.ContractType,
                        BaseSalary = c.BaseSalary,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Status = c.Status,
                        Reason = c.Reason
                    })
                    .ToList()
            };
        }

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
        {
            // Generate Employee Code
            var lastEmployee = await _context.Users
                .OrderByDescending(u => u.EmployeeCode)
                .FirstOrDefaultAsync();

            string newEmployeeCode = "NV001";
            if (lastEmployee != null && lastEmployee.EmployeeCode.StartsWith("NV"))
            {
                var lastNumber = int.Parse(lastEmployee.EmployeeCode.Substring(2));
                newEmployeeCode = $"NV{(lastNumber + 1):D3}";
            }

            // Create User entity
            var user = new User
            {
                EmployeeCode = newEmployeeCode,
                FullName = dto.FullName,
                EmailCompany = dto.EmailCompany,
                Phone = dto.Phone,
                Gender = dto.Gender ?? true, // Default to Male if not specified
                DateOfBirth = dto.StartDate, // Using StartDate as placeholder for DateOfBirth
                Status = true, // New employee is active by default
                DepartmentId = dto.DepartmentId!.Value,
                PositionId = dto.PositionId!.Value
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // AUTO-CREATE ACCOUNT: Generate username from FullName
            // Example: "Long Tuấn Duy" -> "longtuanduy"
            string baseUsername = RemoveVietnameseDiacritics(dto.FullName)
                .ToLower()
                .Replace(" ", "");

            // Check for username conflicts and append number if exists
            string username = baseUsername;
            int suffix = 1;
            while (await _accountService.UsernameExistsAsync(username))
            {
                username = $"{baseUsername}{suffix}";
                suffix++;
            }

            // Get Employee role (default role for new employees)
            var employeeRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Employee");

            if (employeeRole == null)
            {
                throw new BusinessException("Không tìm thấy vai trò 'Employee' trong hệ thống.");
            }

            // Create account using AccountService
            try
            {
                var createAccountDto = new CreateAccountDto
                {
                    Username = username,
                    Password = "Password123", // Default password
                    UserId = user.Id,
                    RoleIds = new List<int> { employeeRole.Id }
                };

                await _accountService.CreateAsync(createAccountDto);
            }
            catch (Exception ex)
            {
                // If account creation fails, we should consider rolling back the user creation
                // For now, we'll just log and continue (user created without account)
                throw new BusinessException($"Đã tạo nhân viên thành công nhưng không thể tạo tài khoản: {ex.Message}");
            }

            // Initialize LeaveBalance for the current year
            var currentYear = DateTime.Now.Year;
            var leaveBalance = new LeaveBalance
            {
                UserId = user.Id,
                Year = currentYear,
                TotalDays = 12,
                UsedDays = 0,
                RemainingDays = 12
            };
            _context.LeaveBalances.Add(leaveBalance);
            await _context.SaveChangesAsync();

            // Return EmployeeDto
            var department = await _context.Departments.FindAsync(dto.DepartmentId!.Value);
            var position = await _context.Positions.FindAsync(dto.PositionId!.Value);

            return new EmployeeDto
            {
                Id = user.Id,
                EmployeeCode = user.EmployeeCode,
                FullName = user.FullName,
                EmailCompany = user.EmailCompany,
                Phone = user.Phone,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Status = user.Status,
                DepartmentId = user.DepartmentId,
                DepartmentName = department?.Name ?? "",
                PositionId = user.PositionId,
                PositionName = position?.Name ?? ""
            };
        }

        /// <summary>
        /// Helper method to remove Vietnamese diacritics and convert to plain ASCII
        /// Example: "Long Tuấn Duy" -> "Long Tuan Duy"
        /// </summary>
        private string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Normalize to decomposed form (separates base characters from diacritics)
            string normalized = text.Normalize(NormalizationForm.FormD);
            
            StringBuilder result = new StringBuilder();
            
            foreach (char c in normalized)
            {
                // Keep only non-spacing marks removed (diacritics are in this category)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            // Additional Vietnamese-specific replacements
            string output = result.ToString().Normalize(NormalizationForm.FormC);
            output = output.Replace('đ', 'd').Replace('Đ', 'D');
            
            return output;
        }

        public async Task<bool> UpdateAsync(UpdateEmployeeDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.Id);

            if (user == null)
                return false;

            // Update User fields only (Account updates are now separate)
            user.FullName = dto.FullName;
            user.EmailCompany = dto.EmailCompany;
            user.Phone = dto.Phone;
            user.Gender = dto.Gender;
            user.DepartmentId = dto.DepartmentId!.Value;
            user.PositionId = dto.PositionId!.Value;
            user.DateOfBirth = dto.DateOfBirth;
            user.Status = dto.Status;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
