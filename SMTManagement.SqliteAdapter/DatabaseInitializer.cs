using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;

namespace SMTManagement.SqliteAdapter;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configured = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string not configured.");

        var builder = new SqliteConnectionStringBuilder(configured);
        if (!Path.IsPathRooted(builder.DataSource))
            builder.DataSource = Path.Combine(environment.ContentRootPath, builder.DataSource);

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        builder.ForeignKeys = true;
        _connectionString = builder.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        var sql = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Orders
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                OrderDate TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Users
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Boards
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Length REAL NOT NULL CHECK(Length > 0),
                Width REAL NOT NULL CHECK(Width > 0),
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Components
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Quantity INTEGER NOT NULL CHECK(Quantity >= 0),
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS OrderBoards
            (
                OrderId INTEGER NOT NULL,
                BoardId INTEGER NOT NULL,

                PRIMARY KEY(OrderId, BoardId),

                FOREIGN KEY(OrderId)
                    REFERENCES Orders(Id) ON DELETE CASCADE,

                FOREIGN KEY(BoardId)
                    REFERENCES Boards(Id) ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS BoardComponents
            (
                BoardId INTEGER NOT NULL,
                ComponentId INTEGER NOT NULL,

                PRIMARY KEY(BoardId, ComponentId),

                FOREIGN KEY(BoardId)
                    REFERENCES Boards(Id) ON DELETE CASCADE,

                FOREIGN KEY(ComponentId)
                    REFERENCES Components(Id) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_Orders_Name
                ON Orders(Name);

            CREATE INDEX IF NOT EXISTS IX_Boards_Name
                ON Boards(Name);

            CREATE INDEX IF NOT EXISTS IX_Components_Name
                ON Components(Name);

            CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username
                ON Users(Username COLLATE NOCASE);

            CREATE INDEX IF NOT EXISTS IX_OrderBoards_BoardId
                ON OrderBoards(BoardId);

            CREATE INDEX IF NOT EXISTS IX_BoardComponents_ComponentId
                ON BoardComponents(ComponentId);
            """;

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        await command.ExecuteNonQueryAsync();
    }
}
