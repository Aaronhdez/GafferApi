using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands;

public class DeletePlayerCommandHandlerShould {
    private IPlayerRepository repository;
    private DeletePlayerCommandHandler handler;

    [SetUp]
    public void Setup() {
        repository = Substitute.For<IPlayerRepository>();
        handler = new DeletePlayerCommandHandler(repository);
    }

    [Test]
    public async Task DeleteAPlayerWhileBeingExecuted() {
        var createPlayerCommand = new DeletePlayerCommand(TestDefaultValues.EmptyPlayerId);
        var player = new Player(
            createPlayerCommand.Id,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultGender,
            TestDefaultValues.DefaultBirthDate);
        repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);

        await handler.Handle(createPlayerCommand, CancellationToken.None);

        await repository.Received(1).Delete(Arg.Is<Guid>(g => g == TestDefaultValues.EmptyPlayerId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ThrowExceptionIfPlayerNotFound() {
        var createPlayerCommand = new DeletePlayerCommand(TestDefaultValues.EmptyPlayerId);
        repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await handler.Handle(createPlayerCommand, CancellationToken.None);

        await result.Should().ThrowAsync<CannotDeletePlayerNotFoundException>();
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileFindingPlayer() {
        var failure = new InvalidOperationException("database unavailable");
        var command = new DeletePlayerCommand(TestDefaultValues.EmptyPlayerId);
        repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileDeletingPlayer() {
        var failure = new InvalidOperationException("database unavailable");
        var command = new DeletePlayerCommand(TestDefaultValues.EmptyPlayerId);
        var player = new Player(
            command.Id,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultGender,
            TestDefaultValues.DefaultBirthDate);

        repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);
        repository.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }
}
