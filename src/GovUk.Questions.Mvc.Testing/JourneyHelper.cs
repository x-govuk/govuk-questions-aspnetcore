using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.Mvc.Testing;

/// <summary>
/// Helper methods for creating instances of <see cref="JourneyCoordinator"/> for testing purposes.
/// </summary>
public class JourneyHelper
{
    private static readonly IServiceProvider _emptyServiceProvider = new ServiceCollection().BuildServiceProvider();

    private readonly JourneyRegistry _journeyRegistry;
    private readonly IJourneyStateStorage _journeyStateStorage;

    internal JourneyHelper(
        JourneyRegistry journeyRegistry,
        IJourneyStateStorage journeyStateStorage)
    {
        ArgumentNullException.ThrowIfNull(journeyRegistry);
        ArgumentNullException.ThrowIfNull(journeyStateStorage);

        _journeyRegistry = journeyRegistry;
        _journeyStateStorage = journeyStateStorage;
    }

    /// <summary>
    /// Creates a new journey instance for the specified coordinator type.
    /// </summary>
    public TCoordinator CreateInstance<TCoordinator>(
        RouteValueDictionary routeValues,
        object state,
        IServiceProvider? serviceProvider = null)
        where TCoordinator : JourneyCoordinator
    {
        var journey = _journeyRegistry.FindJourneyByCoordinatorType(typeof(TCoordinator)) ??
            throw new ArgumentException($"No journey is registered for the coordinator type '{typeof(TCoordinator).FullName}'.", nameof(TCoordinator));

        return (TCoordinator)CreateInstance(journey, routeValues, state, serviceProvider);
    }

    /// <summary>
    /// Creates a new journey instance for the specified coordinator type and journey name.
    /// </summary>
    public TCoordinator CreateInstance<TCoordinator>(
        string journeyName,
        RouteValueDictionary routeValues,
        object state,
        IServiceProvider? serviceProvider = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(state);

        var coordinator = CreateInstance(journeyName, routeValues, state, serviceProvider);

        return (TCoordinator)coordinator;
    }

    /// <summary>
    /// Creates a new journey instance for the specified journey name.
    /// </summary>
    public JourneyCoordinator CreateInstance(
        string journeyName,
        RouteValueDictionary routeValues,
        object state,
        IServiceProvider? serviceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(state);

        var journey = _journeyRegistry.FindJourneyByName(journeyName) ??
            throw new ArgumentException($"No journey with the name '{journeyName}' is registered.", nameof(journeyName));

        return CreateInstance(journey, routeValues, state, serviceProvider);
    }

    /// <summary>
    /// Creates a new journey instance for the specified journey.
    /// </summary>
    public JourneyCoordinator CreateInstance(
        JourneyDescriptor journey,
        RouteValueDictionary routeValues,
        object state,
        IServiceProvider? serviceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(routeValues);

        serviceProvider ??= _emptyServiceProvider;

        if (!JourneyInstanceId.TryCreateNew(journey, routeValues, out var instanceId))
        {
            throw new ArgumentException("Could not create a new JourneyInstanceId with the provided route values.", nameof(routeValues));
        }

        // TODO Validate state type

        _journeyStateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state });

        // TODO Consolidate this with what's in JourneyInstanceProvider
        var coordinatorFactory = _journeyRegistry.GetCoordinatorFactory(journey);
        var coordinator = coordinatorFactory(serviceProvider);
        coordinator.InstanceId = instanceId;
        coordinator.Journey = journey;
        coordinator.StateStorage = _journeyStateStorage;

        return coordinator;
    }
}
