using Gaffer.Api.Shared.ConnectionProviders;


namespace Gaffer.Api.Tests;

public abstract class BaseSqlTest
{
   private readonly IDatabaseConnectionProvider _connectionProvider;
   private string? _dbConnection;
   protected List<string> _tablesToDelete;

   protected BaseSqlTest()
   {
      _dbConnection = "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;Pwd=toor;";
      _connectionProvider = new SqlConnectionProvider(_dbConnection);
   }
   
   public async Task ClearDatabase(List<string> tablesToDelete)
   {
      await _connectionProvider.ClearAsync(tablesToDelete);
   }
}
