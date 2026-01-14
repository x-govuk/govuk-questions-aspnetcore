using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.AspNetCore.Tests;

public class JourneyInstanceProviderTests
{


    [Fact]
    public void GetJourneyInfo_NoEndpoint_ReturnsNull()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        // No endpoint set

        // Act
        var result = journeyInstanceProvider.GetJourneyInfo(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetJourneyInfo_EndpointDoesNotHaveJourneyMetadata_ReturnsNull()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(), null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = journeyInstanceProvider.GetJourneyInfo(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetJourneyInfo_EndpointDoesHaveJourneyMetadata_ReturnsJourneyInfo()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(new JourneyNameMetadata("TestJourney", Optional: true)),
            null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = journeyInstanceProvider.GetJourneyInfo(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestJourney", result.JourneyName);
        Assert.True(result.Optional);
    }

    [Fact]
    public void GetJourneyInstance_CoordinatorWithDependencies_ResolvesFromServiceProvider()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();

        var journey = new JourneyDescriptor("TestJourney", ["id"], typeof(TestJourneyState));

        var journeyRegistry = new JourneyRegistry();
        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinatorWithDependencies), journey);

        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var testService = new TestService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(testService)
            .BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var key = UUID.New().ToUrlSafeString();
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData { Values = { ["id"] = "42" } } });
        httpContext.Request.Path = "/test/42";
        httpContext.Request.QueryString = new QueryString($"?_jid={key}");
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(new JourneyNameMetadata(journey.JourneyName)),
            null);
        httpContext.SetEndpoint(endpoint);

        // Create instance ID and add state to storage
        var instanceId = new JourneyInstanceId(journey.JourneyName, new RouteValueDictionary { ["id"] = "42", [JourneyInstanceId.KeyRouteValueName] = key });
        var state = new TestJourneyState();
        var path = new JourneyPath([JourneyCoordinator.CreateStepFromUrl($"/test/42?_jid={key}")]);
        stateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state, Path = path });

        // Act
        var result = journeyInstanceProvider.GetJourneyInstance(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestJourneyCoordinatorWithDependencies>(result);
        var coordinator = (TestJourneyCoordinatorWithDependencies)result;
        Assert.Same(testService, coordinator.TestService);
    }

    [Fact]
    public void GetJourneyInstance_HttpContextContainsJourneyCoordinator_ReturnsExistingInstance()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;

        var existingCoordinator = new TestJourneyCoordinator();
        httpContext.Items["GovUk.Questions.AspNetCore.JourneyCoordinator"] = existingCoordinator;

        // Act
        var result = journeyInstanceProvider.GetJourneyInstance(httpContext);

        // Assert
        Assert.Same(existingCoordinator, result);
    }

    [Fact]
    public void GetJourneyInstance_EndpointDoesNotHaveAssociatedJourney_ReturnsNull()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(), null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = journeyInstanceProvider.GetJourneyInstance(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetJourneyInstance_EndpointJourneyNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(new JourneyNameMetadata("UnregisteredJourney")),
            null);
        httpContext.SetEndpoint(endpoint);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => journeyInstanceProvider.GetJourneyInstance(httpContext));
        Assert.Equal("No journey found with name 'UnregisteredJourney'.", exception.Message);
    }

    [Fact]
    public void GetJourneyInstance_MissingRouteValues_ReturnsNull()
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
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData() }); // No route values
        httpContext.Request.Path = "/test";
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(new JourneyNameMetadata(journey.JourneyName)),
            null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = journeyInstanceProvider.GetJourneyInstance(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetJourneyInstance_InstanceDoesNotExistInStateStorage_ReturnsNull()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();

        var journey = new JourneyDescriptor("TestJourney", ["id"], typeof(TestJourneyState));

        var journeyRegistry = new JourneyRegistry();
        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journey);

        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var key = UUID.New().ToUrlSafeString();
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData { Values = { ["id"] = "42" } } });
        httpContext.Request.Path = "/test/42";
        httpContext.Request.QueryString = new QueryString($"?_jid={key}");
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(new JourneyNameMetadata(journey.JourneyName)),
            null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = journeyInstanceProvider.GetJourneyInstance(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetJourneyInstance_ValidRequest_ReturnsCoordinator()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();

        var journey = new JourneyDescriptor("TestJourney", ["id"], typeof(TestJourneyState));

        var journeyRegistry = new JourneyRegistry();
        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journey);

        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var key = UUID.New().ToUrlSafeString();
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData { Values = { ["id"] = "42" } } });
        httpContext.Request.Path = "/test/42";
        httpContext.Request.QueryString = new QueryString($"?_jid={key}");
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(new JourneyNameMetadata(journey.JourneyName)),
            null);
        httpContext.SetEndpoint(endpoint);

        // Create instance ID and add state to storage
        var instanceId = new JourneyInstanceId(journey.JourneyName, new RouteValueDictionary { ["id"] = "42", [JourneyInstanceId.KeyRouteValueName] = key });
        var state = new TestJourneyState();
        var path = new JourneyPath([JourneyCoordinator.CreateStepFromUrl($"/test/42?_jid={key}")]);
        stateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state, Path = path });

        // Act
        var result = journeyInstanceProvider.GetJourneyInstance(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Context);
        Assert.Same(httpContext, result.HttpContext);
        Assert.Same(journey, result.Journey);
        Assert.IsType<TestJourneyCoordinator>(result);
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_HttpContextAlreadyContainsCoordinator_ThrowsInvalidOperationException()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;

        var existingCoordinator = new TestJourneyCoordinator();
        httpContext.Items["GovUk.Questions.AspNetCore.JourneyCoordinator"] = existingCoordinator;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => journeyInstanceProvider.TryCreateNewInstanceAsync(httpContext));
        Assert.Equal("A journey instance has already been created for this request.", exception.Message);
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_EndpointDoesNotHaveAssociatedJourney_ReturnsNull()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(), null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = await journeyInstanceProvider.TryCreateNewInstanceAsync(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_EndpointIsNotAnnotatedWithStartsJourneyAttribute_ReturnsNull()
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
            new EndpointMetadataCollection(new JourneyNameMetadata(journey.JourneyName)), // No StartsJourneyMetadata
            null);
        httpContext.SetEndpoint(endpoint);

        // Act
        var result = await journeyInstanceProvider.TryCreateNewInstanceAsync(httpContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_JourneyIsNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();
        var journeyRegistry = new JourneyRegistry();
        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        var endpoint = new Endpoint(
            null,
            new EndpointMetadataCollection(
                new JourneyNameMetadata("UnregisteredJourney"),
                StartsJourneyMetadata.Instance),
            null);
        httpContext.SetEndpoint(endpoint);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => journeyInstanceProvider.TryCreateNewInstanceAsync(httpContext));
        Assert.Equal("No journey found with name 'UnregisteredJourney'.", exception.Message);
    }

    [Fact]
    public async Task TryCreateNewInstanceAsync_MissingRouteValues_ReturnsNull()
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
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = new RouteData() }); // No route values
        httpContext.Request.Path = "/test";
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
        Assert.Null(result);
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
    public async Task TryCreateNewInstanceAsync_CoordinatorWithDependencies_ResolvesFromServiceProvider()
    {
        // Arrange
        var stateStorage = new TestableJourneyStateStorage();

        var journey = new JourneyDescriptor("TestJourney", ["id"], typeof(TestJourneyState));

        var journeyRegistry = new JourneyRegistry();
        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinatorWithDependencies), journey);

        var journeyInstanceProvider = new JourneyInstanceProvider(stateStorage, journeyRegistry);

        var testService = new TestService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(testService)
            .BuildServiceProvider();
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
        Assert.IsType<TestJourneyCoordinatorWithDependencies>(result);
        var coordinator = (TestJourneyCoordinatorWithDependencies)result;
        Assert.Same(testService, coordinator.TestService);
    }

    private record TestJourneyState;

    private class TestJourneyCoordinator : JourneyCoordinator<TestJourneyState>;

    private class TestService;

    private class TestJourneyCoordinatorWithDependencies(TestService testService) : JourneyCoordinator<TestJourneyState>
    {
        public TestService TestService { get; } = testService;
    }

    private class TestableJourneyStateStorage : IJourneyStateStorage
    {
        private Dictionary<string, StateStorageEntry> _data = new();

        public void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
        {
            _data.Remove(GetKey(instanceId, journey));
        }

        public StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
        {
            return _data.TryGetValue(GetKey(instanceId, journey), out var entry) ? entry : null;
        }

        public void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
        {
            _data[GetKey(instanceId, journey)] = stateEntry;
        }

        private static string GetKey(JourneyInstanceId instanceId, JourneyDescriptor journey)
        {
            return $"{journey.JourneyName}|{instanceId}";
        }
    }
}
