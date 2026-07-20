using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Exceptions;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
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
                        Status = c.Status
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

            // Create Account for the user
            var account = new Account
            {
                Username = dto.EmailCompany.Split('@')[0], // Use email prefix as username
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"), // Default password
                Status = true,
                UserId = user.Id
            };

            var role = await _context.Roles.FindAsync(dto.RoleId!.Value);
            if (role != null)
            {
                account.Roles.Add(role);
            }

            _context.Accounts.Add(account);
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

        public async Task<bool> UpdateAsync(UpdateEmployeeDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Account)
                    .ThenInclude(a => a!.Roles)
                .FirstOrDefaultAsync(u => u.Id == dto.Id);

            if (user == null)
                return false;

            // Update User fields
            user.FullName = dto.FullName;
            user.EmailCompany = dto.EmailCompany;
            user.Phone = dto.Phone;
            user.Gender = dto.Gender;
            user.DepartmentId = dto.DepartmentId!.Value;
            user.PositionId = dto.PositionId!.Value;
            user.DateOfBirth = dto.DateOfBirth;
            user.Status = dto.Status;

            // Update Account Role if account exists and role changed
            if (user.Account != null && dto.RoleId.HasValue)
            {
                // Clear existing roles
                user.Account.Roles.Clear();

                // Add new role
                var role = await _context.Roles.FindAsync(dto.RoleId.Value);
                if (role != null)
                {
                    user.Account.Roles.Add(role);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
