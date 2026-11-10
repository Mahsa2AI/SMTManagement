using Microsoft.Data.Sqlite;
using SMTManagement.Contracts.Models;

namespace SMTManagement.SqliteAdapter;

internal static class SqliteEntityMapper
{
    public static Order MapOrder(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        OrderDate = DateTime.Parse(reader.GetString(3)),
        CreatedAt = DateTime.Parse(reader.GetString(4))
    };

    public static Board MapBoard(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Length = reader.GetDouble(3),
        Width = reader.GetDouble(4),
        CreatedAt = DateTime.Parse(reader.GetString(5))
    };

    public static Component MapComponent(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Quantity = reader.GetInt32(3),
        CreatedAt = DateTime.Parse(reader.GetString(4))
    };
}
