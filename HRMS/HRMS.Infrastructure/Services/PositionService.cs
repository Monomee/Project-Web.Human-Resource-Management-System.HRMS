using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

public class PositionService : IPositionService
{
    private readonly ApplicationDbContext _context;

    public PositionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PositionDto>> GetAllAsync()
    {
        return await _context.Positions
            .OrderBy(p => p.JobLevel)
            .ThenBy(p => p.Name)
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                JobLevel = p.JobLevel
            })
            .ToListAsync();
    }

    public async Task<PositionDto?> GetByIdAsync(int id)
    {
        var position = await _context.Positions.FindAsync(id);
        
        if (position == null)
            return null;

        return new PositionDto
        {
            Id = position.Id,
            Code = position.Code,
            Name = position.Name,
            JobLevel = position.JobLevel
        };
    }
}
