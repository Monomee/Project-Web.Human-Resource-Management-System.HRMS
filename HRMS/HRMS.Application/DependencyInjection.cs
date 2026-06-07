using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Hiện tại thư mục Features chưa có Service nào nên tạm thời để trống ở đây.
            // Sau này khi viết EmployeeService, AttendanceService... sẽ thêm vào dưới này.

            return services;
        }
    }
}
