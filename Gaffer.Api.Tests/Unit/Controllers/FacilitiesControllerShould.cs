using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Controllers;

public class FacilitiesControllerShould
{
    private FacilitiesController _controller;
    private ICommandHandler<CreateFacilityCommand, CreateFacilityResponse> _createFacilityCommandHandler;
    private IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>> _searchFacilitiesQueryHandler;
    private ICommandHandler<DeleteFacilityCommand> _deleteFacilityCommandHandler;
    private ICommandHandler<UpdateFacilityCommand> _updateFacilityCommandHandler;
    private CancellationTokenSource _cancellation;

    [SetUp]
    public void Setup()
    {
        _createFacilityCommandHandler = Substitute.For<ICommandHandler<CreateFacilityCommand, CreateFacilityResponse>>();
        _searchFacilitiesQueryHandler = Substitute.For<IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>>>();
        _deleteFacilityCommandHandler = Substitute.For<ICommandHandler<DeleteFacilityCommand>>();
        _updateFacilityCommandHandler = Substitute.For<ICommandHandler<UpdateFacilityCommand>>();
        _controller = new FacilitiesController(_createFacilityCommandHandler, _searchFacilitiesQueryHandler,
            _deleteFacilityCommandHandler, _updateFacilityCommandHandler);
        _cancellation = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown() => _cancellation.Dispose();

    [Test]
    public async Task PassTheRequestTokenToTheQueryWhenGettingAFacilityById()
    {
        var id = Guid.NewGuid();
        _searchFacilitiesQueryHandler.Handle(Arg.Any<SearchFacilitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns([new SearchFacilityResult(id, "Arena", true, default, FacilityType.Arena, 1, null!, [])]);

        await _controller.GetBy(id, _cancellation.Token);

        await _searchFacilitiesQueryHandler.Received(1).Handle(Arg.Any<SearchFacilitiesQuery>(), _cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheQueryWhenGettingAllFacilities()
    {
        _searchFacilitiesQueryHandler.Handle(Arg.Any<SearchFacilitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchFacilityResult>());

        await _controller.GetAll(_cancellation.Token);

        await _searchFacilitiesQueryHandler.Received(1).Handle(Arg.Any<SearchFacilitiesQuery>(), _cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenCreatingAFacility()
    {
        _createFacilityCommandHandler.Handle(Arg.Any<CreateFacilityCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreateFacilityResponse(Guid.NewGuid()));

        await _controller.Create(new CreateFacilityRequest { Address = new CreateFacilityAddressRequest() },
            _cancellation.Token);

        await _createFacilityCommandHandler.Received(1).Handle(Arg.Any<CreateFacilityCommand>(), _cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenDeletingAFacility()
    {
        await _controller.Delete(Guid.NewGuid(), _cancellation.Token);

        await _deleteFacilityCommandHandler.Received(1).Handle(Arg.Any<DeleteFacilityCommand>(), _cancellation.Token);
    }

    [Test]
    public async Task PassTheRequestTokenToTheCommandWhenUpdatingAFacility()
    {
        await _controller.Update(Guid.NewGuid(), new UpdateFacilityRequest { Address = new UpdateFacilityAddressRequest() },
            _cancellation.Token);

        await _updateFacilityCommandHandler.Received(1).Handle(Arg.Any<UpdateFacilityCommand>(), _cancellation.Token);
    }
}
