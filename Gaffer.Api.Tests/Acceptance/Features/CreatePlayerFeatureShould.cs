using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance.Features;

public class CreatePlayerFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _playerRepository;
    private IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private PlayerController _playerController;

    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private readonly DateTime _birthDate = TestDefaultValues.DefaultBirthDate;
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
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler, _searchPlayersQueryHandler);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _connectionProvider.ClearAsync([TestDefaultValues.PlayersTableName]);
    }

    [Test]
    public async Task CreateAPlayerOnDemand()
    {
        var createPlayerRequest = new CreatePlayerRequest(FirstName, LastName, SecondLastName, _birthDate, Gender);

        var playerId = await _playerController.Create(createPlayerRequest);
        
        var response = await _playerController.GetBy(playerId);
        response.Id.Should().Be(playerId);
        response.FirstName.Should().Be(FirstName);
        response.LastName.Should().Be(LastName);
        response.BirthDate.Should().Be(_birthDate);
        response.Gender.Should().Be(Gender);
    }
}