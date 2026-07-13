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
            // Services sẽ được đăng ký tại Infrastructure layer vì chúng phụ thuộc vào DbContext
            // Hiện tại các Service nghiệp vụ (AuthService, AttendanceService...) được đăng ký
            // tại Infrastructure/DependencyInjection.cs vì chúng cần truy cập ApplicationDbContext.
            // Nếu sau này có Service thuần Application (không cần DB), hãy thêm vào đây.

            return services;
        }
    }
}
