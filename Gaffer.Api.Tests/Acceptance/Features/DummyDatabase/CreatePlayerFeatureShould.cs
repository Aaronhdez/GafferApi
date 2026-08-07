using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Tests.Acceptance.Features.DummyDatabase;

public class CreatePlayerFeatureShould
{
    private IDatabaseConnectionProvider _dummyConnectionProvider;
    private IPlayerRepository _dummyPlayerRepository;
    private IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private PlayerController _playerController;

    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string SecondLastName = "Doe";
    private readonly DateTime _birthDate = new(1990, 01, 01);
    private List<Player> _playersList;
    private const Gender Gender = Shared.Gender.MALE;

    [SetUp]
    public void SetUp()
    {
        _dummyConnectionProvider = new DummyConnectionProvider();
        _playersList = new List<Player>();
        _dummyPlayerRepository = new DummyPlayerRepository(_dummyConnectionProvider, _playersList);
        _getPlayerByIdQueryHandler = new GetPlayerByIdQueryHandler(_dummyPlayerRepository);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_dummyPlayerRepository);
        _searchPlayersQueryHandler = new SearchPlayersQueryHandler(_dummyPlayerRepository);
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler, _searchPlayersQueryHandler);
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