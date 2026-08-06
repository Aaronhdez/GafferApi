using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
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
        const string firstName = "John";
        const string lastName = "Doe";
        const string secondLastName = "Doe";
        DateTime birthDate = new(1990, 01, 01);
        const Gender gender = Gender.MALE;
        var createPlayerCommand = new CreatePlayerCommand(firstName, lastName, secondLastName, birthDate, gender);

        await _handler.Handle(createPlayerCommand, CancellationToken.None);

        await _repository.Received(1).Save(Arg.Is<Player>(p =>
            p.FirstName == firstName
            && p.LastName == lastName
            && p.SecondLastName == secondLastName
            && p.Birthdate == birthDate
            && p.Gender == gender
        ), Arg.Any<CancellationToken>());
    }
}