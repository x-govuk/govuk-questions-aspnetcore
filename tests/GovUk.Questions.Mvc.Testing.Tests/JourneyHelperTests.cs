using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.Testing.State;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.Mvc.Testing.Tests;

public class JourneyHelperTests
{
    [Fact]
    public void CreateInstance_WithMissingRouteValues_ThrowsArgumentException()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage();
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary(); // Missing required "id"
        var state = new TestState { Foo = 42 };

        // Act
        var ex = Record.Exception(() => journeyHelper.CreateInstance<TestJourneyCoordinator>(routeValues, state));

        // Assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void CreateInstance_WithCoordinatorTypeOnly_CreatesJourneyAndReturnsCoordinatorInstance()
    {
        // Arrange
        var journeyRegistry = new JourneyRegistry();
        var journeyStateStorage = new InMemoryJourneyStateStorage();
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(
            routeValues,
            state);

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
        var journeyStateStorage = new InMemoryJourneyStateStorage();
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(
            "TestJourney",
            routeValues,
            state);

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
        var journeyStateStorage = new InMemoryJourneyStateStorage();
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var journeyDescriptor = new JourneyDescriptor("TestJourney", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinator), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinator>(
            routeValues,
            state);

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
        var journeyStateStorage = new InMemoryJourneyStateStorage();
        var journeyHelper = new JourneyHelper(journeyRegistry, journeyStateStorage);

        var services = new ServiceCollection()
            .AddSingleton<Dependency>()
            .BuildServiceProvider();

        var journeyDescriptor = new JourneyDescriptor("TestJourneyWithDependency", ["id"], typeof(TestState));

        journeyRegistry.RegisterJourney(typeof(TestJourneyCoordinatorWithDependency), journeyDescriptor);

        var routeValues = new RouteValueDictionary { { "id", 123 } };
        var state = new TestState { Foo = 42 };

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinatorWithDependency>(
            routeValues,
            state,
            services);

        // Assert
        Assert.NotNull(coordinator);
        Assert.NotNull(coordinator.Dependency);
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
