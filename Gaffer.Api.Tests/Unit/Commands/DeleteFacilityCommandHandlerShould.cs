using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands;

public class DeleteFacilityCommandHandlerShould {
    private IFacilityRepository repository;
    private DeleteFacilityCommandHandler handler;

    [SetUp]
    public void Setup() {
        repository = Substitute.For<IFacilityRepository>();
        handler = new DeleteFacilityCommandHandler(repository);
    }

    [Test]
    public async Task DeleteAFacilityWhileBeingExecuted() {
        var command = new DeleteFacilityCommand(Guid.NewGuid());
        var facility = new Facility(
            command.Id,
            "National Stadium",
            true,
            new DateTime(2018, 1, 5),
            FacilityType.Stadium,
            52000,
            new Address(Guid.NewGuid(), "Main Avenue", "100", "28001", "Madrid", "Madrid", "Spain"),
            null);
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([facility]);

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1).Delete(Arg.Is<Guid>(g => g == command.Id), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ThrowExceptionIfFacilityNotFound() {
        var command = new DeleteFacilityCommand(Guid.NewGuid());
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotDeleteFacilityNotFoundException>();
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileFindingFacility() {
        var failure = new InvalidOperationException("database unavailable");
        var command = new DeleteFacilityCommand(Guid.NewGuid());
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileDeletingFacility() {
        var failure = new InvalidOperationException("database unavailable");
        var command = new DeleteFacilityCommand(Guid.NewGuid());
        var facility = new Facility(
            command.Id,
            "City Arena",
            true,
            new DateTime(2020, 7, 20),
            FacilityType.Arena,
            18000,
            new Address(Guid.NewGuid(), "Arena Road", "50", "08001", "Barcelona", "Catalonia", "Spain"),
            null);
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([facility]);
        repository.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }
}
