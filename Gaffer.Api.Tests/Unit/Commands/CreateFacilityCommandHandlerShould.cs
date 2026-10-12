using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Commands;

public class CreateFacilityCommandHandlerShould
{
    private IFacilityRepository _repository;
    private CreateFacilityCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IFacilityRepository>();
        _handler = new CreateFacilityCommandHandler(_repository);
    }

    [Test]
    public async Task CreateAFacilityWhileBeingExecuted()
    {
        var address = new CreateFacilityAddressCommand(
            "Main Avenue",
            "100",
            "28001",
            "Madrid",
            "Madrid",
            "Spain");
        var command = new CreateFacilityCommand(
            "National Stadium",
            true,
            new DateTime(2018, 1, 5),
            FacilityType.Stadium,
            52000,
            address,
            [
                new CreateFacilityStandCommand("North", 13000),
                new CreateFacilityStandCommand("South", 13000)
            ]);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).Save(Arg.Is<Facility>(facility =>
            facility != null && facility.Name == command.Name
            && facility.Active == command.Active
            && facility.BuildDate == command.BuildDate
            && facility.Type == command.Type
            && facility.Capacity == command.Capacity
            && facility.Address.Street == address.Street
            && facility.Address.StreetNumber == address.StreetNumber
            && facility.Address.PostalCode == address.PostalCode
            && facility.Address.City == address.City
            && facility.Address.Region == address.Region
            && facility.Address.Country == address.Country
            && facility.Stands.Count == 2
            && facility.Stands.Any(stand => stand.StandName == "North" && stand.StandCapacity == 13000)
            && facility.Stands.Any(stand => stand.StandName == "South" && stand.StandCapacity == 13000)
        ), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnCreatedFacilityResponseWithGeneratedId()
    {
        var command = new CreateFacilityCommand(
            "City Arena",
            true,
            new DateTime(2020, 7, 20),
            FacilityType.Arena,
            18000,
            new CreateFacilityAddressCommand(
                "Arena Road",
                "50",
                "08001",
                "Barcelona",
                "Catalonia",
                "Spain"),
            null);
        Facility? savedFacility = null;

        _repository.When(x => x.Save(Arg.Any<Facility>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => savedFacility = callInfo.Arg<Facility>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        savedFacility.Should().NotBeNull();
        result.Id.Should().Be(savedFacility!.Id);
        savedFacility.Address.Should().NotBeNull();
        savedFacility.Stands.Should().BeEmpty();
    }

    [Test]
    public async Task ThrowExceptionWhenAddressIsMissing()
    {
        var command = new CreateFacilityCommand(
            "Training Center",
            true,
            new DateTime(2021, 3, 15),
            FacilityType.Training,
            1200,
            null,
            null);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesCreate);
        exception.Errors.Keys.Should().Equal("Address");
        await _repository.DidNotReceiveWithAnyArgs().Save(default!, default);
    }

    [Test]
    public async Task ThrowExceptionWhenStandsExceedFour()
    {
        var command = new CreateFacilityCommand(
            "Regional Pitch",
            true,
            new DateTime(2022, 11, 30),
            FacilityType.Pitch,
            6000,
            new CreateFacilityAddressCommand(
                "Pitch Street",
                "8",
                "46001",
                "Valencia",
                "Valencian Community",
                "Spain"),
            [
                new CreateFacilityStandCommand("A", 1000),
                new CreateFacilityStandCommand("B", 1000),
                new CreateFacilityStandCommand("C", 1000),
                new CreateFacilityStandCommand("D", 1000),
                new CreateFacilityStandCommand("E", 1000)
            ]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesCreate);
        exception.Errors.Keys.Should().Equal("Stands");
        await _repository.DidNotReceiveWithAnyArgs().Save(default!, default);
    }

    [Test]
    public async Task RejectInvalidStandsNamingThemByTheirIndexAndNotSaveIt()
    {
        var command = new CreateFacilityCommand("Arena", true, new DateTime(2020, 1, 1), FacilityType.Arena, 100,
            new CreateFacilityAddressCommand("Main Avenue", "1", "28001", "Madrid", "Madrid", "Spain"),
            [new CreateFacilityStandCommand("", 10), new CreateFacilityStandCommand("Valid", -1)]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesCreate);
        exception.Errors.Keys.Should().BeEquivalentTo("Stands[0].StandName", "Stands[1].StandCapacity");
        await _repository.DidNotReceiveWithAnyArgs().Save(default!, default);
    }

    [Test]
    public async Task RejectANullStandTogetherWithTheOtherStandErrorsAndNotSaveIt()
    {
        var command = new CreateFacilityCommand("Arena", true, new DateTime(2020, 1, 1), FacilityType.Arena, 100,
            new CreateFacilityAddressCommand("Main Avenue", "1", "28001", "Madrid", "Madrid", "Spain"),
            [new CreateFacilityStandCommand("Valid", 10), null, new CreateFacilityStandCommand("", 10)]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesCreate);
        exception.Errors.Keys.Should().BeEquivalentTo("Stands[1]", "Stands[2].StandName");
        await _repository.DidNotReceiveWithAnyArgs().Save(default!, default);
    }

    [Test]
    public async Task ReportTheStandErrorsTogetherWithTheFacilityErrors()
    {
        var command = new CreateFacilityCommand("", true, new DateTime(2020, 1, 1), FacilityType.Arena, -1,
            new CreateFacilityAddressCommand("Main Avenue", "1", "28001", "Madrid", "Madrid", "Spain"),
            [new CreateFacilityStandCommand(new string('s', 101), 10)]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which.Errors.Keys
            .Should().BeEquivalentTo("Name", "Capacity", "Stands[0].StandName");
    }

    [Test]
    public async Task RejectAnInvalidFacilityWithEveryInvalidFieldAndNotSaveIt()
    {
        var command = new CreateFacilityCommand(
            "",
            true,
            DateTime.UtcNow.Date.AddDays(1),
            (FacilityType)99,
            -1,
            new CreateFacilityAddressCommand("Main Avenue", new string('1', 21), "28001", "Madrid", "Madrid", "   "),
            null);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesCreate);
        exception.Errors.Keys.Should().BeEquivalentTo("Name", "BuildDate", "Type", "Capacity", "Address.StreetNumber", "Address.Country");
        await _repository.DidNotReceiveWithAnyArgs().Save(default!, default);
    }
}