using System.Net.Http.Headers;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.WebAPI.Services;

public sealed class ManagementApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public ManagementApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public Task<List<Order>?> GetOrdersAsync(string? search, CancellationToken ct) =>
        GetAsync<List<Order>>(WithSearch("api/orders", search), ct);

    public Task<List<Board>?> GetBoardsAsync(string? search, CancellationToken ct) =>
        GetAsync<List<Board>>(WithSearch("api/boards", search), ct);

    public Task<List<Component>?> GetComponentsAsync(string? search, CancellationToken ct) =>
        GetAsync<List<Component>>(WithSearch("api/components", search), ct);

    public Task<HttpResponseMessage> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, "api/orders", request, ct);

    public Task<HttpResponseMessage> UpdateOrderAsync(int id, UpdateOrderRequest request, CancellationToken ct) =>
        SendAsync(HttpMethod.Put, $"api/orders/{id}", request, ct);

    public Task<HttpResponseMessage> DeleteOrderAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Delete, $"api/orders/{id}", null, ct);

    public Task<HttpResponseMessage> SetOrderBoardsAsync(int id, List<int> boardIds, CancellationToken ct) =>
        SendAsync(HttpMethod.Put, $"api/orders/{id}/boards", new UpdateOrderBoardsRequest(boardIds), ct);

    public Task<HttpResponseMessage> DownloadOrderAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"api/orders/{id}/production-export", null, ct);

    public Task<HttpResponseMessage> CreateBoardAsync(CreateBoardRequest request, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, "api/boards", request, ct);

    public Task<HttpResponseMessage> UpdateBoardAsync(int id, UpdateBoardRequest request, CancellationToken ct) =>
        SendAsync(HttpMethod.Put, $"api/boards/{id}", request, ct);

    public Task<HttpResponseMessage> DeleteBoardAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Delete, $"api/boards/{id}", null, ct);

    public Task<HttpResponseMessage> SetBoardComponentsAsync(int id, List<int> componentIds, CancellationToken ct) =>
        SendAsync(HttpMethod.Put, $"api/boards/{id}/components", new UpdateBoardComponentsRequest(componentIds), ct);

    public Task<HttpResponseMessage> CreateComponentAsync(CreateComponentRequest request, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, "api/components", request, ct);

    public Task<HttpResponseMessage> UpdateComponentAsync(int id, UpdateComponentRequest request, CancellationToken ct) =>
        SendAsync(HttpMethod.Put, $"api/components/{id}", request, ct);

    public Task<HttpResponseMessage> DeleteComponentAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Delete, $"api/components/{id}", null, ct);

    private static string WithSearch(string path, string? search) =>
        string.IsNullOrWhiteSpace(search)
            ? path
            : $"{path}?search={Uri.EscapeDataString(search.Trim())}";

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        var request = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("No active HTTP request is available.");

        using var message = new HttpRequestMessage(method, $"{request.Scheme}://{request.Host}/{path}");
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", _configuration["ApiAuthentication:Token"]);

        if (body is not null)
            message.Content = JsonContent.Create(body);

        return await _httpClient.SendAsync(message, ct);
    }
}