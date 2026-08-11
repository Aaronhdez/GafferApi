using System.Net;
using System.Net.Http.Headers;
using System.Text;
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
using NSubstitute;

namespace Gaffer.Api.Tests.EndToEnd;

public class SearchPlayerFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _playerRepository;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private ICommandHandler<DeletePlayerCommand> _deletePlayerCommandHandler;
    private ICommandHandler<UpdatePlayerCommand> _updatePlayerCommandHandler;
    private HttpClient _client;
    private PlayerController PlayerController;
    private Guid PlayerId = TestDefaultValues.EmptyPlayerId;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private readonly DateTime BirthDate = TestDefaultValues.DefaultBirthDate;
    private const Gender Gender = TestDefaultValues.DefaultGender;
    private Player Player1;
    private Player Player2;
    private Player Player3;

    [SetUp]
    public void SetUp()
    {
        _client = TestFixture.Client;
        Player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, BirthDate);
        Player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", null, Gender.MALE, BirthDate);
        Player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", null, Gender.MALE, BirthDate);
    }
    
    [Test]
    public async Task GetPlayers()
    {
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var formattedRequest = Newtonsoft.Json.JsonConvert.SerializeObject(createPlayerRequest);
        var createUri = new Uri($"http://localhost:50000/api/v1.0/players/");
        var result = await _client.PostAsync(createUri, new StringContent(formattedRequest, Encoding.UTF8, "application/json"));
        var formattedResult = await result.Content.ReadFromJsonAsync<Guid>();
        

        var queryUri = new Uri($"http://localhost:50000/api/v1.0/players/{formattedResult.ToString()}");
        var response = await _client.GetAsync(queryUri);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
//    [Test]
//    public async Task GetAPlayerFromRepositoryByUsingItsId()
//    {
//        var player = new Player(PlayerId, FirstName, LastName, SecondLastName, Gender, BirthDate);
//        await InsertAPlayerInDb(player);
//
//        var response = await PlayerController.GetBy(PlayerId);
//
//        response.Id.Should().Be(PlayerId);
//        response.FirstName.Should().Be(FirstName);
//        response.LastName.Should().Be(LastName);
//        response.BirthDate.Should().Be(BirthDate);
//        response.Gender.Should().Be(Gender);
//    }
//    
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//
//        var response = await PlayerController.Search(new SearchPlayersRequest());
//
//        response.Count.Should().Be(3);
//        response.Should().Contain(x => x.Id == Player1.Id);
//        response.Should().Contain(x => x.Id == Player2.Id);
//        response.Should().Contain(x => x.Id == Player3.Id);
//    }
//
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithIdFilter() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//        var namePlayerFilter = new SearchPlayersFilter {
//            Name = "Id",
//            Value = $"{Player2.Id}"
//        };
//        var searchPlayersRequest = new SearchPlayersRequest();
//        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
//        searchPlayersRequest.Filters.Add(namePlayerFilter);
//        
//        var response = await PlayerController.Search(searchPlayersRequest);
//
//        response.Count.Should().Be(1);
//        response.Should().Contain(x => x.Id == Player2.Id);
//    }
//    
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithFirstNameFilter() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//        var namePlayerFilter = new SearchPlayersFilter {
//            Name = "FirstName",
//            Value = $"{Player2.FirstName}"
//        };
//        var searchPlayersRequest = new SearchPlayersRequest();
//        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
//        searchPlayersRequest.Filters.Add(namePlayerFilter);
//        
//        var response = await PlayerController.Search(searchPlayersRequest);
//
//        response.Count.Should().Be(1);
//        response.Should().Contain(x => x.Id == Player2.Id);
//        response.Should().Contain(x => x.FirstName == Player2.FirstName);
//    }
//    
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithSecondNameFilter() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//        var namePlayerFilter = new SearchPlayersFilter {
//            Name = "LastName",
//            Value = $"{Player2.LastName}"
//        };
//        var searchPlayersRequest = new SearchPlayersRequest();
//        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
//        searchPlayersRequest.Filters.Add(namePlayerFilter);
//        
//        var response = await PlayerController.Search(searchPlayersRequest);
//
//        response.Count.Should().Be(1);
//        response.Should().Contain(x => x.Id == Player2.Id);
//        response.Should().Contain(x => x.FirstName == Player2.FirstName);
//    }
}