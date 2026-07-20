using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.Testing.State;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.AspNetCore.Testing.Tests;

public class JourneyHelperTests
{
    [Fact]
    public void CreateInstance_WithMissingRouteValues_ThrowsArgumentException()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary(); // Missing required "id"
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var ex = Record.Exception(() => journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, _ => state, pathUrls));

        // Assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void CreateInstance_WithCoordinatorTypeOnly_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, _ => state, pathUrls);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor));
        Assert.NotNull(coordinator.InstanceId);
        Assert.Equal(journeyDescriptor, coordinator.Journey);
        Assert.Equal(state, coordinator.State);
    }

    [Fact]
    public void CreateInstance_WithJourneyName_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>("TestJourney", routeValues, _ => state, pathUrls);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor));
        Assert.NotNull(coordinator.InstanceId);
        Assert.Equal(journeyDescriptor, coordinator.Journey);
        Assert.Equal(state, coordinator.State);
    }

    [Fact]
    public void CreateInstance_WithJourneyDescriptor_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, _ => state, pathUrls);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor));
        Assert.NotNull(coordinator.InstanceId);
        Assert.Equal(journeyDescriptor, coordinator.Journey);
        Assert.Equal(state, coordinator.State);
    }

    [Fact]
    public void CreateInstance_WithJourneyDescriptorForCoordinatorWithDependencies_CreatesCoordinatorSuccessfully()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var services = new ServiceCollection()
            .AddSingleton<Dependency>()
            .BuildServiceProvider();

        var journeyDescriptor = new JourneyDescriptor("TestJourneyWithDependency", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinatorWithDependency), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        var coordinatorFactory = () => ActivatorUtilities.CreateInstance<TestJourneyCoordinatorWithDependency>(services);

        // Act
        var coordinator = journeyHelper.CreateInstance(routeValues, _ => state, pathUrls, coordinatorFactory);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(coordinator.Dependency);
    }

    [Fact]
    public void CreateInstance_WithInvalidStateType_ThrowsArgumentException()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var invalidState = new { Bar = "Invalid" }; // Anonymous type, not TestState

        string[] pathUrls = ["/step1"];

        // Act
        var ex = Record.Exception(() => journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, _ => invalidState, pathUrls));

        // Assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public async Task CreateInstanceAsync_WithCoordinatorTypeOnly_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var coordinator = await journeyHelper.CreateInstanceAsync<TestJourneyCoordinator>(routeValues, _ => Task.FromResult<object>(state), pathUrls);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor));
        Assert.NotNull(coordinator.InstanceId);
        Assert.Equal(journeyDescriptor, coordinator.Journey);
        Assert.Equal(state, coordinator.State);
    }

    [Fact]
    public async Task CreateInstanceAsync_WithJourneyName_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var coordinator = await journeyHelper.CreateInstanceAsync<TestJourneyCoordinator>("TestJourney", routeValues, _ => Task.FromResult<object>(state), pathUrls);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor));
        Assert.NotNull(coordinator.InstanceId);
        Assert.Equal(journeyDescriptor, coordinator.Journey);
        Assert.Equal(state, coordinator.State);
    }

    [Fact]
    public async Task CreateInstanceAsync_WithJourneyDescriptor_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var coordinator = await journeyHelper.CreateInstanceAsync(journeyDescriptor, routeValues, _ => Task.FromResult<object>(state), pathUrls);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor));
        Assert.NotNull(coordinator.InstanceId);
        Assert.Equal(journeyDescriptor, coordinator.Journey);
        Assert.Equal(state, coordinator.State);
    }

    [Fact]
    public async Task CreateInstanceAsync_WithJourneyDescriptorForCoordinatorWithDependencies_CreatesCoordinatorSuccessfully()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var services = new ServiceCollection()
            .AddSingleton<Dependency>()
            .BuildServiceProvider();

        var journeyDescriptor = new JourneyDescriptor("TestJourneyWithDependency", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinatorWithDependency), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        var coordinatorFactory = () => ActivatorUtilities.CreateInstance<TestJourneyCoordinatorWithDependency>(services);

        // Act
        var coordinator = await journeyHelper.CreateInstanceAsync(routeValues, _ => Task.FromResult<object>(state), pathUrls, coordinatorFactory);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(coordinator.Dependency);
    }

    [Fact]
    public async Task CreateInstanceAsync_WithInvalidStateType_ThrowsArgumentException()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var invalidState = new { Bar = "Invalid" }; // Anonymous type, not TestState

        string[] pathUrls = ["/step1"];

        // Act
        var exception = await Record.ExceptionAsync(async () => await journeyHelper.CreateInstanceAsync<TestJourneyCoordinator>(routeValues, _ => Task.FromResult<object>(invalidState), pathUrls));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public async Task CreateInstanceAsync_WithMissingRouteValues_ThrowsArgumentException()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary(); // Missing required "id"
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/step1"];

        // Act
        var exception = await Record.ExceptionAsync(async () => await journeyHelper.CreateInstanceAsync<TestJourneyCoordinator>(routeValues, _ => Task.FromResult<object>(state), pathUrls));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void CreateInstance_SeedsPathWithStepIdsThatMatchRuntimeNormalization()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/apply/name", "/apply/date-of-birth"];

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, _ => state, pathUrls);

        // Assert
        var path = journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor)!.Path;

        // The seeded StepIds must omit _jid/returnUrl so they match the current step the runtime
        // derives from a request URL such as "/apply/name?_jid=<key>".
        var runtimeStep = coordinator.CreateStepFromUrl(
            coordinator.InstanceId.EnsureUrlHasKey("/apply/name"));
        Assert.True(path.ContainsStep(runtimeStep));

        Assert.Collection(
            path.Steps,
            step =>
            {
                Assert.Equal("/apply/name", step.StepId);
                Assert.Equal("/apply/name", step.NormalizedUrl);
            },
            step =>
            {
                Assert.Equal("/apply/date-of-birth", step.StepId);
                Assert.Equal("/apply/date-of-birth", step.NormalizedUrl);
            });
    }

    [Fact]
    public void CreateInstance_WithUrlsContainingJidAndReturnUrl_StripsThemFromSeededSteps()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage(Options.Create<GovUkQuestionsOptions>(new()));
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        string[] pathUrls = ["/apply/name?_jid=abc&returnUrl=%2Fhome&foo=bar"];

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, _ => state, pathUrls);

        // Assert
        var path = journeyStateStorage.GetState(coordinator.InstanceId, journeyDescriptor)!.Path;

        var step = Assert.Single(path.Steps);
        Assert.Equal("/apply/name?foo=bar", step.StepId);
        Assert.Equal("/apply/name?foo=bar", step.NormalizedUrl);
    }

    private class TestJourneyCoordinator : JourneyCoordinator<TestState>;

    private class TestJourneyCoordinatorWithDependency(Dependency dependency) : JourneyCoordinator<TestState>
    {
        public Dependency Dependency { get; } = dependency;
    }

    private class Dependency
    {
    }

    private record TestState
    {
        public int Foo { get; set; }
    }
}
