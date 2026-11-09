namespace SMTManagement.Contracts.Interfaces.SqliteAdapter;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
}
