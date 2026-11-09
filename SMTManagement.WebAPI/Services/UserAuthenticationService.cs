using Microsoft.AspNetCore.Identity;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;

namespace SMTManagement.WebAPI.Services;

public sealed record UserAccount(int Id, string Username, string PasswordHash);

public sealed class UserAuthenticationService
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserAuthenticationService> _logger;
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();

    public UserAuthenticationService(
        ISqliteConnectionFactory factory,
        IConfiguration configuration,
        ILogger<UserAuthenticationService> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureInitialUserAsync(CancellationToken ct = default)
    {
        var username = _configuration["Authentication:InitialUser:Username"];
        var password = _configuration["Authentication:InitialUser:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException(
                "Authentication:InitialUser:Username and Password must be configured.");

        await using var connection = await _factory.CreateConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, PasswordHash, CreatedAt)
            SELECT @Username, @PasswordHash, @CreatedAt
            WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = @Username);
            """;

        var account = new UserAccount(0, username.Trim(), string.Empty);
        var hash = _passwordHasher.HashPassword(account, password);
        command.Parameters.AddWithValue("@Username", account.Username);
        command.Parameters.AddWithValue("@PasswordHash", hash);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));
        var inserted = await command.ExecuteNonQueryAsync(ct);
        _logger.LogInformation(
            inserted == 1
                ? "Initial application user provisioned. Username: {Username}"
                : "Initial application user already exists. Username: {Username}",
            account.Username);
    }

    public async Task<UserAccount?> ValidateAsync(
        string? username,
        string? password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Login failed because username or password was not provided.");
            return null;
        }

        await using var connection = await _factory.CreateConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Username, PasswordHash
            FROM Users
            WHERE Username = @Username;
            """;
        command.Parameters.AddWithValue("@Username", username.Trim());

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            _logger.LogWarning("Login failed for unknown username: {Username}", username.Trim());
            return null;
        }

        var account = new UserAccount(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2));

        var result = _passwordHasher.VerifyHashedPassword(
            account, account.PasswordHash, password);

        if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
        {
            _logger.LogInformation("User login succeeded. Username: {Username}", account.Username);
            return account;
        }

        _logger.LogWarning("Login failed because the password was invalid. Username: {Username}", account.Username);
        return null;
    }

    public async Task<bool> ChangePasswordAsync(
        string? username,
        string? currentPassword,
        string? newPassword,
        CancellationToken ct = default)
    {
        var account = await ValidateAsync(username, currentPassword, ct);
        if (account is null || string.IsNullOrEmpty(newPassword))
        {
            _logger.LogWarning("Password change failed. Username: {Username}", username ?? "<unknown>");
            return false;
        }

        var newHash = _passwordHasher.HashPassword(account, newPassword);

        await using var connection = await _factory.CreateConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET PasswordHash = @PasswordHash
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@PasswordHash", newHash);
        command.Parameters.AddWithValue("@Id", account.Id);

        var changed = await command.ExecuteNonQueryAsync(ct) == 1;
        if (changed)
            _logger.LogInformation("Password changed successfully. Username: {Username}", account.Username);
        else
            _logger.LogWarning("Password change did not update a user record. Username: {Username}", account.Username);

        return changed;
    }
}