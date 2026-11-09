namespace SMTManagement.Contracts.Interfaces.Services;

public interface IProductionService
{
    Task<byte[]?> ExportOrderAsync(int orderId, CancellationToken ct = default);
}
