using System.Threading.Tasks;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
}
