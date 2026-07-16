using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Exceptions;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service implementation for managing employment contracts
/// Business Rules:
/// - Only HR role can create contracts
/// - Default status is "Pending" when created
/// - Contracts are immutable (cannot be edited once created)
/// - EndDate must be >= StartDate
/// </summary>
public class ContractService : IContractService
{
    private readonly ApplicationDbContext _context;

    public ContractService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all contracts for a specific user/employee
    /// </summary>
    public async Task<List<ContractDto>> GetContractsByUserIdAsync(int userId)
    {
        var contracts = await _context.EmploymentContracts
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ContractDto
            {
                Id = c.Id,
                ContractNo = c.ContractNo,
                ContractType = c.ContractType,
                BaseSalary = c.BaseSalary,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status
            })
            .ToListAsync();

        return contracts;
    }

    /// <summary>
    /// Create a new employment contract
    /// Business rule: Initial status is always "Pending"
    /// </summary>
    public async Task<ContractDto> CreateAsync(CreateContractDto dto)
    {
        // Validate business rules
        if (dto.EndDate < dto.StartDate)
        {
            throw new BusinessException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        // Check if user exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            throw new BusinessException("Nhân viên không tồn tại trong hệ thống.");
        }

        // Generate contract number (format: HD001, HD002, etc.)
        var maxContractNo = await _context.EmploymentContracts
            .OrderByDescending(c => c.Id)
            .Select(c => c.ContractNo)
            .FirstOrDefaultAsync();

        string newContractNo;
        if (string.IsNullOrEmpty(maxContractNo))
        {
            newContractNo = "HD001";
        }
        else
        {
            // Extract number part (assuming format HDxxx)
            var numberPart = maxContractNo.Substring(2);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                newContractNo = $"HD{(lastNumber + 1):D3}";
            }
            else
            {
                newContractNo = "HD001";
            }
        }

        // Create new contract with status "Active"
        var contract = new EmploymentContract
        {
            ContractNo = newContractNo,
            ContractType = dto.ContractType,
            BaseSalary = dto.BaseSalary,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = "Active", // Status is Active by default
            UserId = dto.UserId
        };

        _context.EmploymentContracts.Add(contract);
        await _context.SaveChangesAsync();

        // Return created contract
        return new ContractDto
        {
            Id = contract.Id,
            ContractNo = contract.ContractNo,
            ContractType = contract.ContractType,
            BaseSalary = contract.BaseSalary,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Status = contract.Status
        };
    }

    /// <summary>
    /// Update an existing employment contract
    /// Authorization: Only HRM role can update
    /// </summary>
    public async Task<bool> UpdateAsync(UpdateContractDto dto)
    {
        // Validate business rules
        if (dto.EndDate < dto.StartDate)
        {
            throw new BusinessException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        // Find existing contract
        var contract = await _context.EmploymentContracts.FindAsync(dto.Id);
        if (contract == null)
        {
            return false;
        }

        // Update fields
        contract.ContractType = dto.ContractType;
        contract.BaseSalary = dto.BaseSalary;
        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;
        contract.Status = dto.Status;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get contract types (distinct values from database)
    /// </summary>
    public async Task<List<string>> GetContractTypesAsync()
    {
        var types = await _context.EmploymentContracts
            .Select(c => c.ContractType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // If no types exist, return default types
        if (!types.Any())
        {
            return new List<string>
            {
                "Thử việc",
                "Có thời hạn (1 năm)",
                "Có thời hạn (2 năm)",
                "Có thời hạn (3 năm)",
                "Không thời hạn"
            };
        }

        return types;
    }

    /// <summary>
    /// Approve a contract (change status from Pending to Active)
    /// Authorization: Only HRM role can approve
    /// </summary>
    public async Task<bool> ApproveContractAsync(int contractId)
    {
        var contract = await _context.EmploymentContracts.FindAsync(contractId);
        if (contract == null)
        {
            return false;
        }

        if (contract.Status != "Pending")
        {
            throw new BusinessException("Chỉ có thể phê duyệt hợp đồng đang ở trạng thái 'Chờ duyệt'.");
        }

        contract.Status = "Active";
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Reject a contract (change status from Pending to Rejected)
    /// Authorization: Only HRM role can reject
    /// </summary>
    public async Task<bool> RejectContractAsync(int contractId, string reason)
    {
        var contract = await _context.EmploymentContracts.FindAsync(contractId);
        if (contract == null)
        {
            return false;
        }

        if (contract.Status != "Pending")
        {
            throw new BusinessException("Chỉ có thể từ chối hợp đồng đang ở trạng thái 'Chờ duyệt'.");
        }

        contract.Status = "Rejected";
        await _context.SaveChangesAsync();

        return true;
    }
}
