using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using NSubstitute;
using NuGet.Protocol.Core.Types;

namespace Gaffer.Api.Tests.Unit.Controllers;

public class PlayerControllerShould
{
    private PlayerController _controller;
    private IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;

    [SetUp]
    public void Setup()
    {
        _getPlayerByIdQueryHandler = Substitute.For<IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse>>();
        _createPlayerCommandHandler = Substitute.For<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>>();
        _searchPlayersQueryHandler = Substitute.For<IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>>();
        _controller = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler,
            _searchPlayersQueryHandler);
    }

    [Test]
    public async Task CallAQueryToGetAPlayerById()
    {
        var newGuid = Guid.NewGuid();
        _searchPlayersQueryHandler.Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchPlayerResult>
            {
                new(Guid.NewGuid(), null, null, null, default, Gender.MALE)
            });;

        await _controller.GetBy(newGuid);

        await _searchPlayersQueryHandler.Received(1)
            .Handle(Arg.Is<SearchPlayersQuery>(spq =>
                    spq.Filters[0].Name == "Id"
                    && spq.Filters[0].Value == $"{newGuid}"),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CallAQueryToSearchAllPlayersWithoutFilters()
    {
        _searchPlayersQueryHandler.Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchPlayerResult>
            {
                new(Guid.NewGuid(), null, null, null, default, Gender.MALE)
            });

        await _controller.Search(new SearchPlayersRequest());

        await _searchPlayersQueryHandler.Received(1)
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CallAQueryToSearchAllPlayersWithIdFilter()
    {
        var newGuid = Guid.NewGuid();
        var guidFilter = new SearchPlayersFilter() { Name = "Id", Value = $"{newGuid}" };
        var guidQueryFilter = new QueryFilter() { Name = guidFilter.Name, Value = guidFilter.Value };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters.Add(guidFilter);
        _searchPlayersQueryHandler.Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchPlayerResult>
            {
                new(newGuid, null, null, null, default, Gender.MALE)
            });

        await _controller.Search(searchPlayersRequest);

        await _searchPlayersQueryHandler.Received(1)
            .Handle(Arg.Is<SearchPlayersQuery>(spq =>
                    spq.Filters[0].Name == guidFilter.Name
                    && spq.Filters[0].Value == guidFilter.Value),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CallACommandToCreateAPlayerById()
    {
        const string firstName = "John";
        const string lastName = "Doe";
        const string secondLastName = "Doe";
        DateTime birthDate = new(1990, 01, 01);
        const Gender gender = Gender.MALE;
        var newGuid = Guid.NewGuid();
        var request = new CreatePlayerRequest(firstName, lastName, secondLastName, birthDate, gender);
        _createPlayerCommandHandler.Handle(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreatePlayerResponse(newGuid));

        var createResult = await _controller.Create(request);

        createResult.Should().Be(newGuid);
        await _createPlayerCommandHandler.Received(1).Handle(Arg.Is<CreatePlayerCommand>(command =>
            command != null
            && command.FirstName == request.FirstName
            && command.LastName == request.LastName
            && command.SecondLastName == request.SecondLastName
            && command.Gender == request.Gender
            && command.Birthdate == request.BirthDate
        ), Arg.Any<CancellationToken>());
    }
}