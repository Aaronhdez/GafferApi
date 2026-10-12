using System.Reflection;
using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Tests.TestShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Unit.Startups;

/// <summary>
/// TECHDEBT-006 AC2.4: the model binding 400 of the production registrations writes the same RFC 7807 body as the
/// exception middleware (camelCase, <c>errorTranslationKey</c> and <c>errors</c> at the root).
/// </summary>
public class ValidationStartupShould {
    [Test]
    public void AnswerAModelBindingFailureWithTheCamelCaseValidationProblem() {
        using var provider = StartupServices.Build();
        var factory = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory;
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("request.firstName", "raw binder message");
        modelState.AddModelError("request.address.street", "raw binder message");
        modelState.AddModelError("request", "raw binder message");

        var result = factory(PlayerCreateContext(modelState));

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.StatusCode.Should().Be(400);
        content.ContentType.Should().Be("application/json");
        var body = ProblemJson.Parse(content.Content!);
        body["title"]!.Value<string>().Should().Be("Cannot create player");
        body["status"]!.Value<int>().Should().Be(400);
        body["instance"]!.Value<string>().Should().Be(PlayersPath);
        ProblemJson.TranslationKeyOf(body).Should().Be("Error.Players.Create.Validation");
        ProblemJson.ErrorsOf(body).Properties().Select(property => property.Name).Should()
            .BeEquivalentTo("firstName", "address.street", "body");
        content.Content.Should().NotContain("raw binder message");
    }

    private const string PlayersPath = "/api/v1.0/players";

    private static ActionContext PlayerCreateContext(ModelStateDictionary modelState) {
        var method = typeof(PlayerController).GetMethod(nameof(PlayerController.Create))!;
        var descriptor = new ControllerActionDescriptor {
            ControllerTypeInfo = typeof(PlayerController).GetTypeInfo(),
            MethodInfo = method,
            Parameters = method.GetParameters()
                .Select(parameter => (ParameterDescriptor)new ParameterDescriptor {
                    Name = parameter.Name!,
                    ParameterType = parameter.ParameterType,
                    BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes()),
                })
                .ToList(),
        };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = PlayersPath;
        return new ActionContext(httpContext, new RouteData(), descriptor, modelState);
    }
}
