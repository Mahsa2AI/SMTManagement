using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models.DTOs;
using SMTManagement.SqliteAdapter;

namespace SMTManagement.SqliteAdapter.Repositories;

public class ComponentRepository : IComponentRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<ComponentRepository> _logger;

    public ComponentRepository(
        ISqliteConnectionFactory factory,
        ILogger<ComponentRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Contracts.Models.Component>> GetAllAsync(
        string? search = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving components. Search: {Search}",
            search);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT Id, Name, Description, Quantity, CreatedAt
                FROM Components
                WHERE @Search IS NULL
                   OR Name LIKE @Pattern
                   OR Description LIKE @Pattern
                ORDER BY Id DESC;
                """;

            command.Parameters.AddWithValue(
                "@Search", (object?)search ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@Pattern", $"%{search ?? string.Empty}%");

            var result = new List<Contracts.Models.Component>();

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
                    result.Add(SqliteEntityMapper.MapComponent(reader));

            _logger.LogDebug(
                "Retrieved {ComponentCount} components.",
                result.Count);

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Retrieving components was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve components. Search: {Search}",
                search);

            throw;
        }
    }

    public async Task<Contracts.Models.Component?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving component with ID {ComponentId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT Id, Name, Description, Quantity, CreatedAt
                FROM Components WHERE Id = @Id;
                """;

            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync(ct);

            var component = await reader.ReadAsync(ct)
                ? SqliteEntityMapper.MapComponent(reader)
                : null;

            if (component is null)
            {
                _logger.LogWarning(
                    "Component with ID {ComponentId} was not found.",
                    id);

                return null;
            }

            _logger.LogDebug(
                "Successfully retrieved component with ID {ComponentId}.",
                id);

            return component;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Retrieving component {ComponentId} was cancelled.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve component with ID {ComponentId}.",
                id);

            throw;
        }
    }

    public async Task<int> CreateAsync(
        CreateComponentRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating component with name {ComponentName}.",
            request.Name);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Components
                    (Name, Description, Quantity, CreatedAt)
                VALUES
                    (@Name, @Description, @Quantity, @CreatedAt);

                SELECT last_insert_rowid();
                """;

            command.Parameters.AddWithValue("@Name", request.Name.Trim());
            command.Parameters.AddWithValue(
                "@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Quantity", request.Quantity);
            command.Parameters.AddWithValue(
                "@CreatedAt", DateTime.UtcNow.ToString("O"));

            var id = Convert.ToInt32(
                await command.ExecuteScalarAsync(ct));

            _logger.LogInformation(
                "Component created successfully. ComponentId: {ComponentId}, ComponentName: {ComponentName}",
                id,
                request.Name);

            return id;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Creating component {ComponentName} was cancelled.",
                request.Name);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create component with name {ComponentName}.",
                request.Name);

            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateComponentRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Updating component with ID {ComponentId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                UPDATE Components
                SET Name = @Name,
                    Description = @Description,
                    Quantity = @Quantity
                WHERE Id = @Id;
                """;

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name.Trim());
            command.Parameters.AddWithValue(
                "@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Quantity", request.Quantity);

            var updated = await command.ExecuteNonQueryAsync(ct) > 0;

            if (!updated)
            {
                _logger.LogWarning(
                    "Component update failed. Component with ID {ComponentId} was not found.",
                    id);

                return false;
            }

            _logger.LogInformation(
                "Component with ID {ComponentId} updated successfully.",
                id);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Updating component {ComponentId} was cancelled.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update component with ID {ComponentId}.",
                id);

            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Deleting component with ID {ComponentId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Components WHERE Id = @Id;";

            command.Parameters.AddWithValue("@Id", id);

            var deleted = await command.ExecuteNonQueryAsync(ct) > 0;

            if (!deleted)
            {
                _logger.LogWarning(
                    "Component deletion failed. Component with ID {ComponentId} was not found.",
                    id);

                return false;
            }

            _logger.LogInformation(
                "Component with ID {ComponentId} deleted successfully.",
                id);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Deleting component {ComponentId} was cancelled.",
                id);

            throw;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            _logger.LogWarning(
                ex,
                "Cannot delete component {ComponentId} because it is referenced by another record.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete component with ID {ComponentId}.",
                id);

            throw;
        }
    }
}
