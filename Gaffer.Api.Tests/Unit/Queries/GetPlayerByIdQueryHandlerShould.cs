using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NuGet.Protocol.Core.Types;

namespace Gaffer.Api.Tests.Unit.Queries;

public class GetPlayerByIdQueryHandlerShould
{
    private GetPlayerByIdQueryHandler _handler;
    private IPlayerRepository _repository;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new GetPlayerByIdQueryHandler(_repository);
    }

    [Test]
    public async Task CallTheRepositoryWithId1ToGetAPlayerById1()
    {
        var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var player = new Player(newGuid, string.Empty, string.Empty, null, DateTime.UtcNow, Gender.OTHER);
        _repository.Get(newGuid).Returns(player);
        
        var response = await _handler.Handle(new GetPlayerByIdQuery(newGuid), CancellationToken.None);
        
        await _repository.Received(1).Get(newGuid);
        response.Id.Should().Be(newGuid);
        response.FirstName.Should().Be(string.Empty);
        response.LastName.Should().Be(string.Empty);
        response.Gender.Should().Be(Gender.OTHER);
    }
}