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

            // Create User entity only (Account creation is now separate)
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
