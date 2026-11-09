using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SMTManagement.Contracts.Models.DTOs;
using SMTManagement.WebAPI.Services;

namespace SMTManagement.WebAPI.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ManagementApiClient _api;

    public IndexModel(ManagementApiClient api)
    {
        _api = api;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? OrderSearch { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? BoardSearch { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public string? ComponentSearch { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty] public int OrderId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public string OrderName { get; set; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.BindProperty] public string? OrderDescription { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public DateTime OrderDate { get; set; } = DateTime.Today;
    [Microsoft.AspNetCore.Mvc.BindProperty] public int BoardId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public string BoardName { get; set; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.BindProperty] public string? BoardDescription { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public double BoardLength { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public double BoardWidth { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public int ComponentId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public string ComponentName { get; set; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.BindProperty] public string? ComponentDescription { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public int ComponentQuantity { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public int RelationshipOrderId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public int RelationshipBoardId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty] public int[] SelectedBoardIds { get; set; } = [];
    [Microsoft.AspNetCore.Mvc.BindProperty] public int[] SelectedComponentIds { get; set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostCreateOrderAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.CreateOrderAsync(new CreateOrderRequest(OrderName, OrderDescription, OrderDate), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostUpdateOrderAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.UpdateOrderAsync(OrderId, new UpdateOrderRequest(OrderName, OrderDescription, OrderDate), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostDeleteOrderAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.DeleteOrderAsync(OrderId, ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAssignOrderBoardsAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.SetOrderBoardsAsync(RelationshipOrderId, SelectedBoardIds.ToList(), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostDownloadOrderAsync(CancellationToken ct)
    {
        using var response = await _api.DownloadOrderAsync(OrderId, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = await response.Content.ReadAsStringAsync(ct);
            await LoadAsync(ct);
            return Page();
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        return File(content, "application/json", $"production-order-{OrderId}.json");
    }

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostCreateBoardAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.CreateBoardAsync(new CreateBoardRequest(BoardName, BoardDescription, BoardLength, BoardWidth), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostUpdateBoardAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.UpdateBoardAsync(BoardId, new UpdateBoardRequest(BoardName, BoardDescription, BoardLength, BoardWidth), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostDeleteBoardAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.DeleteBoardAsync(BoardId, ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAssignBoardComponentsAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.SetBoardComponentsAsync(RelationshipBoardId, SelectedComponentIds.ToList(), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostCreateComponentAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.CreateComponentAsync(new CreateComponentRequest(ComponentName, ComponentDescription, ComponentQuantity), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostUpdateComponentAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.UpdateComponentAsync(ComponentId, new UpdateComponentRequest(ComponentName, ComponentDescription, ComponentQuantity), ct), ct);

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostDeleteComponentAsync(CancellationToken ct) =>
        await MutateAsync(() => _api.DeleteComponentAsync(ComponentId, ct), ct);

    private async Task<Microsoft.AspNetCore.Mvc.IActionResult> MutateAsync(Func<Task<HttpResponseMessage>> action, CancellationToken ct)
    {
        using var response = await action();
        if (response.IsSuccessStatusCode)
            return RedirectToPage();

        ErrorMessage = await response.Content.ReadAsStringAsync(ct);
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Orders = await _api.GetOrdersAsync(OrderSearch, ct) ?? [];
        Boards = await _api.GetBoardsAsync(BoardSearch, ct) ?? [];
        Components = await _api.GetComponentsAsync(ComponentSearch, ct) ?? [];
        AvailableBoards = await _api.GetBoardsAsync(null, ct) ?? [];
        AvailableComponents = await _api.GetComponentsAsync(null, ct) ?? [];
    }

    public List<SMTManagement.Contracts.Models.Order> Orders { get; private set; } = [];
    public List<SMTManagement.Contracts.Models.Board> Boards { get; private set; } = [];
    public List<SMTManagement.Contracts.Models.Component> Components { get; private set; } = [];
    public List<SMTManagement.Contracts.Models.Board> AvailableBoards { get; private set; } = [];
    public List<SMTManagement.Contracts.Models.Component> AvailableComponents { get; private set; } = [];
}
