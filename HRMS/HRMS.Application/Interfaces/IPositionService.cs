using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

public interface IPositionService
{
    Task<List<PositionDto>> GetAllAsync();
    Task<PositionDto?> GetByIdAsync(int id);
}
