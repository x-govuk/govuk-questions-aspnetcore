using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Mvc;
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

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var expectedState = new TestState() { Foo = 123 };
        mockStateStorage.Setup(mock => mock.GetState(instanceId)).Returns(new StateStorageEntry() { State = expectedState });

        var coordinator = new TestJourneyCoordinator
        {
            Journey = new JourneyDescriptor("test", [], typeof(TestState)),
            InstanceId = instanceId,
            StateStorage = mockStateStorage.Object
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
        var coordinator = new TestJourneyCoordinator
        {
            Journey = new JourneyDescriptor("test", [], typeof(TestState)),
            InstanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } })
        };

        var actionContext = new ActionContext();

        var context = new GetStartingStateContext(actionContext);

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

        var coordinator = new TestJourneyCoordinator
        {
            Journey = new JourneyDescriptor("test", [], typeof(TestState)),
            InstanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } }),
            StateStorage = mockStateStorage.Object
        };

        // Act
        coordinator.Delete();

        // Assert
        mockStateStorage.Verify(s => s.DeleteState(coordinator.InstanceId), Times.Once);
    }

    [Fact]
    public void UpdateState_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var initialState = new TestState();
        mockStateStorage.Setup(mock => mock.GetState(instanceId)).Returns(new StateStorageEntry() { State = initialState });

        var coordinator = new TestJourneyCoordinator
        {
            Journey = new JourneyDescriptor("test", [], typeof(TestState)),
            InstanceId = instanceId,
            StateStorage = mockStateStorage.Object
        };

        // Act
        coordinator.UpdateState(state => state.Foo = 42);

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public async Task UpdateStateAsync_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, Ulid.NewUlid() } });

        var initialState = new TestState();
        mockStateStorage.Setup(mock => mock.GetState(instanceId)).Returns(new StateStorageEntry() { State = initialState });

        var coordinator = new TestJourneyCoordinator
        {
            Journey = new JourneyDescriptor("test", [], typeof(TestState)),
            InstanceId = instanceId,
            StateStorage = mockStateStorage.Object
        };

        // Act
        await coordinator.UpdateStateAsync(async state =>
        {
            await Task.Yield();
            state.Foo = 42;
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    private class TestJourneyCoordinator : JourneyCoordinator<TestState>;

    private record TestState
    {
        public int Foo { get; set; }
    };
}
