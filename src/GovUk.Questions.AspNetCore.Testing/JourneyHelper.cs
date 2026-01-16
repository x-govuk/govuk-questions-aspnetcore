using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GovUk.Questions.AspNetCore.Testing;

/// <summary>
/// Helper methods for creating instances of <see cref="JourneyCoordinator"/> for testing purposes.
/// </summary>
public class JourneyHelper
{
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
        Func<JourneyInstanceId, object> getState,
        IEnumerable<string> pathUrls,
        Func<TCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getState);
        ArgumentNullException.ThrowIfNull(pathUrls);

        var journey = _journeyRegistry.FindJourneyByCoordinatorType(typeof(TCoordinator)) ??
            throw new ArgumentException($"No journey is registered for the coordinator type '{typeof(TCoordinator).FullName}'.", nameof(TCoordinator));

        Func<JourneyCoordinator>? wrappedFactory = coordinatorFactory;

        return (TCoordinator)CreateInstance(journey, routeValues, getState, pathUrls, wrappedFactory, httpContext);
    }

    /// <summary>
    /// Creates a new journey instance for the specified coordinator type and journey name.
    /// </summary>
    public TCoordinator CreateInstance<TCoordinator>(
        string journeyName,
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, object> getState,
        IEnumerable<string> pathUrls,
        Func<TCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getState);
        ArgumentNullException.ThrowIfNull(pathUrls);

        Func<JourneyCoordinator>? wrappedFactory = coordinatorFactory;

        return (TCoordinator)CreateInstance(journeyName, routeValues, getState, pathUrls, wrappedFactory, httpContext);
    }

    /// <summary>
    /// Creates a new journey instance for the specified journey name.
    /// </summary>
    public JourneyCoordinator CreateInstance(
        string journeyName,
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, object> getState,
        IEnumerable<string> pathUrls,
        Func<JourneyCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getState);

        var journey = _journeyRegistry.FindJourneyByName(journeyName) ??
            throw new ArgumentException($"No journey with the name '{journeyName}' is registered.", nameof(journeyName));

        return CreateInstance(journey, routeValues, getState, pathUrls, coordinatorFactory, httpContext);
    }

    /// <summary>
    /// Creates a new journey instance for the specified journey.
    /// </summary>
    public JourneyCoordinator CreateInstance(
        JourneyDescriptor journey,
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, object> getState,
        IEnumerable<string> pathUrls,
        Func<JourneyCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getState);
        ArgumentNullException.ThrowIfNull(pathUrls);

        if (!JourneyInstanceId.TryCreateNew(journey, routeValues, out var instanceId))
        {
            throw new ArgumentException("Could not create a new JourneyInstanceId with the provided route values.", nameof(routeValues));
        }

        var state = getState(instanceId);
        var stateType = state.GetType();
        if (!journey.IsStateTypeValid(stateType))
        {
            throw new ArgumentException(
                "State type is not valid; expected " +
                $"'{journey.StateType.FullName}', but got '{stateType.FullName}'.",
                nameof(getState));
        }

        var journeyQualifiedUrls = pathUrls.Select(url => instanceId.EnsureUrlHasKey(url));
        var path = new JourneyPath(journeyQualifiedUrls.Select(url => new JourneyPathStep(StepId: url, url)));

        _journeyStateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state, Path = path });

        var coordinatorContext = new JourneyCoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = _journeyStateStorage,
            HttpContext = httpContext ?? new DefaultHttpContext()
        };
        var coordinator = ActivateCoordinator(coordinatorContext, coordinatorFactory);

        return coordinator;
    }

    private JourneyCoordinator ActivateCoordinator(
        JourneyCoordinatorContext context,
        Func<JourneyCoordinator>? factory)
    {
        if (factory is null)
        {
            var coordinatorType = _journeyRegistry.GetCoordinatorType(context.Journey);
            factory = () => (JourneyCoordinator)Activator.CreateInstance(coordinatorType)!;
        }

        var coordinator = factory();
        coordinator.Context = context;
        return coordinator;
    }
}
