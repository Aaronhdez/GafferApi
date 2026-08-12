using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Commands;

public class DeletePlayerCommandHandlerShould
{
    private IPlayerRepository _repository;
    private DeletePlayerCommandHandler _handler;
    
    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new DeletePlayerCommandHandler(_repository);
    }

    [Test]
    public async Task DeleteAPlayerWhileBeingExecuted()
    {
        var createPlayerCommand = new DeletePlayerCommand(TestDefaultValues.EmptyPlayerId);
        var player = new Player(
            createPlayerCommand.Id,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultGender,
            TestDefaultValues.DefaultBirthDate);
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);

        await _handler.Handle(createPlayerCommand, CancellationToken.None);

        await _repository.Received(1).Delete(Arg.Is<Guid>(g => g == TestDefaultValues.EmptyPlayerId), Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task ThrowExceptionIfPlayerNotFound()
    {
        var createPlayerCommand = new DeletePlayerCommand(TestDefaultValues.EmptyPlayerId);
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await _handler.Handle(createPlayerCommand, CancellationToken.None);

        await result.Should().ThrowAsync<CannotDeletePlayerNotFoundException>();
    }
}