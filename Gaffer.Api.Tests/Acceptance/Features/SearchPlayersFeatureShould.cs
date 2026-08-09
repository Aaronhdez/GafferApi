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

public class SearchPlayersFeatureShould : PlayerFeatureShould
{
    [Test]
    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);

        var response = await PlayerController.Search(new SearchPlayersRequest());

        response.Count.Should().Be(3);
        response.Should().Contain(x => x.Id == Player1.Id);
        response.Should().Contain(x => x.Id == Player2.Id);
        response.Should().Contain(x => x.Id == Player3.Id);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithIdFilter() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "Id",
            Value = $"{Player2.Id}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await PlayerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == Player2.Id);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithFirstNameFilter() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "FirstName",
            Value = $"{Player2.FirstName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await PlayerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == Player2.Id);
        response.Should().Contain(x => x.FirstName == Player2.FirstName);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithSecondNameFilter() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "LastName",
            Value = $"{Player2.LastName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await PlayerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == Player2.Id);
        response.Should().Contain(x => x.FirstName == Player2.FirstName);
    }
}