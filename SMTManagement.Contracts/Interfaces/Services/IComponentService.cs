using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.Contracts.Interfaces.Services;

public interface IComponentService
{
    Task<List<Component>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Component?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateComponentRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, UpdateComponentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
