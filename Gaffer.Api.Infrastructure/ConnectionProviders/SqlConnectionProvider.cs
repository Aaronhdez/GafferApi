using MySqlConnector;

namespace Gaffer.Api.Infrastructure.Databases;

public class SqlConnectionProvider : IDatabaseConnectionProvider
{
    private readonly MySqlConnection _sqlConnection;

    public SqlConnectionProvider(string? dbConnection)
    {
        if (string.IsNullOrEmpty(dbConnection)) throw new ArgumentNullException(nameof(dbConnection));
        _sqlConnection = new MySqlConnection(dbConnection);
    }

    public async Task<bool> TestConnection()
    {
        _sqlConnection.Open();
        var query = "SELECT (0) FROM Parameters";
        var command = new MySqlCommand(query, _sqlConnection);
        await command.ExecuteReaderAsync();
        await _sqlConnection.CloseAsync();
        return true;
    }
}