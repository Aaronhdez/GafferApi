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
        var birthDate = new DateTime(2000, 1, 1);    
        var player = new Player(newGuid, string.Empty, string.Empty, null, birthDate, Gender.OTHER);
        _repository.Get(newGuid).Returns(player);
        
        var response = await _handler.Handle(new GetPlayerByIdQuery(newGuid), CancellationToken.None);
        
        await _repository.Received(1).Get(newGuid);
        response.Id.Should().Be(newGuid);
        response.FirstName.Should().Be(string.Empty);
        response.LastName.Should().Be(string.Empty);
        response.BirthDate.Should().Be(birthDate);
        response.Gender.Should().Be(Gender.OTHER);
    }
    
    [Test]
    public async Task CallTheRepositoryWithId2ToGetAPlayerById2()
    {
        var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var birthDate = new DateTime(2000, 1, 1);    
        var player = new Player(newGuid, "John", "String", "Doe", birthDate, Gender.MALE);
        _repository.Get(newGuid).Returns(player);
        
        var response = await _handler.Handle(new GetPlayerByIdQuery(newGuid), CancellationToken.None);
        
        await _repository.Received(1).Get(newGuid);
        response.Id.Should().Be(newGuid);
        response.FirstName.Should().Be("John");
        response.LastName.Should().Be("String");
        response.SecondLastName.Should().Be("Doe");
        response.BirthDate.Should().Be(birthDate);
        response.Gender.Should().Be(Gender.MALE);
    }
    
    [Test]
    public async Task CallTheRepositoryWithId3ToGetAPlayerById3()
    {
        var newGuid = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var birthDate = new DateTime(2000, 1, 1);    
        var player = new Player(newGuid, "Jane", "Smith", "Doe", birthDate, Gender.FEMALE);
        _repository.Get(newGuid).Returns(player);
        
        var response = await _handler.Handle(new GetPlayerByIdQuery(newGuid), CancellationToken.None);
        
        await _repository.Received(1).Get(newGuid);
        response.Id.Should().Be(newGuid);
        response.FirstName.Should().Be("Jane");
        response.LastName.Should().Be("Smith");
        response.SecondLastName.Should().Be("Doe");
        response.BirthDate.Should().Be(birthDate);
        response.Gender.Should().Be(Gender.FEMALE);
    }
}