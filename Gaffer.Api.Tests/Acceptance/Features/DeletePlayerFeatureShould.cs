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

public class DeletePlayerFeatureShould : PlayerFeatureShould
{
    [Test]
    public async Task DeleteAPlayerOnCommand()
    {
        var player = new Player(PlayerId, FirstName, LastName, SecondLastName, Gender, BirthDate);
        await InsertAPlayerInDb(player);
        var filter = new SearchPlayersFilter { Name = "Id", Value = $"{PlayerId}" };
        var request = new SearchPlayersRequest();
        request.Filters.Add(filter);

        await PlayerController.Delete(new DeletePlayerRequest(PlayerId));
        
        var result = await PlayerController.Search(request);
        result.Should().BeEmpty();
    }
    
    [Test]
    public void ThrowExceptionWhenPlayerNotFound()
    {       
        var result = async () => await PlayerController.Delete(new DeletePlayerRequest(Guid.NewGuid()));
        
        result.Should().ThrowAsync<PlayerNotFoundException>();
    }
}