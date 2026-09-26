using AwesomeAssertions;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Exceptions;

public class ExceptionHandlingMiddlewareShould
{
    private const string FilterNotPermittedKey = "Error.Search.FilterNotPermitted";
    private const string RequestPath = "/facilities";

    // No HTTP route sends arbitrary Facility filters, so the chain repository -> middleware is closed here:
    // the request pipeline runs the real SqlFacilityRepository with a filter field that is not permitted.
    [Test]
    public async Task ReturnABadRequestWhenAFacilitySearchUsesAFilterThatIsNotPermitted()
    {
        var connectionProvider = Substitute.For<IDatabaseConnectionProvider>();
        var repository = new SqlFacilityRepository(connectionProvider);
        var middleware = new ExceptionHandlingMiddleware(async _ => await repository.Get(new FacilitySearchCriteria
        {
            criteriaList = [new SearchCriteria { Name = "Street", Value = "Main Avenue" }]
        }, CancellationToken.None));
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/json");
        var body = await ReadBody(context);
        body["Status"]!.Value<int>().Should().Be(400);
        body["Title"]!.Value<string>().Should().Be(FilterNotPermittedKey);
        body["Detail"]!.Value<string>().Should().Be("FilterNotPermitted");
        body["Instance"]!.Value<string>().Should().Be(RequestPath);
        body["Extensions"]!["ErrorTranslationKey"]!.Value<string>().Should().Be(FilterNotPermittedKey);
    }

    [Test]
    public async Task KeepReturningAnUnknownErrorForAGenericArgumentException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new ArgumentException("boom"));
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = RequestPath;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JObject> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return JObject.Parse(await reader.ReadToEndAsync());
    }
}
