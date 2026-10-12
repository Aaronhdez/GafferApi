using Gaffer.Api.Shared.ConnectionProviders;


namespace Gaffer.Api.Tests;

public abstract class BaseSqlTest {
    private readonly IDatabaseConnectionProvider connectionProvider;
    private string? dbConnection;
    protected List<string> TablesToDelete = [];

    protected BaseSqlTest() {
        dbConnection = TestConfiguration.DatabaseConnectionString;
        connectionProvider = new SqlConnectionProvider(dbConnection);
    }

    public async Task ClearDatabase(List<string> tablesToDelete) => await connectionProvider.ClearAsync(tablesToDelete);
}
