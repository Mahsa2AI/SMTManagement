using SMTManagement.Contracts.Models;

namespace SMTManagement.WebAPI.Services;

public interface IManagementPageDataService
{
    Task<ManagementPageData> LoadAsync(
        string? orderSearch,
        string? boardSearch,
        string? componentSearch,
        CancellationToken ct);
}

public sealed record ManagementPageData(
    List<Order> Orders,
    List<Board> Boards,
    List<Component> Components,
    List<Board> AvailableBoards,
    List<Component> AvailableComponents);
