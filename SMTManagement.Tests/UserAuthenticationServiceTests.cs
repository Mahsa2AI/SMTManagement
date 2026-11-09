using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.WebAPI.Services;
using Xunit;

namespace SMTManagement.Tests;

public sealed class UserAuthenticationServiceTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"smt-auth-tests-{Guid.NewGuid():N}.db");

    private UserAuthenticationService _service = null!;

    public async Task InitializeAsync()
    {
        var connectionString = $"Data Source={_databasePath};Foreign Keys=True;Pooling=False";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Users
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:InitialUser:Username"] = "admin",
                ["Authentication:InitialUser:Password"] = "InitialPassword!123"
            })
            .Build();

        _service = new UserAuthenticationService(
            new TestSqliteConnectionFactory(connectionString),
            configuration,
            NullLogger<UserAuthenticationService>.Instance);
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);

        var walPath = $"{_databasePath}-wal";
        var shmPath = $"{_databasePath}-shm";
        if (File.Exists(walPath))
            File.Delete(walPath);
        if (File.Exists(shmPath))
            File.Delete(shmPath);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task EnsureInitialUserAsync_CreatesUserThatCanLogin()
    {
        await _service.EnsureInitialUserAsync();

        var account = await _service.ValidateAsync("admin", "InitialPassword!123");

        Assert.NotNull(account);
        Assert.Equal("admin", account.Username);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNullForInvalidCredentials()
    {
        await _service.EnsureInitialUserAsync();

        Assert.Null(await _service.ValidateAsync("admin", "wrong-password"));
        Assert.Null(await _service.ValidateAsync("missing", "InitialPassword!123"));
    }

    [Fact]
    public async Task ChangePasswordAsync_RequiresCurrentPasswordAndUpdatesHash()
    {
        await _service.EnsureInitialUserAsync();

        Assert.False(await _service.ChangePasswordAsync(
            "admin", "wrong-password", "NewPassword!123"));

        Assert.True(await _service.ChangePasswordAsync(
            "admin", "InitialPassword!123", "NewPassword!123"));

        Assert.Null(await _service.ValidateAsync("admin", "InitialPassword!123"));
        Assert.NotNull(await _service.ValidateAsync("admin", "NewPassword!123"));
    }

    [Fact]
    public async Task EnsureInitialUserAsync_DoesNotOverwriteExistingPassword()
    {
        await _service.EnsureInitialUserAsync();
        Assert.True(await _service.ChangePasswordAsync(
            "admin", "InitialPassword!123", "UserSelectedPassword!123"));

        await _service.EnsureInitialUserAsync();

        Assert.Null(await _service.ValidateAsync("admin", "InitialPassword!123"));
        Assert.NotNull(await _service.ValidateAsync("admin", "UserSelectedPassword!123"));
    }

    private sealed class TestSqliteConnectionFactory : ISqliteConnectionFactory
    {
        private readonly string _connectionString;

        public TestSqliteConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<SqliteConnection> CreateConnectionAsync(CancellationToken ct = default)
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }
    }
}
