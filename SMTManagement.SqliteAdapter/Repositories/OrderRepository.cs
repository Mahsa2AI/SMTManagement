using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Contracts.Models;
using SMTManagement.Contracts.Models.DTOs;

namespace SMTManagement.SqliteAdapter.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(
        ISqliteConnectionFactory factory,
        ILogger<OrderRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Order>> GetAllAsync(
        string? search = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving orders. Search: {Search}",
            search);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT Id, Name, Description, OrderDate, CreatedAt
                FROM Orders
                WHERE @Search IS NULL
                   OR Name LIKE @Pattern
                   OR Description LIKE @Pattern
                ORDER BY Id DESC;
                """;

            command.Parameters.AddWithValue(
                "@Search", (object?)search ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@Pattern", $"%{search ?? string.Empty}%");

            var result = new List<Order>();

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
                result.Add(MapOrder(reader));

            foreach (var order in result)
            {
                await using var boardCommand = connection.CreateCommand();
                boardCommand.CommandText = """
                    SELECT b.Id, b.Name, b.Description, b.Length, b.Width, b.CreatedAt
                    FROM Boards b
                    INNER JOIN OrderBoards ob ON ob.BoardId = b.Id
                    WHERE ob.OrderId = @OrderId
                    ORDER BY b.Id;
                    """;
                boardCommand.Parameters.AddWithValue("@OrderId", order.Id);

                await using (var boardReader = await boardCommand.ExecuteReaderAsync(ct))
                {
                    while (await boardReader.ReadAsync(ct))
                        order.Boards.Add(MapBoard(boardReader));
                }

                foreach (var board in order.Boards)
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
                        board.Components.Add(MapComponent(componentReader));
                }
            }

            _logger.LogDebug(
                "Retrieved {OrderCount} orders.",
                result.Count);

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Retrieving orders was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve orders. Search: {Search}",
                search);

            throw;
        }
    }

    public async Task<Order?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving order with ID {OrderId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            var order = await GetOrderAsync(connection, id, ct);

            if (order is null)
            {
                _logger.LogWarning(
                    "Order with ID {OrderId} was not found.",
                    id);

                return null;
            }

            await using var boardCommand = connection.CreateCommand();

            boardCommand.CommandText = """
                SELECT b.Id, b.Name, b.Description, b.Length, b.Width, b.CreatedAt
                FROM Boards b
                INNER JOIN OrderBoards ob ON ob.BoardId = b.Id
                WHERE ob.OrderId = @OrderId
                ORDER BY b.Id;
                """;

            boardCommand.Parameters.AddWithValue("@OrderId", id);

            var boards = new List<Board>();

            await using (var reader = await boardCommand.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                    boards.Add(MapBoard(reader));
            }

            foreach (var board in boards)
            {
                await using var componentCommand = connection.CreateCommand();

                componentCommand.CommandText = """
                    SELECT c.Id, c.Name, c.Description, c.Quantity, c.CreatedAt
                    FROM Components c
                    INNER JOIN BoardComponents bc ON bc.ComponentId = c.Id
                    WHERE bc.BoardId = @BoardId
                    ORDER BY c.Id;
                    """;

                componentCommand.Parameters.AddWithValue(
                    "@BoardId", board.Id);

                await using var reader =
                    await componentCommand.ExecuteReaderAsync(ct);

                while (await reader.ReadAsync(ct))
                    board.Components.Add(MapComponent(reader));
            }

            order.Boards = boards;

            _logger.LogDebug(
                "Retrieved order {OrderId} with {BoardCount} boards and {ComponentCount} components.",
                id,
                boards.Count,
                boards.Sum(b => b.Components.Count));

            return order;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Retrieving order {OrderId} was cancelled.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve order with ID {OrderId}.",
                id);

            throw;
        }
    }

    public async Task<int> CreateAsync(
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating order with name {OrderName}.",
            request.Name);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Orders
                    (Name, Description, OrderDate, CreatedAt)
                VALUES
                    (@Name, @Description, @OrderDate, @CreatedAt);

                SELECT last_insert_rowid();
                """;

            command.Parameters.AddWithValue("@Name", request.Name.Trim());

            command.Parameters.AddWithValue(
                "@Description",
                (object?)request.Description ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@OrderDate",
                request.OrderDate.ToUniversalTime().ToString("O"));

            command.Parameters.AddWithValue(
                "@CreatedAt",
                DateTime.UtcNow.ToString("O"));

            var id = Convert.ToInt32(
                await command.ExecuteScalarAsync(ct));

            _logger.LogInformation(
                "Order created successfully. OrderId: {OrderId}, OrderName: {OrderName}",
                id,
                request.Name);

            return id;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Creating order {OrderName} was cancelled.",
                request.Name);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create order with name {OrderName}.",
                request.Name);

            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateOrderRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Updating order with ID {OrderId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                UPDATE Orders
                SET Name = @Name,
                    Description = @Description,
                    OrderDate = @OrderDate
                WHERE Id = @Id;
                """;

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name.Trim());

            command.Parameters.AddWithValue(
                "@Description",
                (object?)request.Description ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@OrderDate",
                request.OrderDate.ToUniversalTime().ToString("O"));

            var updated = await command.ExecuteNonQueryAsync(ct) > 0;

            if (!updated)
            {
                _logger.LogWarning(
                    "Order with ID {OrderId} was not found. Update skipped.",
                    id);

                return false;
            }

            _logger.LogInformation(
                "Order with ID {OrderId} updated successfully.",
                id);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Updating order {OrderId} was cancelled.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update order with ID {OrderId}.",
                id);

            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Deleting order with ID {OrderId}.",
            id);

        try
        {
            await using var connection =
                await _factory.CreateConnectionAsync(ct);

            await using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Orders WHERE Id = @Id;";

            command.Parameters.AddWithValue("@Id", id);

            var deleted = await command.ExecuteNonQueryAsync(ct) > 0;

            if (!deleted)
            {
                _logger.LogWarning(
                    "Order with ID {OrderId} was not found. Delete skipped.",
                    id);

                return false;
            }

            _logger.LogInformation(
                "Order with ID {OrderId} deleted successfully.",
                id);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Deleting order {OrderId} was cancelled.",
                id);

            throw;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            _logger.LogWarning(
                ex,
                "Cannot delete order {OrderId} because it is referenced by another record.",
                id);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete order with ID {OrderId}.",
                id);

            throw;
        }
    }

    public async Task<bool> SetBoardsAsync(
        int orderId,
        List<int> boardIds,
        CancellationToken ct = default)
    {
        var ids = boardIds.Distinct().ToList();

        _logger.LogInformation(
            "Assigning {BoardCount} boards to order {OrderId}.",
            ids.Count,
            orderId);

        await using var connection =
            await _factory.CreateConnectionAsync(ct);

        using var transaction = connection.BeginTransaction();

        try
        {
            if (!await ExistsAsync(
                connection, transaction, "Orders", orderId, ct))
            {
                _logger.LogWarning(
                    "Cannot assign boards. Order {OrderId} was not found.",
                    orderId);

                transaction.Rollback();
                return false;
            }

            foreach (var boardId in ids)
            {
                if (!await ExistsAsync(
                    connection, transaction, "Boards", boardId, ct))
                {
                    _logger.LogWarning(
                        "Cannot assign boards to order {OrderId}. Board {BoardId} does not exist.",
                        orderId,
                        boardId);

                    throw new ArgumentException(
                        $"Board {boardId} does not exist.");
                }
            }

            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText =
                    "DELETE FROM OrderBoards WHERE OrderId = @Id;";

                delete.Parameters.AddWithValue("@Id", orderId);

                await delete.ExecuteNonQueryAsync(ct);
            }

            foreach (var boardId in ids)
            {
                await using var insert = connection.CreateCommand();

                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO OrderBoards(OrderId, BoardId)
                    VALUES (@OrderId, @BoardId);
                    """;

                insert.Parameters.AddWithValue("@OrderId", orderId);
                insert.Parameters.AddWithValue("@BoardId", boardId);

                await insert.ExecuteNonQueryAsync(ct);
            }

            transaction.Commit();

            _logger.LogInformation(
                "Successfully assigned {BoardCount} boards to order {OrderId}.",
                ids.Count,
                orderId);

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            transaction.Rollback();

            _logger.LogDebug(
                "Board assignment for order {OrderId} was cancelled.",
                orderId);

            throw;
        }
        catch (Exception ex)
        {
            transaction.Rollback();

            if (ex is not ArgumentException)
            {
                _logger.LogError(
                    ex,
                    "Failed to assign boards to order {OrderId}.",
                    orderId);
            }

            throw;
        }
    }

    private static async Task<Order?> GetOrderAsync(
        SqliteConnection connection,
        int id,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Name, Description, OrderDate, CreatedAt
            FROM Orders WHERE Id = @Id;
            """;

        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct)
            ? MapOrder(reader)
            : null;
    }

    internal static Order MapOrder(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        OrderDate = DateTime.Parse(reader.GetString(3)),
        CreatedAt = DateTime.Parse(reader.GetString(4))
    };

    internal static Board MapBoard(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Length = reader.GetDouble(3),
        Width = reader.GetDouble(4),
        CreatedAt = DateTime.Parse(reader.GetString(5))
    };

    internal static Component MapComponent(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Quantity = reader.GetInt32(3),
        CreatedAt = DateTime.Parse(reader.GetString(4))
    };

    internal static async Task<bool> ExistsAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string table,
        int id,
        CancellationToken ct)
    {
        var allowedTables = new[] { "Orders", "Boards", "Components" };

        if (!allowedTables.Contains(table))
            throw new ArgumentException("Invalid table.");

        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText =
            $"SELECT EXISTS(SELECT 1 FROM {table} WHERE Id = @Id);";

        command.Parameters.AddWithValue("@Id", id);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(ct)) == 1;
    }
}
