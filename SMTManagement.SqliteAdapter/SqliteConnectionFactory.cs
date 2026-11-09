using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;

namespace SMTManagement.SqliteAdapter;

public class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Database connection string is missing.");

        var builder = new SqliteConnectionStringBuilder(configured);

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(environment.ContentRootPath, builder.DataSource);
        }

        var directory = Path.GetDirectoryName(builder.DataSource);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        builder.ForeignKeys = true;

        _connectionString = builder.ToString();
    }

    public async Task<SqliteConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync(ct);

        return connection;
    }
}

