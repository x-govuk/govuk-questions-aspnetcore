using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GovUk.Questions.Mvc.Tests;

public class IntegrationTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private HttpClient HttpClient => fixture.HttpClient;

    [Fact]
    public async Task CompleteJourney()
    {
        // Start journey
        var firstPageResponse = await HttpClient.GetAsync("integration-test/123/first", TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status302Found, (int)firstPageResponse.StatusCode);
        Assert.StartsWith("/integration-test/123/first?_jid=", firstPageResponse.Headers.Location?.ToString());
        var journeyInstanceKey = QueryHelpers.ParseQuery(firstPageResponse.Headers.Location!.OriginalString.Split('?')[1])["_jid"].ToString();

        // Read initial state
        var firstPageGetWithJidResponse = await HttpClient.GetAsync(firstPageResponse.Headers.Location!, TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status200OK, (int)firstPageGetWithJidResponse.StatusCode);
        await AssertStateAsync(firstPageGetWithJidResponse, 42);

        // Modify state and go to next step
        var firstPagePostResponse = await HttpClient.PostAsync(
            "/integration-test/123/first?_jid=" + journeyInstanceKey,
            new FormUrlEncodedContent([
                KeyValuePair.Create("foo", "69")
            ]),
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status302Found, (int)firstPagePostResponse.StatusCode);
        Assert.Equal($"/integration-test/123/second?_jid={journeyInstanceKey}", firstPagePostResponse.Headers.Location?.ToString());

        // Read modified state
        var secondPageResponse = await HttpClient.GetAsync(firstPagePostResponse.Headers.Location!, TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status200OK, (int)secondPageResponse.StatusCode);
        await AssertStateAsync(secondPageResponse, 69);

        // End the journey
        var finalPagePostResponse = await HttpClient.PostAsync(
            "/integration-test/123/final?_jid=" + journeyInstanceKey,
            new FormUrlEncodedContent([]),
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status204NoContent, (int)finalPagePostResponse.StatusCode);

        // Confirm the journey is no longer available
        var finalPageGetResponse = await HttpClient.GetAsync(
            "/integration-test/123/final?_jid=" + journeyInstanceKey,
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status400BadRequest, (int)finalPageGetResponse.StatusCode);

        async Task AssertStateAsync(HttpResponseMessage response, int expectedFoo)
        {
            var state = await response.Content.ReadFromJsonAsync<IntegrationTestJourneyState>(TestContext.Current.CancellationToken);
            Assert.NotNull(state);
            Assert.Equal(expectedFoo, state.Foo);
        }
    }
}

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly IHost _host;
    private HttpClient? _httpClient;

    public IntegrationTestFixture()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices((ctx, services) =>
                    {
                        services.AddMvc();

                        services.AddSession();

                        services.AddGovUkQuestions();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();

                        app.UseSession();

                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .Build();
    }

    public HttpClient HttpClient => _httpClient ??= CreateClient();

    private HttpClient CreateClient() => new(new CookieContainerHandler { InnerHandler = _host.GetTestServer().CreateHandler() })
    {
        BaseAddress = _host.GetTestServer().BaseAddress
    };

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _httpClient?.Dispose();
        _host.Dispose();
        return ValueTask.CompletedTask;
    }

    async ValueTask IAsyncLifetime.InitializeAsync() => await _host.StartAsync();
}

public record IntegrationTestJourneyState
{
    public required int Foo { get; set; }
}

[Journey("IntegrationTestJourney", ["id"])]
public class IntegrationTestJourneyCoordinator : JourneyCoordinator<IntegrationTestJourneyState>
{
    public override IntegrationTestJourneyState GetStartingState(GetStartingStateContext context)
    {
        return new IntegrationTestJourneyState { Foo = 42 };
    }
}

[Route("integration-test/{id}")]
[JourneyName("IntegrationTestJourney")]
public class IntegrationTestController(IntegrationTestJourneyCoordinator coordinator) : Controller
{
    [StartsJourney]
    [HttpGet("first")]
    public IActionResult FirstPage() => GetState();

    [HttpPost("first")]
    public IActionResult FirstPagePost([FromForm] int foo) =>
        coordinator.Advance(
            Url.Action("SecondPage", coordinator.InstanceId.RouteValues)!,
            s => s.Foo = foo);

    [HttpGet("second")]
    public IActionResult SecondPage() => GetState();

    [HttpPost("second")]
    public IActionResult SecondPagePost() =>
        coordinator.Advance(Url.Action("FinalPage", coordinator.InstanceId.RouteValues)!);

    [HttpGet("final")]
    public IActionResult FinalPage() => GetState();

    [HttpPost("final")]
    public IActionResult FinalPagePost()
    {
        coordinator.Delete();
        return NoContent();
    }

    private IActionResult GetState() => Json(coordinator.State);
}
