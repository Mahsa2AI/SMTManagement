using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Order>> GetAllAsync(string? search = null, CancellationToken ct = default) =>
        _repository.GetAllAsync(search, ct);

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<int> CreateAsync(CreateOrderRequest request, CancellationToken ct = default) =>
        _repository.CreateAsync(request, ct);

    public Task<bool> UpdateAsync(int id, UpdateOrderRequest request, CancellationToken ct = default) =>
        _repository.UpdateAsync(id, request, ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);

    public Task<bool> SetBoardsAsync(int orderId, List<int> boardIds, CancellationToken ct = default) =>
        _repository.SetBoardsAsync(orderId, boardIds, ct);
}
