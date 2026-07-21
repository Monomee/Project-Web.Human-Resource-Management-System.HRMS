using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for managing employment contracts
/// </summary>
public interface IContractService
{
    /// <summary>
    /// Get all contracts for a specific user/employee
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of contracts ordered by StartDate descending</returns>
    Task<List<ContractDto>> GetContractsByUserIdAsync(int userId);

    /// <summary>
    /// Create a new employment contract
    /// Business rule: Initial status is always "Pending"
    /// </summary>
    /// <param name="dto">Contract creation data</param>
    /// <returns>Created contract with generated ContractNo</returns>
    Task<ContractDto> CreateAsync(CreateContractDto dto);

    /// <summary>
    /// Update an existing employment contract
    /// Authorization: Only HRM role can update
    /// </summary>
    /// <param name="dto">Contract update data</param>
    /// <returns>True if updated successfully, false if not found</returns>
    Task<bool> UpdateAsync(UpdateContractDto dto);

    /// <summary>
    /// Get contract types (distinct values from database)
    /// </summary>
    /// <returns>List of contract type names</returns>
    Task<List<string>> GetContractTypesAsync();

    /// <summary>
    /// Approve a contract (change status from Pending to Active)
    /// Authorization: Only HRM role can approve
    /// </summary>
    Task<bool> ApproveContractAsync(int contractId);

    /// <summary>
    /// Reject a contract (change status from Pending to Rejected)
    /// Authorization: Only HRM role can reject
    /// </summary>
    Task<bool> RejectContractAsync(int contractId, string reason);

    /// <summary>
    /// Terminate a contract (change status from Active to Terminated)
    /// Authorization: Only HRM role can terminate
    /// </summary>
    Task<bool> TerminateContractAsync(int contractId, string reason);

    /// <summary>
    /// Deactivate/Disable a contract (vô hiệu hóa - change Active contract to Terminated status)
    /// Authorization: Only HRM role can deactivate
    /// </summary>
    Task<bool> DeactivateContractAsync(int contractId, string reason);
}
