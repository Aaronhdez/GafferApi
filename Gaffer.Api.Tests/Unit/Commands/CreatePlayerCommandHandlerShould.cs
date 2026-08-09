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
            p.FirstName == TestDefaultValues.DefaultFirstName
            && p.LastName == TestDefaultValues.DefaultLastName
            && p.SecondLastName == TestDefaultValues.DefaultSecondLastName
            && p.Birthdate == TestDefaultValues.DefaultBirthDate
            && p.Gender == TestDefaultValues.DefaultGender
        ), Arg.Any<CancellationToken>());
    }
}