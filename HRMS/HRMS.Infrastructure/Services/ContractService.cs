using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Exceptions;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;


public class ContractService : IContractService
{
    private readonly ApplicationDbContext _context;

    public ContractService(ApplicationDbContext context)
    {
        _context = context;
    }

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
                Status = c.Status,
                Reason = c.Reason
            })
            .ToListAsync();

        return contracts;
    }

    
    public async Task<ContractDto> CreateAsync(CreateContractDto dto)
    {
       
        if (dto.EndDate < dto.StartDate)
        {
            throw new BusinessException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

       
        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            throw new BusinessException("Nhân viên không tồn tại trong hệ thống.");
        }

       
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

        
        var contract = new EmploymentContract
        {
            ContractNo = newContractNo,
            ContractType = dto.ContractType,
            BaseSalary = dto.BaseSalary,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = "Pending", 
            UserId = dto.UserId
        };

        _context.EmploymentContracts.Add(contract);
        await _context.SaveChangesAsync();

 
        return new ContractDto
        {
            Id = contract.Id,
            ContractNo = contract.ContractNo,
            ContractType = contract.ContractType,
            BaseSalary = contract.BaseSalary,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Status = contract.Status,
            Reason = contract.Reason
        };
    }

    
    public async Task<bool> UpdateAsync(UpdateContractDto dto)
    {
        
        if (dto.EndDate < dto.StartDate)
        {
            throw new BusinessException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        
        var contract = await _context.EmploymentContracts.FindAsync(dto.Id);
        if (contract == null)
        {
            return false;
        }

        
        if (contract.Status != "Pending" && contract.Status != "Rejected")
        {
            throw new BusinessException("Chỉ có thể chỉnh sửa hợp đồng đang ở trạng thái 'Chờ duyệt' hoặc 'Từ chối'.");
        }

        
        contract.ContractType = dto.ContractType;
        contract.BaseSalary = dto.BaseSalary;
        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;

        
        if (contract.Status == "Rejected")
        {
            contract.Status = "Pending";
        }
        else
        {
            contract.Status = dto.Status;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetContractTypesAsync()
    {
        var types = await _context.EmploymentContracts
            .Select(c => c.ContractType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        
        if (!types.Any())
        {
            return new List<string>
            {
                "Thử việc",
                "Có thời hạn",
                "Không thời hạn"
            };
        }

        return types;
    }

    
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
        contract.Reason = reason; 
        await _context.SaveChangesAsync();

        return true;
    }

    
    public async Task<bool> TerminateContractAsync(int contractId, string reason)
    {
        var contract = await _context.EmploymentContracts.FindAsync(contractId);
        if (contract == null)
        {
            return false;
        }

        if (contract.Status != "Active")
        {
            throw new BusinessException("Chỉ có thể chấm dứt hợp đồng đang ở trạng thái 'Hiệu lực'.");
        }

        // Terminate contract by setting EndDate to today
        contract.EndDate = DateOnly.FromDateTime(DateTime.Today);
        contract.Status = "Terminated";
        contract.Reason = reason; // Save termination reason

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deactivate/Disable a contract (vô hiệu hóa)
    /// Changes status from Active to Terminated without modifying EndDate
    /// </summary>
    public async Task<bool> DeactivateContractAsync(int contractId, string reason)
    {
        var contract = await _context.EmploymentContracts.FindAsync(contractId);
        if (contract == null)
        {
            return false;
        }

        if (contract.Status != "Active")
        {
            throw new BusinessException("Chỉ có thể vô hiệu hóa hợp đồng đang ở trạng thái 'Hiệu lực'.");
        }

        // Deactivate by changing status to Terminated
        contract.Status = "Terminated";
        contract.Reason = reason; // Save deactivation reason

        await _context.SaveChangesAsync();

        return true;
    }
}
