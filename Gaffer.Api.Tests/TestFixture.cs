using System.Net.Http.Headers;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning.Conventions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared.Clients;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using Gaffer.Api.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NSwag.AspNetCore;

namespace Gaffer.Api.Tests;

[SetUpFixture]
public class TestFixture
{
    public static IConfigurationRoot Configuration;
    public static IGafferApiClient GafferApiClientTest;
    public static IDatabaseConnectionProvider SqlConnectionProviderTest { get; set; }
    public static HttpClient HttpClient;
    public static TestServer TestServer;
    public static UseCasesMock UseCasesMock;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        UseCasesMock = new UseCasesMock();
        Configuration = TestConfiguration.Build();
        var testDatabase = TestConfiguration.RequireDatabaseConnectionString(Configuration);
        // Unregistered instance used only by ClearDatabase; each DI scope gets its own provider.
        SqlConnectionProviderTest = new SqlConnectionProvider(testDatabase);
        TestServer = await StartApiServer(Configuration["ApiUrl"], testDatabase);
        GafferApiClientTest = InitializeGafferHttpClient(TestServer, Configuration["ApiUrl"]);
        await ClearDatabase();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await ClearDatabase();
        if (SqlConnectionProviderTest is IAsyncDisposable cleanupProvider) await cleanupProvider.DisposeAsync();
        GafferApiClientTest?.Dispose();
        TestServer?.Dispose();
    }

    private IGafferApiClient InitializeGafferHttpClient(TestServer testServer, string? baseAddress)
    {
        if (baseAddress == null) throw new NullReferenceException(nameof(baseAddress));
        Uris.BaseAddress = baseAddress;
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(baseAddress);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return new GafferApiClientTest(httpClient);
    }

    /// <summary>
    /// Starts the test API host (same DI as the shared fixture) against the given connection string.
    /// An optional <paramref name="loggerProvider"/> captures the host logs (e.g. the exception middleware).
    /// </summary>
    internal static async Task<TestServer> StartApiServer(string? baseAddress, string testDatabase,
        ILoggerProvider? loggerProvider = null)
    {
        if (baseAddress == null) throw new NullReferenceException(nameof(baseAddress));

        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer();
                webHostBuilder.UseEnvironment("Test");
                webHostBuilder.UseUrls(baseAddress);
                if (loggerProvider != null) webHostBuilder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));
                webHostBuilder.ConfigureAppConfiguration((builderContext, config) => TestConfiguration.AddSources(config));
                webHostBuilder.ConfigureServices(services =>
                {
                    services.AddMvcCore()
                        .AddMvcOptions(options =>
                            options.Filters.Add(new ProducesAttribute("application/json"))
                        )
                        .AddNewtonsoftJson(options =>
                        {
                            options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                            options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        });
                    var apiVersionsDefinition = new GafferApiVersioningDefinition();
                    services.AddSingleton(apiVersionsDefinition);
                    services.AddApiVersioning(options => {
                            options.DefaultApiVersion = new ApiVersion(ApiVersioning.Current, 0);
                            options.AssumeDefaultVersionWhenUnspecified = true;
                            options.ReportApiVersions = true;
                        })
                        .AddMvc(options => {
                            var apiVersionsDefinition = new GafferApiVersioningDefinition();
                            options.Conventions.Controller<PlayerController>().HasApiVersions(apiVersionsDefinition.Versions);
                            options.Conventions.Controller<FacilitiesController>().HasApiVersions(apiVersionsDefinition.Versions);
                        })
                        .AddApiExplorer(options => {
                            options.GroupNameFormat = "'v'VVV";
                            options.SubstituteApiVersionInUrl = true;
                        });
                    services.AddRouting();
                    services.AddControllers();
                    services.AddScoped<IDatabaseConnectionProvider>(_ => new SqlConnectionProvider(testDatabase));
                    services.AddScoped<IPlayerRepository,SqlPlayerRepository>();
                    services.AddScoped<IFacilityRepository, SqlFacilityRepository>();
                    services.AddScoped<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>, CreatePlayerCommandHandler>();
                    services.AddScoped<ICommandHandler<CreateFacilityCommand, CreateFacilityResponse>, CreateFacilityCommandHandler>();
                    services.AddScoped<ICommandHandler<DeleteFacilityCommand>, DeleteFacilityCommandHandler>();
                    services.AddScoped<ICommandHandler<UpdateFacilityCommand>, UpdateFacilityCommandHandler>();
                    services.AddScoped<IQueryHandler<SearchFacilitiesQuery, List<SearchFacilityResult>>, SearchFacilitiesQueryHandler>();
                    services.AddScoped<ICommandHandler<DeletePlayerCommand>, DeletePlayerCommandHandler>();
                    services.AddScoped<ICommandHandler<UpdatePlayerCommand>, UpdatePlayerCommandHandler>();
                    services.AddScoped<IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>, SearchPlayersQueryHandler>();
                    services.AddHttpContextAccessor();
                });
                webHostBuilder.UseStartup<TestStartup>();
            })
            .Build();
        await host.StartAsync();
        return host.GetTestServer();
    }

    private static async Task ClearDatabase()
    {
        // Null when OneTimeSetUp failed before connecting (e.g. missing credentials); keep that failure visible.
        if (SqlConnectionProviderTest is null) return;
        await SqlConnectionProviderTest.ClearAsync(DbTableNames.TablesToDelete);
    }
}

public class TestStartup
{

    public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, IApiVersionDescriptionProvider provider)
    {
        if (environment.IsEnvironment("Test"))
        {
            app
                .UseMiddleware<ExceptionHandlingMiddleware>()
                .UseOpenApi()
                .UseSwaggerUi(settings =>
                {
                    foreach (var description in provider.ApiVersionDescriptions.Reverse())
                    {
                        settings.SwaggerRoutes.Add(new SwaggerUiRoute(description.GroupName.ToUpperInvariant(),
                            $"/swagger/{description.GroupName}/swagger.json"));
                    }
                })
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}