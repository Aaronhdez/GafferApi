using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Facilities;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Queries.Facilities;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Controllers;

public class FacilitiesControllerShould {
    private FacilitiesController controller;
    private ICommandHandler<CreateFacilityCommand, CreateFacilityResponse> createFacilityCommandHandler;
    private IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>> searchFacilitiesQueryHandler;
    private ICommandHandler<DeleteFacilityCommand> deleteFacilityCommandHandler;
    private ICommandHandler<UpdateFacilityCommand> updateFacilityCommandHandler;
    private CancellationTokenSource cancellation;

    [SetUp]
    public void Setup() {
        createFacilityCommandHandler = Substitute.For<ICommandHandler<CreateFacilityCommand, CreateFacilityResponse>>();
        searchFacilitiesQueryHandler = Substitute.For<IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>>>();
        deleteFacilityCommandHandler = Substitute.For<ICommandHandler<DeleteFacilityCommand>>();
        updateFacilityCommandHandler = Substitute.For<ICommandHandler<UpdateFacilityCommand>>();
        controller = new FacilitiesController(createFacilityCommandHandler, searchFacilitiesQueryHandler,
            deleteFacilityCommandHandler, updateFacilityCommandHandler);
        cancellation = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown() => cancellation.Dispose();

    [Test]
    public async Task PassTheRequestTokenToTheQueryWhenGettingAFacilityById() {
        var id = Guid.NewGuid();
        searchFacilitiesQueryHandler.Handle(Arg.Any<SearchFacilitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns([new SearchFacilityResult(id, "Arena", true, default, FacilityType.Arena, 1, null!, [])]);

        await controller.GetBy(id, cancellation.Token);

        await searchFacilitiesQueryHandler.Received(1).Handle(Arg.Any<SearchFacilitiesQuery>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheQueryWhenGettingAllFacilities() {
        searchFacilitiesQueryHandler.Handle(Arg.Any<SearchFacilitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchFacilityResult>());

        await controller.GetAll(cancellation.Token);

        await searchFacilitiesQueryHandler.Received(1).Handle(Arg.Any<SearchFacilitiesQuery>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenCreatingAFacility() {
        createFacilityCommandHandler.Handle(Arg.Any<CreateFacilityCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreateFacilityResponse(Guid.NewGuid()));

        await controller.Create(new CreateFacilityRequest { Address = new CreateFacilityAddressRequest() },
            cancellation.Token);

        await createFacilityCommandHandler.Received(1).Handle(Arg.Any<CreateFacilityCommand>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenDeletingAFacility() {
        await controller.Delete(Guid.NewGuid(), cancellation.Token);

        await deleteFacilityCommandHandler.Received(1).Handle(Arg.Any<DeleteFacilityCommand>(), cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenUpdatingAFacility() {
        await controller.Update(Guid.NewGuid(), new UpdateFacilityRequest { Address = new UpdateFacilityAddressRequest() },
            cancellation.Token);

        await updateFacilityCommandHandler.Received(1).Handle(Arg.Any<UpdateFacilityCommand>(), cancellation.Token);
    }
}
