using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.Services;

public sealed class BoardService : IBoardService
{
    private readonly IBoardRepository _repository;

    public BoardService(IBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Board>> GetAllAsync(string? search = null, CancellationToken ct = default) =>
        _repository.GetAllAsync(search, ct);

    public Task<Board?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _repository.GetByIdAsync(id, ct);

    public Task<int> CreateAsync(CreateBoardRequest request, CancellationToken ct = default) =>
        _repository.CreateAsync(request, ct);

    public Task<bool> UpdateAsync(int id, UpdateBoardRequest request, CancellationToken ct = default) =>
        _repository.UpdateAsync(id, request, ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);

    public Task<bool> SetComponentsAsync(int boardId, List<int> componentIds, CancellationToken ct = default) =>
        _repository.SetComponentsAsync(boardId, componentIds, ct);
}
