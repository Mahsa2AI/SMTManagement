using Microsoft.Data.Sqlite;

namespace SMTManagement.SqliteAdapter;

internal static class SqliteRepositoryHelper
{
    private static readonly HashSet<string> AllowedTables =
    [
        "Orders",
        "Boards",
        "Components"
    ];

    public static async Task<bool> ExistsAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string table,
        int id,
        CancellationToken ct)
    {
        if (!AllowedTables.Contains(table))
            throw new ArgumentException("Invalid table.", nameof(table));

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT EXISTS(SELECT 1 FROM {table} WHERE Id = @Id);";
        command.Parameters.AddWithValue("@Id", id);

        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == 1;
    }
}
