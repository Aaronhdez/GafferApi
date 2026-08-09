using AwesomeAssertions;
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

public class DeletePlayerFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _playerRepository;
    private IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private ICommandHandler<DeletePlayerCommand, DeletePlayerResult> _deletePlayerCommandHandler;
    private PlayerController _playerController;

    private Guid _playerId = TestDefaultValues.EmptyPlayerId;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private readonly DateTime _birthDate = TestDefaultValues.DefaultBirthDate;
    private List<Player> _playersList;
    private const Gender Gender = TestDefaultValues.DefaultGender;

    [SetUp]
    public void SetUp()
    {
        var connectionString = TestDefaultValues.TestConnectionString;
        _connectionProvider = new SqlConnectionProvider(connectionString);
        _playerRepository = new SqlPlayerRepository(_connectionProvider);
        _getPlayerByIdQueryHandler = new SearchPlayerByIdQueryHandler(_playerRepository);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_playerRepository);
        _searchPlayersQueryHandler = new SearchPlayersQueryHandler(_playerRepository);
        _deletePlayerCommandHandler = new DeletePlayerCommandHandler(_playerRepository);
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler,
            _searchPlayersQueryHandler, _deletePlayerCommandHandler);
    }

    [Test]
    public async Task DeleteAPlayerOnCommand()
    {
        var player = new Player(_playerId, FirstName, LastName, SecondLastName, Gender, _birthDate);
        await InsertAPlayerInDb(player);
        var filter = new SearchPlayersFilter { Name = "Id", Value = $"{_playerId}" };
        var request = new SearchPlayersRequest();
        request.Filters.Add(filter);

        _playerController.Delete(new DeletePlayerRequest(_playerId));
        
        var result = await _playerController.Search(request);
        result.Should().BeEmpty();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connectionProvider.ClearAsync([TestDefaultValues.PlayersTableName]);
    }

    private async Task InsertAPlayerInDb(Player player)
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