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
            // Đọc chuỗi kết nối "MyCnn" từ appsettings.json của WebUI và nạp vào DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("MyCnn")));

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

            return services;
        }
    }
}
