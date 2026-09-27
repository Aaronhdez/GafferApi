using AwesomeAssertions;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Exceptions;

public class ExceptionHandlingMiddlewareShould
{
    private const string FilterNotPermittedKey = "Error.Search.FilterNotPermitted";
    private const string RequestPath = "/facilities";
    private RecordingLoggerProvider logs;

    [SetUp]
    public void SetUp() => logs = new RecordingLoggerProvider();

    [TearDown]
    public void TearDown() => logs.Dispose();

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
        }, CancellationToken.None), logs.CreateLogger<ExceptionHandlingMiddleware>());
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
        var middleware = new ExceptionHandlingMiddleware(_ => throw new ArgumentException("boom"), logs.CreateLogger<ExceptionHandlingMiddleware>());
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task ReturnAnUnknownErrorForAnUnmappedException()
    {
        var middleware = CreateMiddleware(new InvalidOperationException("boom"));
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/json");
        var body = await ReadBody(context);
        body["Status"]!.Value<int>().Should().Be(500);
        body["Title"]!.Value<string>().Should().Be("Unknown error");
        body["Instance"]!.Value<string>().Should().Be(RequestPath);
    }

    [Test]
    public async Task LogAnUnmappedExceptionOnceAsAnErrorWithTheException()
    {
        var exception = new InvalidOperationException("boom");
        var middleware = CreateMiddleware(exception);

        await middleware.Invoke(CreateContext());

        var entries = logs.EntriesFor<ExceptionHandlingMiddleware>();
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
        entries[0].Exception.Should().BeSameAs(exception);
    }

    [Test]
    public async Task LogAnExceptionMappedToBadRequestOnceAsAnError()
    {
        var middleware = CreateMiddleware(new PlayerSearchFilterNotPermittedException("Street"));
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(400);
        var body = await ReadBody(context);
        body["Title"]!.Value<string>().Should().Be(FilterNotPermittedKey);
        body["Detail"]!.Value<string>().Should().Be("FilterNotPermitted");
        body["Instance"]!.Value<string>().Should().Be(RequestPath);
        var entries = logs.EntriesFor<ExceptionHandlingMiddleware>();
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
    }

    [Test]
    public async Task NotLogAnExceptionMappedToNotFound()
    {
        var middleware = CreateMiddleware(new PlayerNotFoundException("not found", Guid.NewGuid()));
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(404);
        logs.EntriesFor<ExceptionHandlingMiddleware>().Should().BeEmpty();
    }

    [Test]
    public async Task ReturnClientClosedRequestWhenTheRequestIsAbortedByTheClient()
    {
        var middleware = CreateMiddleware(new OperationCanceledException());
        var context = CreateAbortedContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(ClientClosedRequest);
        context.Response.Body.Length.Should().Be(0);
    }

    [Test]
    public async Task LogAnAbortedRequestOnceAsInformationWithoutError()
    {
        var middleware = CreateMiddleware(new TaskCanceledException());

        await middleware.Invoke(CreateAbortedContext());

        var entries = logs.EntriesFor<ExceptionHandlingMiddleware>();
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Information);
    }

    [Test]
    public async Task KeepReturningAnUnknownErrorForACancellationWhenTheRequestIsNotAborted()
    {
        var exception = new OperationCanceledException();
        var middleware = CreateMiddleware(exception);
        var context = CreateContext();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
        var entries = logs.EntriesFor<ExceptionHandlingMiddleware>();
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
    }

    private const int ClientClosedRequest = 499;

    private static DefaultHttpContext CreateAbortedContext()
    {
        var context = CreateContext();
        var aborted = new CancellationTokenSource();
        aborted.Cancel();
        context.RequestAborted = aborted.Token;
        return context;
    }

    private ExceptionHandlingMiddleware CreateMiddleware(Exception exception) =>
        new(_ => throw exception, logs.CreateLogger<ExceptionHandlingMiddleware>());

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
