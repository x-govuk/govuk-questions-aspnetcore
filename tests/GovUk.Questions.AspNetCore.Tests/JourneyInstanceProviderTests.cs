using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.AspNetCore.Tests;

public class JourneyInstanceProviderTests
{
    [Fact]
    public void GetJourneyInstance_HttpContextContainsJourneyCoordinator_ReturnsExistingInstance()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void GetJourneyInstance_EndpointDoesNotHaveAssociatedJourney_ReturnsNull()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void GetJourneyInstance_EndpointJourneyNotRegistered_ThrowsInvalidOperationException()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void GetJourneyInstance_MissingRouteValues_ReturnsNull()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void GetJourneyInstance_InstanceDoesNotExistInStateStorage_ReturnsNull()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void GetJourneyInstance_ValidRequest_ReturnsCoordinator()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_HttpContextAlreadyContainsCoordinator_ThrowsInvalidOperationException()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_EndpointDoesNotHaveAssociatedJourney_ReturnsNull()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_EndpointIsNotAnnotatedWithStartsJourneyAttribute_ReturnsNull()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_JourneyIsNotRegistered_ThrowsInvalidOperationException()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_MissingRouteValues_ReturnsNull()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_ValidRequest_CreatesNewInstanceIdAndReturnsCoordinator()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();

        var journey = new JourneyDescriptor("TestJourney", ["id"], typeof(TestJourneyState));

        var journeyRegistry = new JourneyRegistry();
        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journey);

        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData { Values = { ["id"] = "42" } } });
        httpContext.Request.Path = "/test/42";
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(
                new JourneyNameMetadata(journey.JourneyName),
                StartsJourneyMetadata.Instance),
            null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = await journeyInstanceProvider.TryCreateNewInstanceAsync(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Context);
        Assert.NotNull(result.InstanceId);
        Assert.Same(result.HttpContext, httpContext);
        Assert.Same(journey, result.Journey);
        Assert.NotNull(result.StateStorage);
        Assert.IsType<TestJourneyState>(result.State);
        Assert.IsType<TestJourneyCoordinator>(result);
    }

    [Fact]
    public void TryGetJourneyName_NoEndpoint_ReturnsFalse()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void TryGetJourneyName_EndpointDoesNotHaveJourneyMetadata_ReturnsFalse()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void TryGetJourneyName_EndpointDoesHaveJourneyMetadata_ReturnsTrueAndOutputsJourneyName()
    {
        throw new NotImplementedException();
    }

    private record TestJourneyState;

    private class TestJourneyCoordinator : JourneyCoordinator<TestJourneyState>;

    private class TestableJourneyStateStorage : IJourneyStateStorage
    {
        private Dictionary<(JourneyDescriptor, JourneyInstanceId), StateStorageEntry> _data = new();

        public void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
        {
            _data.Remove((journey, instanceId));
        }

        public StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
        {
            return _data.TryGetValue((journey, instanceId), out var entry) ? entry : null;
        }

        public void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
        {
            _data[(journey, instanceId)] = stateEntry;
        }
    }
}
