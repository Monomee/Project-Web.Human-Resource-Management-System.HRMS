using HRMS.Application.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _context;

    public RoleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        return await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name
            })
            .ToListAsync();
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        
        if (role == null)
            return null;

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name
        };
    }
}
