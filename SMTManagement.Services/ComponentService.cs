using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.Services;

public sealed class ComponentService : IComponentService
{
    private readonly IComponentRepository _repository;

    public ComponentService(IComponentRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Component>> GetAllAsync(string? search = null, CancellationToken ct = default) =>
        _repository.GetAllAsync(search, ct);

    public Task<Component?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<int> CreateAsync(CreateComponentRequest request, CancellationToken ct = default) =>
        _repository.CreateAsync(request, ct);

    public Task<bool> UpdateAsync(int id, UpdateComponentRequest request, CancellationToken ct = default) =>
        _repository.UpdateAsync(id, request, ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);
}
