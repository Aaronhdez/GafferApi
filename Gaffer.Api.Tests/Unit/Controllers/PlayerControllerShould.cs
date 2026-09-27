using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Tests.TestShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Controllers;

public class PlayerControllerShould
{
    private PlayerController _controller;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private ICommandHandler<DeletePlayerCommand> _deletePlayerCommandHandler;
    private ICommandHandler<UpdatePlayerCommand> _updatePlayerCommandHandler;

    [SetUp]
    public void Setup()
    {
        _searchPlayersQueryHandler = Substitute.For<IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>>();
        _createPlayerCommandHandler = Substitute.For<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>>();
        _deletePlayerCommandHandler = Substitute.For<ICommandHandler<DeletePlayerCommand>>();
        _updatePlayerCommandHandler = Substitute.For<ICommandHandler<UpdatePlayerCommand>>();
        _controller = new PlayerController(_createPlayerCommandHandler,
            _searchPlayersQueryHandler, _deletePlayerCommandHandler, _updatePlayerCommandHandler);
    }

    [Test]
    public async Task CallAQueryToGetAPlayerById()
    {
        var newGuid = Guid.NewGuid();
        _searchPlayersQueryHandler.Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchPlayerResult>
            {
                new(Guid.NewGuid(), null, null, null, default, Gender.MALE)
            });
        ;

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
        var newGuid = Guid.NewGuid();
        var request = new CreatePlayerRequest(
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultBirthDate,
            TestDefaultValues.DefaultGender);
        _createPlayerCommandHandler.Handle(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreatePlayerResponse(newGuid));

        var createResult = await _controller.Create(request);

        var created = createResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.ActionName.Should().Be(nameof(PlayerController.GetBy));
        created.RouteValues.Should().Contain("id", newGuid);
        created.Value.Should().Be(newGuid);
        await _createPlayerCommandHandler.Received(1).Handle(Arg.Is<CreatePlayerCommand>(command =>
            command != null
            && command.FirstName == request.FirstName
            && command.LastName == request.LastName
            && command.SecondLastName == request.SecondLastName
            && command.Gender == request.Gender
            && command.Birthdate == request.BirthDate
        ), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CallACommandToDeleteAPlayerById()
    {
        var id = Guid.NewGuid();

        await _controller.Delete(id);

        await _deletePlayerCommandHandler.Received(1).Handle(Arg.Is<DeletePlayerCommand>(command =>
            command != null && command.Id == id
        ), Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task PassTheRequestTokenToTheQueryWhenGettingAPlayerById()
    {
        using var cancellation = new CancellationTokenSource();
        _searchPlayersQueryHandler.Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns([new SearchPlayerResult(Guid.NewGuid(), TestDefaultValues.DefaultFirstName,
                TestDefaultValues.DefaultLastName, null, default, Gender.MALE)]);

        await _controller.GetBy(Guid.NewGuid(), cancellation.Token);

        await _searchPlayersQueryHandler.Received(1).Handle(Arg.Any<SearchPlayersQuery>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheQueryWhenSearchingPlayers()
    {
        using var cancellation = new CancellationTokenSource();
        _searchPlayersQueryHandler.Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchPlayerResult>());

        await _controller.Search(new SearchPlayersRequest(), cancellation.Token);

        await _searchPlayersQueryHandler.Received(1).Handle(Arg.Any<SearchPlayersQuery>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenCreatingAPlayer()
    {
        using var cancellation = new CancellationTokenSource();
        _createPlayerCommandHandler.Handle(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreatePlayerResponse(Guid.NewGuid()));

        await _controller.Create(new CreatePlayerRequest(
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultBirthDate,
            TestDefaultValues.DefaultGender), cancellation.Token);

        await _createPlayerCommandHandler.Received(1).Handle(Arg.Any<CreatePlayerCommand>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenDeletingAPlayer()
    {
        using var cancellation = new CancellationTokenSource();

        await _controller.Delete(Guid.NewGuid(), cancellation.Token);

        await _deletePlayerCommandHandler.Received(1).Handle(Arg.Any<DeletePlayerCommand>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenUpdatingAPlayer()
    {
        using var cancellation = new CancellationTokenSource();

        await _controller.Update(Guid.NewGuid(), new UpdatePlayerRequest(), cancellation.Token);

        await _updatePlayerCommandHandler.Received(1).Handle(Arg.Any<UpdatePlayerCommand>(), cancellation.Token);
    }

    [Test]
    public async Task CallACommandToUpdateAPlayerById()
    {
        var id = Guid.NewGuid();

        await _controller.Update(id, new UpdatePlayerRequest());
        await _updatePlayerCommandHandler.Received(1).Handle(Arg.Is<UpdatePlayerCommand>(command =>
            command != null && command.Id == id
        ), Arg.Any<CancellationToken>());
    }
}