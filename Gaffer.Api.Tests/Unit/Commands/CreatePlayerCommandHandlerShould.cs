using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Commands;

public class CreatePlayerCommandHandlerShould
{
    private IPlayerRepository _repository;
    private CreatePlayerCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new CreatePlayerCommandHandler(_repository);
    }

    [Test]
    public async Task CreateAPlayerWhileBeingExecuted()
    {
        var createPlayerCommand = new CreatePlayerCommand(
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultBirthDate,
            TestDefaultValues.DefaultGender);

        await _handler.Handle(createPlayerCommand, CancellationToken.None);

        await _repository.Received(1).Save(Arg.Is<Player>(p =>
            p != null && p.FirstName == TestDefaultValues.DefaultFirstName
            && p.LastName == TestDefaultValues.DefaultLastName
            && p.SecondLastName == TestDefaultValues.DefaultSecondLastName
            && p.Birthdate == TestDefaultValues.DefaultBirthDate
            && p.Gender == TestDefaultValues.DefaultGender
        ), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnCreatedPlayerResponseWithGeneratedId()
    {
        var command = new CreatePlayerCommand(
            "Jane",
            "Smith",
            null,
            new DateTime(1995, 4, 12),
            Gender.FEMALE);
        Player? savedPlayer = null;

        _repository.When(x => x.Save(Arg.Any<Player>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => savedPlayer = callInfo.Arg<Player>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        savedPlayer.Should().NotBeNull();
        result.Id.Should().Be(savedPlayer!.Id);
        await _repository.Received(1).Save(Arg.Is<Player>(p =>
            p != null && p.Id == result.Id
            && p.FirstName == command.FirstName
            && p.LastName == command.LastName
            && p.SecondLastName == command.SecondLastName
            && p.Birthdate == command.Birthdate
            && p.Gender == command.Gender
        ), Arg.Any<CancellationToken>());
    }
}