using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance.Features;

public class PlayerFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _playerRepository;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private ICommandHandler<DeletePlayerCommand, DeletePlayerResult> _deletePlayerCommandHandler;
    protected PlayerController PlayerController;
    
    protected Guid PlayerId = TestDefaultValues.EmptyPlayerId;
    protected static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    protected static readonly string LastName = TestDefaultValues.DefaultLastName;
    protected static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    protected readonly DateTime BirthDate = TestDefaultValues.DefaultBirthDate;
    protected const Gender Gender = TestDefaultValues.DefaultGender;
    protected Player Player1;
    protected Player Player2;
    protected Player Player3;

    
    [SetUp]
    public void SetUp()
    {
        var connectionString = TestDefaultValues.TestConnectionString;
        _connectionProvider = new SqlConnectionProvider(connectionString);
        _playerRepository = new SqlPlayerRepository(_connectionProvider);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_playerRepository);
        _searchPlayersQueryHandler = new SearchPlayersQueryHandler(_playerRepository);
        _deletePlayerCommandHandler = new DeletePlayerCommandHandler(_playerRepository);
        PlayerController = new PlayerController(
            _createPlayerCommandHandler,
            _searchPlayersQueryHandler, 
            _deletePlayerCommandHandler);
        
        Player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, BirthDate);
        Player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", null, Gender.MALE, BirthDate);
        Player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", null, Gender.MALE, BirthDate);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _connectionProvider.ClearAsync([TestDefaultValues.PlayersTableName]);
    }

    protected async Task InsertAPlayerInDb(Player player)
    {
        var query = "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
                    "VALUES (@Id,  @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate);";
        await _connectionProvider.ExecuteAsync(query, new
        {
            player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            Gender = player.Gender.ToString(),
            BirthDate = player.Birthdate
        });
    }
}