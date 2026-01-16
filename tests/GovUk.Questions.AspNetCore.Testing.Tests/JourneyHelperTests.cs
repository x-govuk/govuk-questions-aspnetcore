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

        // Act
        var coordinator = journeyHelper.CreateInstance<TestJourneyCoordinatorWithDependency>(routeValues, _ => state, pathUrls, services);

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
