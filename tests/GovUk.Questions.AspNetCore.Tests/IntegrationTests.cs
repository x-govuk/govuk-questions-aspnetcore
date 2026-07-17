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

namespace GovUk.Questions.AspNetCore.Tests;

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

        // Advance to final step
        var secondPagePostResponse = await HttpClient.PostAsync(
            "/integration-test/123/second?_jid=" + journeyInstanceKey,
            new FormUrlEncodedContent([]),
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status302Found, (int)secondPagePostResponse.StatusCode);
        Assert.Equal($"/integration-test/123/final?_jid={journeyInstanceKey}", secondPagePostResponse.Headers.Location?.ToString());

        // Go back to the first step with a returnUrl
        var firstPageWithReturnUrlResponse = await HttpClient.GetAsync(
            "/integration-test/123/first?_jid=" + journeyInstanceKey + "&returnUrl=" + Uri.EscapeDataString("/integration-test/123/final?_jid=" + journeyInstanceKey),
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status200OK, (int)firstPageWithReturnUrlResponse.StatusCode);

        // Re-submit first page with modified state and go to returnUrl
        var firstPageWithReturnUrlPostResponse = await HttpClient.PostAsync(
            "/integration-test/123/first?_jid=" + journeyInstanceKey + "&returnUrl=" + Uri.EscapeDataString("/integration-test/123/final?_jid=" + journeyInstanceKey),
            new FormUrlEncodedContent([
                KeyValuePair.Create("foo", "100")
            ]),
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status302Found, (int)firstPageWithReturnUrlPostResponse.StatusCode);
        Assert.Equal($"/integration-test/123/final?_jid={journeyInstanceKey}", firstPageWithReturnUrlPostResponse.Headers.Location?.ToString());

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

    [Fact]
    public async Task CompleteJourneyWithConfirmationInSameController()
    {
        // Start the journey
        var startResponse = await HttpClient.GetAsync("exclude-test/start", TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status302Found, (int)startResponse.StatusCode);
        var jid = QueryHelpers.ParseQuery(startResponse.Headers.Location!.OriginalString.Split('?')[1])["_jid"].ToString();

        // Advance to the check-answers step
        var startPostResponse = await HttpClient.PostAsync(
            "/exclude-test/start?_jid=" + jid,
            new FormUrlEncodedContent([]),
            TestContext.Current.CancellationToken);
        Assert.Equal($"/exclude-test/check-answers?_jid={jid}", startPostResponse.Headers.Location?.ToString());

        // Submit check-answers, which deletes the instance and redirects to the confirmation page
        var checkAnswersPostResponse = await HttpClient.PostAsync(
            "/exclude-test/check-answers?_jid=" + jid,
            new FormUrlEncodedContent([]),
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status302Found, (int)checkAnswersPostResponse.StatusCode);
        Assert.Equal("/exclude-test/confirmation", checkAnswersPostResponse.Headers.Location?.ToString());

        // The confirmation page is in the same [Journey] controller but opts out with [ExcludeFromJourney],
        // so it is reachable even though the instance has been deleted.
        var confirmationResponse = await HttpClient.GetAsync(
            checkAnswersPostResponse.Headers.Location!,
            TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status200OK, (int)confirmationResponse.StatusCode);
        Assert.Equal("Confirmed", await confirmationResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExcludeFromJourneyOnControllerOptsOutOfInheritedJourney()
    {
        // The [Journey] is inherited from the base controller; the derived controller opts every action out
        // with a class-level [ExcludeFromJourney], so its action is reachable with no journey instance.
        var response = await HttpClient.GetAsync("/exclude-test-inherited/confirmation", TestContext.Current.CancellationToken);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("Confirmed (inherited)", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
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

[JourneyCoordinator("IntegrationTestJourney", ["id"])]
public class IntegrationTestJourneyCoordinator : JourneyCoordinator<IntegrationTestJourneyState>
{
    public override IntegrationTestJourneyState GetStartingState()
    {
        return new IntegrationTestJourneyState { Foo = 42 };
    }
}

[Route("integration-test/{id}")]
[Journey("IntegrationTestJourney")]
public class IntegrationTestController(IntegrationTestJourneyCoordinator coordinator) : Controller
{
    [StartsJourney]
    [HttpGet("first")]
    public IActionResult FirstPage() => GetState();

    [HttpPost("first")]
    public IActionResult FirstPagePost([FromForm] int foo) =>
        coordinator.AdvanceTo(
            Url.Action("SecondPage", coordinator.InstanceId.RouteValues)!,
            s => s.Foo = foo);

    [HttpGet("second")]
    public IActionResult SecondPage() => GetState();

    [HttpPost("second")]
    public IActionResult SecondPagePost() =>
        coordinator.AdvanceTo(Url.Action("FinalPage", coordinator.InstanceId.RouteValues)!);

    [HttpGet("final")]
    public IActionResult FinalPage() => GetState();

    [HttpPost("final")]
    public IActionResult FinalPagePost()
    {
        coordinator.DeleteInstance();
        return NoContent();
    }

    private IActionResult GetState() => Json(coordinator.State);
}

[JourneyCoordinator("ExcludeTestJourney", [])]
public class ExcludeTestJourneyCoordinator : JourneyCoordinator<IntegrationTestJourneyState>
{
    public override IntegrationTestJourneyState GetStartingState() => new() { Foo = 1 };
}

[Route("exclude-test")]
[Journey("ExcludeTestJourney")]
public class ExcludeTestController(ExcludeTestJourneyCoordinator coordinator) : Controller
{
    [StartsJourney]
    [HttpGet("start")]
    public IActionResult Start() => Json(coordinator.State);

    [HttpPost("start")]
    public IActionResult StartPost() =>
        coordinator.AdvanceTo(Url.Action(nameof(CheckAnswers), coordinator.InstanceId.RouteValues)!);

    [HttpGet("check-answers")]
    public IActionResult CheckAnswers() => Json(coordinator.State);

    [HttpPost("check-answers")]
    public IActionResult CheckAnswersPost()
    {
        coordinator.DeleteInstance();
        return RedirectToAction(nameof(Confirmation));
    }

    [ExcludeFromJourney]
    [HttpGet("confirmation")]
    public IActionResult Confirmation() => Content("Confirmed");
}

[Journey("ExcludeTestJourney")]
public abstract class ExcludeTestBaseController : Controller;

[Route("exclude-test-inherited")]
[ExcludeFromJourney]
public class ExcludeTestInheritedController(ExcludeTestJourneyCoordinator coordinator) : ExcludeTestBaseController
{
    [HttpGet("confirmation")]
    public IActionResult Confirmation() => Content(coordinator is not null ? "Confirmed (inherited)" : "");
}
