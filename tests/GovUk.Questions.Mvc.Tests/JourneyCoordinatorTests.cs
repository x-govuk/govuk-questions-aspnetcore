using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUlid;

namespace GovUk.Questions.Mvc.Tests;

public class JourneyCoordinatorTests
{
    [Fact]
    public void GetState_GetsStateFromStateStorage()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var path = new JourneyPath([new JourneyPathStep("/step1")]);

        var expectedState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = expectedState, Path = path });

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator
        {
            Context = context
        };

        // Act
        var state = coordinator.State;

        // Assert
        Assert.Equal(expectedState.Foo, state.Foo);
    }

    [Fact]
    public async Task GetStartingStateAsync_CallsDefaultConstructorAndReturnsNewInstance()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var coordinatorContext = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator { Context = coordinatorContext };

        var httpContext = new DefaultHttpContext();

        var context = new GetStartingStateContext(httpContext);

        // Act
        var state = await coordinator.GetStartingStateAsync(context);

        // Assert
        Assert.NotNull(state);
    }

    [Fact]
    public void Delete_CallsDeleteOnStateStorage()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        coordinator.Delete();

        // Assert
        mockStateStorage.Verify(s => s.DeleteState(coordinator.InstanceId, coordinator.Journey), Times.Once);
    }

    [Fact]
    public void UpdateState_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var path = new JourneyPath([new JourneyPathStep("/step1")]);

        var initialState = new TestState();
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        coordinator.UpdateState(state => state with { Foo = 42 });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public async Task UpdateStateAsync_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var path = new JourneyPath([new JourneyPathStep("/step1")]);

        var initialState = new TestState();
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        await coordinator.UpdateStateAsync(async state =>
        {
            await Task.Yield();
            return state with { Foo = 42 };
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    private class TestJourneyCoordinator : JourneyCoordinator<TestState>;

    private record TestState
    {
        public int Foo { get; set; }
    }
}
