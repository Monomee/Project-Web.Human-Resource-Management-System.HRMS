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
                    DepartmentName = u.Department.Name,
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
    }
}
