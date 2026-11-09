using Microsoft.Data.Sqlite;

namespace SMTManagement.Contracts.Interfaces.SqliteAdapter;

public interface ISqliteConnectionFactory
{
    Task<SqliteConnection> CreateConnectionAsync(CancellationToken ct = default);
}
