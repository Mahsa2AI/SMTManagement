using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;
using SMTManagement.SqliteAdapter;

namespace SMTManagement.SqliteAdapter.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<BoardRepository> _logger;

    public BoardRepository(
        ISqliteConnectionFactory factory,
        ILogger<BoardRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Board>> GetAllAsync(
        string? search = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving boards. Search: {Search}",
            search);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT Id, Name, Description, Length, Width, CreatedAt
                FROM Boards
                WHERE @Search IS NULL
                   OR Name LIKE @Pattern
                   OR Description LIKE @Pattern
                ORDER BY Id DESC;
                """;

            command.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
            command.Parameters.AddWithValue("@Pattern", $"%{search ?? string.Empty}%");

            var result = new List<Board>();

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
                    result.Add(SqliteEntityMapper.MapBoard(reader));

            foreach (var board in result)
            {
                await using var componentCommand = connection.CreateCommand();
                componentCommand.CommandText = """
                    SELECT c.Id, c.Name, c.Description, c.Quantity, c.CreatedAt
                    FROM Components c
                    INNER JOIN BoardComponents bc ON bc.ComponentId = c.Id
                    WHERE bc.BoardId = @BoardId
                    ORDER BY c.Id;
                    """;
                componentCommand.Parameters.AddWithValue("@BoardId", board.Id);

                await using var componentReader = await componentCommand.ExecuteReaderAsync(ct);
                while (await componentReader.ReadAsync(ct))
                    board.Components.Add(SqliteEntityMapper.MapComponent(componentReader));
            }

            _logger.LogDebug(
                "Retrieved {BoardCount} boards.",
                result.Count);

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Retrieving boards was cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve boards. Search: {Search}",
                search);

            throw;
        }
    }

    public async Task<Board?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving board with ID {BoardId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT Id, Name, Description, Length, Width, CreatedAt
                FROM Boards WHERE Id = @Id;
                """;

            command.Parameters.AddWithValue("@Id", id);

            Board? board;

            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                board = await reader.ReadAsync(ct)
                    ? SqliteEntityMapper.MapBoard(reader)
                    : null;
            }

            if (board is null)
            {
                _logger.LogWarning(
                    "Board with ID {BoardId} was not found.",
                    id);

                return null;
            }

            await using var componentsCommand = connection.CreateCommand();

            componentsCommand.CommandText = """
                SELECT c.Id, c.Name, c.Description, c.Quantity, c.CreatedAt
                FROM Components c
                INNER JOIN BoardComponents bc ON bc.ComponentId = c.Id
                WHERE bc.BoardId = @BoardId
                ORDER BY c.Id;
                """;

            componentsCommand.Parameters.AddWithValue("@BoardId", id);

            await using var componentsReader =
                await componentsCommand.ExecuteReaderAsync(ct);

            while (await componentsReader.ReadAsync(ct))
                board.Components.Add(
                    SqliteEntityMapper.MapComponent(componentsReader));

            _logger.LogDebug(
                "Retrieved board {BoardId} with {ComponentCount} components.",
                id,
                board.Components.Count);

            return board;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Retrieving board {BoardId} was cancelled.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve board with ID {BoardId}.",
                id);

            throw;
        }
    }

    public async Task<int> CreateAsync(
        CreateBoardRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating board with name {BoardName}.",
            request.Name);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Boards
                    (Name, Description, Length, Width, CreatedAt)
                VALUES
                    (@Name, @Description, @Length, @Width, @CreatedAt);

                SELECT last_insert_rowid();
                """;

            command.Parameters.AddWithValue("@Name", request.Name.Trim());
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Length", request.Length);
            command.Parameters.AddWithValue("@Width", request.Width);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

            var id = Convert.ToInt32(
                await command.ExecuteScalarAsync(ct));

            _logger.LogInformation(
                "Board created successfully. BoardId: {BoardId}, BoardName: {BoardName}",
                id,
                request.Name);

            return id;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Creating board {BoardName} was cancelled.",
                request.Name);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create board with name {BoardName}.",
                request.Name);

            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateBoardRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Updating board with ID {BoardId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                UPDATE Boards
                SET Name = @Name,
                    Description = @Description,
                    Length = @Length,
                    Width = @Width
                WHERE Id = @Id;
                """;

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name.Trim());
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Length", request.Length);
            command.Parameters.AddWithValue("@Width", request.Width);

            var updated = await command.ExecuteNonQueryAsync(ct) > 0;

            if (!updated)
            {
                _logger.LogWarning(
                    "Board update failed. Board with ID {BoardId} was not found.",
                    id);

                return false;
            }

            _logger.LogInformation(
                "Board with ID {BoardId} updated successfully.",
                id);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Updating board {BoardId} was cancelled.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update board with ID {BoardId}.",
                id);

            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Deleting board with ID {BoardId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Boards WHERE Id = @Id;";

            command.Parameters.AddWithValue("@Id", id);

            var deleted = await command.ExecuteNonQueryAsync(ct) > 0;

            if (!deleted)
            {
                _logger.LogWarning(
                    "Board deletion failed. Board with ID {BoardId} was not found.",
                    id);

                return false;
            }

            _logger.LogInformation(
                "Board with ID {BoardId} deleted successfully.",
                id);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Deleting board {BoardId} was cancelled.",
                id);

            throw;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            _logger.LogWarning(
                ex,
                "Cannot delete board {BoardId} because it is referenced by another record.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete board with ID {BoardId}.",
                id);

            throw;
        }
    }

    public async Task<bool> SetComponentsAsync(
        int boardId,
        List<int> componentIds,
        CancellationToken ct = default)
    {
        var ids = componentIds.Distinct().ToList();

        _logger.LogInformation(
            "Assigning {ComponentCount} components to board {BoardId}.",
            ids.Count,
            boardId);

        await using var connection =
            await _factory.CreateConnectionAsync(ct);

        using var transaction = connection.BeginTransaction();

        try
        {
            if (!await SqliteRepositoryHelper.ExistsAsync(
                connection, transaction, "Boards", boardId, ct))
            {
                _logger.LogWarning(
                    "Cannot assign components. Board {BoardId} was not found.",
                    boardId);

                transaction.Rollback();
                return false;
            }

            foreach (var componentId in ids)
            {
                if (!await SqliteRepositoryHelper.ExistsAsync(
                    connection, transaction, "Components", componentId, ct))
                {
                    _logger.LogWarning(
                        "Cannot assign components to board {BoardId}. Component {ComponentId} does not exist.",
                        boardId,
                        componentId);

                    throw new ArgumentException(
                        $"Component {componentId} does not exist.");
                }
            }

            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = """
                    DELETE FROM BoardComponents
                    WHERE BoardId = @BoardId;
                    """;

                delete.Parameters.AddWithValue("@BoardId", boardId);

                await delete.ExecuteNonQueryAsync(ct);
            }

            foreach (var componentId in ids)
            {
                await using var insert = connection.CreateCommand();

                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO BoardComponents(BoardId, ComponentId)
                    VALUES (@BoardId, @ComponentId);
                    """;

                insert.Parameters.AddWithValue("@BoardId", boardId);
                insert.Parameters.AddWithValue("@ComponentId", componentId);

                await insert.ExecuteNonQueryAsync(ct);
            }

            transaction.Commit();

            _logger.LogInformation(
                "Successfully assigned {ComponentCount} components to board {BoardId}.",
                ids.Count,
                boardId);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            transaction.Rollback();

            _logger.LogDebug(
                "Component assignment for board {BoardId} was cancelled.",
                boardId);

            throw;
        }
        catch (Exception ex)
        {
            transaction.Rollback();

            if (ex is not ArgumentException)
            {
                _logger.LogError(
                    ex,
                    "Failed to assign components to board {BoardId}.",
                    boardId);
            }

            throw;
        }
    }
}
