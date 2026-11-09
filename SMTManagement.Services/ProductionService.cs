namespace SMTManagement.Services;

using Microsoft.Extensions.Logging;
using SMTManagement.Contracts.Interfaces.Services;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;

public class ProductionService : IProductionService
{
    private readonly ILogger<IProductionService> _logger;

    private readonly IOrderRepository _orders;

    public ProductionService(IOrderRepository orders, ILogger<IProductionService> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    public async Task<byte[]?> ExportOrderAsync(int orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("Production export requested. OrderId: {OrderId}", orderId);

        var order = await _orders.GetByIdAsync(orderId, ct);

        if (order is null)
        {
            _logger.LogWarning("Production export failed because Order was not found. OrderId: {OrderId}", orderId);

            return null;
        }

        var document = new
        {
            ExportId = Guid.NewGuid(),
            ExportedAt = DateTime.UtcNow,
            Order = order
        };

        var result = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(document);

        _logger.LogInformation(
            "Production export generated. OrderId: {OrderId}, Boards: {BoardCount}, FileSize: {FileSize}",
            orderId,
            order.Boards.Count,
            result.Length);

        return result;
    }
}
