namespace SMTManagement.WebAPI.Services;

public sealed class ManagementPageDataService : IManagementPageDataService
{
    private readonly ManagementApiClient _api;

    public ManagementPageDataService(ManagementApiClient api)
    {
        _api = api;
    }

    public async Task<ManagementPageData> LoadAsync(
        string? orderSearch,
        string? boardSearch,
        string? componentSearch,
        CancellationToken ct)
    {
        var ordersTask = _api.GetOrdersAsync(orderSearch, ct);
        var boardsTask = _api.GetBoardsAsync(boardSearch, ct);
        var componentsTask = _api.GetComponentsAsync(componentSearch, ct);
        var availableBoardsTask = string.IsNullOrWhiteSpace(boardSearch)
            ? boardsTask
            : _api.GetBoardsAsync(null, ct);
        var availableComponentsTask = string.IsNullOrWhiteSpace(componentSearch)
            ? componentsTask
            : _api.GetComponentsAsync(null, ct);

        await Task.WhenAll(
            ordersTask,
            boardsTask,
            componentsTask,
            availableBoardsTask,
            availableComponentsTask);

        return new ManagementPageData(
            await ordersTask ?? [],
            await boardsTask ?? [],
            await componentsTask ?? [],
            await availableBoardsTask ?? [],
            await availableComponentsTask ?? []);
    }
}
