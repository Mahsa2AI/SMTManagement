using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.Contracts.Interfaces.SqliteAdapter;

public interface IBoardRepository
{
    Task<List<Board>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Board?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateBoardRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, UpdateBoardRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> SetComponentsAsync(int boardId, List<int> componentIds, CancellationToken ct = default);
}

