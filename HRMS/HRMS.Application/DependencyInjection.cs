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

            return services;
        }
    }
}
