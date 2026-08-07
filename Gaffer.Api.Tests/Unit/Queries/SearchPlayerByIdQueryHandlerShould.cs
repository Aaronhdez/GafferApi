using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NuGet.Protocol.Core.Types;

namespace Gaffer.Api.Tests.Unit.Queries;

public class SearchPlayerByIdQueryHandlerShould
{
    private SearchPlayerByIdQueryHandler _handler;
    private IPlayerRepository _repository;
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string? SecondLastName = "Doe";
    private const Gender Gender = Shared.Gender.MALE;
    private readonly Guid _newGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly DateTime _birthDate = new DateTime(2000, 1, 1);
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _cancellationToken = CancellationToken.None;
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new SearchPlayerByIdQueryHandler(_repository);
    }

    [Test]
    public async Task CallTheRepositoryWithIdToGetAPlayerByItsId()
    {
        var player = new Player(_newGuid, FirstName, LastName, SecondLastName, _birthDate, Gender);
        var searchCriteria = new PlayerSearchCriteria();
        searchCriteria.criteriaList.Add(new SearchCriteria
        {
            Name = "Id",
            Value = $"{_newGuid}"
        });
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), _cancellationToken).Returns([player]);

        var response = await _handler.Handle(new SearchPlayerByIdQuery(_newGuid), CancellationToken.None);

        await _repository.Received(1)
            .Get(Arg.Is<PlayerSearchCriteria>(psc =>
                    psc.criteriaList.Exists(sc => sc.Name == "Id" && sc.Value == $"{_newGuid}")),
                _cancellationToken);
        response.Id.Should().Be(_newGuid);
        response.FirstName.Should().Be(FirstName);
        response.LastName.Should().Be(LastName);
        response.SecondLastName.Should().Be(SecondLastName);
        response.Birthdate.Should().Be(_birthDate);
        response.Gender.Should().Be(Gender);
    }


    [Test]
    public async Task ThrowExceptionWhenPlayerIsNotFound()
    {
        var otherPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), _cancellationToken).Returns([]);

        var response = async () => await _handler.Handle(new SearchPlayerByIdQuery(_newGuid), CancellationToken.None);

        await response.Should().ThrowAsync<PlayerNotFoundException>();
    }
}