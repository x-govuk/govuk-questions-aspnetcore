using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace GovUk.Questions.AspNetCore.Tests;

public class JourneyCoordinatorTests
{
    [Fact]
    public void GetState_GetsStateFromStateStorage()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var coordinatorContext = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator { Context = coordinatorContext };

        // Act
        var state = await coordinator.GetStartingStateAsync();

        // Assert
        Assert.NotNull(state);
    }

    [Fact]
    public void DeleteInstance_CallsDeleteOnStateStorage()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = new DefaultHttpContext()
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        coordinator.DeleteInstance();

        // Assert
        mockStateStorage.Verify(s => s.DeleteState(coordinator.InstanceId, coordinator.Journey), Times.Once);
    }

    [Fact]
    public void OnInvalidStep_ReturnsRedirectToLastStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1"), new JourneyPathStep("/step2", "/step2")]);

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
        var result = coordinator.OnInvalidStep();

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
    }

    [Fact]
    public void StepIsValid_ReturnsTrueForValidStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1"), new JourneyPathStep("/step2", "/step2")]);

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
        var result = coordinator.StepIsValid(new JourneyPathStep("/step1", "/step1"));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StepIsValid_ReturnsFalseForInvalidStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1"), new JourneyPathStep("/step2", "/step2")]);

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
        var result = coordinator.StepIsValid(new JourneyPathStep("/step3", "/step3"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnsafeSetPathSteps_UpdatesPathInStateStorage()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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

        var newPathSteps = new[] { new JourneyPathStep("/step1", "/step1"), new JourneyPathStep("/step2", "/step2") };

        // Act
        coordinator.UnsafeSetPathSteps(newPathSteps);

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => e.Path.Steps.SequenceEqual(newPathSteps))), Times.Once);
    }

    [Fact]
    public void UpdateState_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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

    [Fact]
    public void UpdateState_BaseClass_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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
        ((JourneyCoordinator)coordinator).UpdateState(state =>
        {
            ((TestState)state).Foo = 42;
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public void UpdateState_BaseClass_InvokesFunctionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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
        ((JourneyCoordinator)coordinator).UpdateState(state => ((TestState)state) with { Foo = 42 });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public async Task UpdateStateAsync_BaseClass_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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
        await ((JourneyCoordinator)coordinator).UpdateStateAsync(async state =>
        {
            await Task.Yield();
            ((TestState)state).Foo = 42;
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public async Task UpdateStateAsync_BaseClass_InvokesFunctionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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
        await ((JourneyCoordinator)coordinator).UpdateStateAsync(async state =>
        {
            await Task.Yield();
            return ((TestState)state) with { Foo = 42 };
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public void UpdateState_GenericClass_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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
        coordinator.UpdateState(state =>
        {
            state.Foo = 42;
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public async Task UpdateStateAsync_GenericClass_InvokesActionWithCurrentStateAndPersistsChanges()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

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
            state.Foo = 42;
        });

        // Assert
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e => ((TestState)e.State).Foo == 42)), Times.Once);
    }

    [Fact]
    public void AdvanceTo_WithoutStateUpdate_RedirectsToNextStepAndUpdatesPath()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = coordinator.AdvanceTo("/step2");

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 123 && // State should remain unchanged
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public void AdvanceTo_BaseClass_WithActionUpdateState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = ((JourneyCoordinator)coordinator).AdvanceTo("/step2", state =>
        {
            ((TestState)state).Foo = 456;
        });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 456 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public void AdvanceTo_BaseClass_WithFuncGetNewState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = ((JourneyCoordinator)coordinator).AdvanceTo("/step2", state => ((TestState)state) with { Foo = 789 });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 789 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public async Task AdvanceToAsync_BaseClass_WithActionUpdateState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = await ((JourneyCoordinator)coordinator).AdvanceToAsync("/step2", async state =>
        {
            await Task.Yield();
            ((TestState)state).Foo = 999;
        });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 999 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public async Task AdvanceToAsync_BaseClass_WithFuncGetNewState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = await ((JourneyCoordinator)coordinator).AdvanceToAsync("/step2", async state =>
        {
            await Task.Yield();
            return ((TestState)state) with { Foo = 111 };
        });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 111 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public void AdvanceTo_GenericClass_WithActionUpdateState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = coordinator.AdvanceTo("/step2", state =>
        {
            state.Foo = 222;
        });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 222 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public void AdvanceTo_GenericClass_WithFuncGetNewState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = coordinator.AdvanceTo("/step2", state => state with { Foo = 333 });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 333 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public async Task AdvanceToAsync_GenericClass_WithActionUpdateState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = await coordinator.AdvanceToAsync("/step2", async state =>
        {
            await Task.Yield();
            state.Foo = 444;
        });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 444 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Fact]
    public async Task AdvanceToAsync_GenericClass_WithFuncGetNewState_UpdatesStateAndRedirectsToNextStep()
    {
        // Arrange
        var mockStateStorage = new Mock<IJourneyStateStorage>();

        var journey = new JourneyDescriptor("test", [], typeof(TestState));

        var instanceId = new JourneyInstanceId("test", new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, UUID.New().ToUrlSafeString() } });

        var path = new JourneyPath([new JourneyPathStep("/step1", "/step1")]);

        var initialState = new TestState { Foo = 123 };
        mockStateStorage
            .Setup(mock => mock.GetState(instanceId, journey))
            .Returns(new StateStorageEntry { State = initialState, Path = path });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/step1";

        var context = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = mockStateStorage.Object,
            HttpContext = httpContext
        };

        var coordinator = new TestJourneyCoordinator { Context = context };

        // Act
        var result = await coordinator.AdvanceToAsync("/step2", async state =>
        {
            await Task.Yield();
            return state with { Foo = 555 };
        });

        // Assert
        var redirectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/step2", redirectResult.Url);
        mockStateStorage.Verify(s => s.SetState(instanceId, journey, It.Is<StateStorageEntry>(e =>
            ((TestState)e.State).Foo == 555 &&
            e.Path.Steps.Count == 2 &&
            e.Path.Steps.Last().StepId == "/step2"
        )), Times.Once);
    }

    [Theory]
    [InlineData("path", new[] { "foo" }, "path")]
    [InlineData("path?foo", new[] { "foo" }, "path")]
    [InlineData("path?foo=42", new[] { "foo" }, "path")]
    [InlineData("path?foo=42&bar=69", new[] { "foo" }, "path?bar=69")]
    [InlineData("path?foo=42&bar=69", new[] { "bar" }, "path?foo=42")]
    [InlineData("path?foo=42&bar=69&baz=3", new[] { "bar" }, "path?foo=42&baz=3")]
    public void GetUrlWithoutQueryParameters(string url, string[] queryParamsToRemove, string expectedUrl)
    {
        // Arrange

        // Act
        var result = JourneyCoordinator.GetUrlWithoutQueryParameters(url, queryParamsToRemove);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    private class TestJourneyCoordinator : JourneyCoordinator<TestState>;

    private record TestState
    {
        public int Foo { get; set; }
    }
}
