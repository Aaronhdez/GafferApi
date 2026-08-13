using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Tests.Unit.Controllers;

public class FacilitiesControllerContractShould
{
    [Test]
    public void ExposeAFacilitiesControllerWithApiControllerRoute()
    {
        var facilitiesControllerType = FindTypeByName("FacilitiesController");

        facilitiesControllerType.Should().NotBeNull();
        facilitiesControllerType!.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true)
            .Should().NotBeEmpty();

        var routeAttribute = facilitiesControllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .FirstOrDefault();
        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v{version:apiVersion}/facilities");
    }

    [Test]
    public void ExposeCreateFacilityActionUsingHttpPost()
    {
        var facilitiesControllerType = FindTypeByName("FacilitiesController");

        facilitiesControllerType.Should().NotBeNull();
        var createMethod = facilitiesControllerType!.GetMethod("Create");
        createMethod.Should().NotBeNull();
        createMethod!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
            .Should().NotBeEmpty();
    }

    [Test]
    public void DefineCreateFacilityRequestWithAddressAndStands()
    {
        var createFacilityRequestType = FindTypeByName("CreateFacilityRequest");

        createFacilityRequestType.Should().NotBeNull();
        createFacilityRequestType!.GetProperty("Name").Should().NotBeNull();
        createFacilityRequestType.GetProperty("Active").Should().NotBeNull();
        createFacilityRequestType.GetProperty("BuildDate").Should().NotBeNull();
        createFacilityRequestType.GetProperty("Type").Should().NotBeNull();
        createFacilityRequestType.GetProperty("Capacity").Should().NotBeNull();
        createFacilityRequestType.GetProperty("Address").Should().NotBeNull();
        createFacilityRequestType.GetProperty("Stands").Should().NotBeNull();
    }

    [Test]
    public void RequireAddressOnCreateFacilityRequest()
    {
        var createFacilityRequestType = FindTypeByName("CreateFacilityRequest");

        createFacilityRequestType.Should().NotBeNull();
        var addressProperty = createFacilityRequestType!.GetProperty("Address");
        addressProperty.Should().NotBeNull();

        addressProperty!.GetCustomAttributes(typeof(RequiredAttribute), true).Should().NotBeEmpty();
    }

    [Test]
    public void LimitStandsToMaximumOfFourOnCreateFacilityRequest()
    {
        var createFacilityRequestType = FindTypeByName("CreateFacilityRequest");

        createFacilityRequestType.Should().NotBeNull();
        var standsProperty = createFacilityRequestType!.GetProperty("Stands");
        standsProperty.Should().NotBeNull();

        var maxLengthAttribute = standsProperty!.GetCustomAttributes(typeof(MaxLengthAttribute), true)
            .Cast<MaxLengthAttribute>()
            .FirstOrDefault();

        maxLengthAttribute.Should().NotBeNull();
        maxLengthAttribute!.Length.Should().Be(4);
    }

    [Test]
    public void DefineFacilityTypeEnumWithExpectedValues()
    {
        var facilityTypeEnum = FindTypeByName("FacilityType");

        facilityTypeEnum.Should().NotBeNull();
        facilityTypeEnum!.IsEnum.Should().BeTrue();
        Enum.GetNames(facilityTypeEnum)
            .Should()
            .BeEquivalentTo(["Stadium", "Arena", "Pitch", "Training"]);
    }

    private static Type? FindTypeByName(string typeName)
    {
        return typeof(PlayerController).Assembly.GetTypes().FirstOrDefault(type => type.Name == typeName);
    }
}