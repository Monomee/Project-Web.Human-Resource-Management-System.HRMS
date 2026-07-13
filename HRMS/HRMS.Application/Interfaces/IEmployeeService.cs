using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeDto>> GetEmployeesWithDetailsAsync();
        Task<bool> UpdateEmployeeStatusAsync(int employeeId, bool newStatus);
        Task<EmployeeDetailDto> GetEmployeeDetailByIdAsync(int employeeId);
    }
}
