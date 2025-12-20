using GovUk.Questions.Mvc.Description;

namespace GovUk.Questions.Mvc.Tests.Description;

public class JourneyInfoRegistryTests
{
    [Fact]
    public void RegisterJourney_JourneyAlreadyExists_ThrowsArgumentException()
    {
        // Arrange
        var registry = new JourneyInfoRegistry();
        registry.RegisterJourney(typeof(TestJourneyCoordinator), CreateDescriptor());

        // Act
        var ex = Record.Exception(() => registry.RegisterJourney(typeof(TestJourneyCoordinator), CreateDescriptor()));

        // Assert
        var argumentException = Assert.IsType<ArgumentException>(ex);
        Assert.StartsWith("A journey with the name 'Test' has already been registered.", argumentException.Message);
        Assert.Equal("journey", argumentException.ParamName);
    }

    [Fact]
    public void RegisterJourney_NewJourney_AddsJourneySuccessfully()
    {
        // Arrange
        var registry = new JourneyInfoRegistry();

        // Act
        registry.RegisterJourney(typeof(TestJourneyCoordinator), CreateDescriptor());

        // Assert
        var journey = registry.FindJourneyByName("Test");
        Assert.NotNull(journey);
        Assert.Equal("Test", journey.JourneyName);
        Assert.Equal(typeof(TestState), journey.StateType);
    }

    [Fact]
    public void FindJourneyByName_JourneyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var registry = new JourneyInfoRegistry();

        // Act
        var journey = registry.FindJourneyByName("NonExistentJourney");

        // Assert
        Assert.Null(journey);
    }

    [Fact]
    public void FindJourneyByName_ValidJourney_ReturnsJourneyDescriptor()
    {
        // Arrange
        var registry = new JourneyInfoRegistry();
        var descriptor = CreateDescriptor();
        registry.RegisterJourney(typeof(TestJourneyCoordinator), descriptor);

        // Act
        var journey = registry.FindJourneyByName("Test");

        // Assert
        Assert.NotNull(journey);
        Assert.Equal(descriptor, journey);
    }

    private JourneyDescriptor CreateDescriptor() => new("Test", [], typeof(TestState));

    private record TestState;

    private class TestJourneyCoordinator : JourneyCoordinator<TestState>;
}
