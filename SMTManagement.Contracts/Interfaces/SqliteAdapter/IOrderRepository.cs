using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;
namespace SMTManagement.Contracts.Interfaces.SqliteAdapter;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, UpdateOrderRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> SetBoardsAsync(int orderId, List<int> boardIds, CancellationToken ct = default);
}

