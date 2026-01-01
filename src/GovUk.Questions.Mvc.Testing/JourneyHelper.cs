using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Http;
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
        IEnumerable<string> pathUrls,
        IServiceProvider? serviceProvider = null,
        HttpContext? httpContext = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(pathUrls);

        var journey = _journeyRegistry.FindJourneyByCoordinatorType(typeof(TCoordinator)) ??
            throw new ArgumentException($"No journey is registered for the coordinator type '{typeof(TCoordinator).FullName}'.", nameof(TCoordinator));

        return (TCoordinator)CreateInstance(journey, routeValues, state, pathUrls, serviceProvider);
    }

    /// <summary>
    /// Creates a new journey instance for the specified coordinator type and journey name.
    /// </summary>
    public TCoordinator CreateInstance<TCoordinator>(
        string journeyName,
        RouteValueDictionary routeValues,
        object state,
        IEnumerable<string> pathUrls,
        IServiceProvider? serviceProvider = null,
        HttpContext? httpContext = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(pathUrls);

        var coordinator = CreateInstance(journeyName, routeValues, state, pathUrls, serviceProvider);

        return (TCoordinator)coordinator;
    }

    /// <summary>
    /// Creates a new journey instance for the specified journey name.
    /// </summary>
    public JourneyCoordinator CreateInstance(
        string journeyName,
        RouteValueDictionary routeValues,
        object state,
        IEnumerable<string> pathUrls,
        IServiceProvider? serviceProvider = null,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(state);

        var journey = _journeyRegistry.FindJourneyByName(journeyName) ??
            throw new ArgumentException($"No journey with the name '{journeyName}' is registered.", nameof(journeyName));

        return CreateInstance(journey, routeValues, state, pathUrls, serviceProvider);
    }

    /// <summary>
    /// Creates a new journey instance for the specified journey.
    /// </summary>
    public JourneyCoordinator CreateInstance(
        JourneyDescriptor journey,
        RouteValueDictionary routeValues,
        object state,
        IEnumerable<string> pathUrls,
        IServiceProvider? serviceProvider = null,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(pathUrls);

        serviceProvider ??= _emptyServiceProvider;

        if (!JourneyInstanceId.TryCreateNew(journey, routeValues, out var instanceId))
        {
            throw new ArgumentException("Could not create a new JourneyInstanceId with the provided route values.", nameof(routeValues));
        }

        var stateType = state.GetType();
        if (!journey.IsStateTypeValid(stateType))
        {
            throw new ArgumentException(
                "State type is not valid; expected " +
                $"'{journey.StateType.FullName}', but got '{stateType.FullName}'.",
                nameof(state));
        }

        var path = new JourneyPath(pathUrls.Select(url => new JourneyPathStep(url)));

        _journeyStateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state, Path = path });

        var coordinatorFactory = _journeyRegistry.GetCoordinatorActivator(journey);
        var coordinatorContext = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = _journeyStateStorage,
            HttpContext = httpContext ?? new DefaultHttpContext()
        };
        var coordinator = coordinatorFactory(serviceProvider, coordinatorContext);

        return coordinator;
    }
}
