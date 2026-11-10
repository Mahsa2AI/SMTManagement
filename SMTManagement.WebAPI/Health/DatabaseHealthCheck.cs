using Microsoft.Extensions.Diagnostics.HealthChecks;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;

namespace SMTManagement.WebAPI.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ISqliteConnectionFactory connectionFactory,
        ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection =
                await _connectionFactory.CreateConnectionAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result) == 1
                ? HealthCheckResult.Healthy("SQLite database is available.")
                : HealthCheckResult.Unhealthy("SQLite database health query returned an unexpected result.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite database health check failed.");
            return HealthCheckResult.Unhealthy("SQLite database is unavailable.", ex);
        }
    }
}
