using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HRMS.Infrastructure.Persistence;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Services;

namespace HRMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Đọc chuỗi kết nối "MyCnn" từ appsettings.json của WebUI và nạp vào DbContext.
            // Dùng AddDbContextFactory (KHÔNG PHẢI AddDbContext) - tự động đăng ký CẢ 2:
            // IDbContextFactory<ApplicationDbContext> (EmployeeLookup dùng, tự tạo context riêng
            // mỗi lần gọi) VÀ ApplicationDbContext dạng Scoped (AuthService, EmployeeService...
            // vẫn inject trực tiếp được như cũ, không cần sửa gì thêm ở các service đó).
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("MyCnn")));

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<DbConcurrencyGate>();

            // Đăng ký dịch vụ xác thực AuthService
            services.AddScoped<IAuthService, AuthService>();

            // Đăng ký dịch vụ quản lý nhân viên EmployeeService
            services.AddScoped<IEmployeeService, EmployeeService>();

            // Đăng ký dịch vụ quản lý phòng ban
            services.AddScoped<IDepartmentService, DepartmentService>();

            // Đăng ký dịch vụ quản lý chức vụ
            services.AddScoped<IPositionService, PositionService>();

            // Đăng ký dịch vụ quản lý vai trò
            services.AddScoped<IRoleService, RoleService>();

            // Đăng ký dịch vụ đọc file Excel (Task 3.3)
            services.AddScoped<IExcelParserService, ExcelParserService>();

            // Đăng ký dịch vụ nghiệp vụ chấm công (Task 3.3)
            services.AddScoped<IAttendanceService, AttendanceService>();

            // Đăng ký dịch vụ quản lý hợp đồng
            services.AddScoped<IContractService, ContractService>();

            // Đăng ký dịch vụ tính lương (Task 3.4)
            services.AddScoped<IPayrollService, PayrollService>();

            // Module Request Workflow (Task 3.2) - tra cứu nhân viên/quản lý
            services.AddScoped<IEmployeeLookup, EmployeeLookup>();

            return services;
        }
    }
}