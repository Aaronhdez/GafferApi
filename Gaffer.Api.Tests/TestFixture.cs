using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning.Conventions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared.Clients;
using Gaffer.Api.Shared.ConnectionProviders;
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
using Newtonsoft.Json;
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
        Configuration = PrepareConfiguration(new ConfigurationBuilder()).Build();
        TestServer = await InitializeApiServer(Configuration["ApiUrl"], Configuration["ConnectionStrings:Database"]);
        GafferApiClientTest = InitializeGafferHttpClient(TestServer, Configuration["ApiUrl"]);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
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

    private static async Task<TestServer> InitializeApiServer(string? baseAddress, string testDatabase)
    {
        if (baseAddress == null) throw new NullReferenceException(nameof(baseAddress));

        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer();
                webHostBuilder.UseEnvironment("Test");
                webHostBuilder.UseUrls(baseAddress);
                webHostBuilder.ConfigureAppConfiguration((builderContext, config) => { PrepareConfiguration(config); });
                webHostBuilder.ConfigureServices(services =>
                {
                    services.AddMvcCore()
                        .AddMvcOptions(options =>
                            options.Filters.Add(new ProducesAttribute("application/json"))
                        )
                        .AddJsonOptions(options =>
                        {
                            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        })
                        .AddNewtonsoftJson(options =>
                        {
                            options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
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
                        })
                        .AddApiExplorer(options => {
                            options.GroupNameFormat = "'v'VVV";
                            options.SubstituteApiVersionInUrl = true;
                        });
                    services.AddRouting();
                    services.AddControllers();
                    SqlConnectionProviderTest = new SqlConnectionProvider(testDatabase);
                    services.AddScoped<IDatabaseConnectionProvider>(_ => SqlConnectionProviderTest);
                    services.AddScoped<IPlayerRepository,SqlPlayerRepository>();
                    services.AddScoped<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>, CreatePlayerCommandHandler>();
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



    private static IConfigurationBuilder PrepareConfiguration(IConfigurationBuilder configurationBuilder)
    {
        return configurationBuilder.SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("testsettings.json", optional: false);
    }
}

public class TestStartup
{

    public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, IApiVersionDescriptionProvider provider)
    {
        if (environment.IsEnvironment("Test"))
        {
            app
                //.UseMiddleware<ExceptionHandlingMiddleware>()
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
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    //.RequireAuthorization();
                });
        }
    }
}