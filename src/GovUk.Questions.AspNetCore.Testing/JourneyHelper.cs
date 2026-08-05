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
    /// Creates a new journey instance for the specified coordinator type.
    /// </summary>
    public async Task<TCoordinator> CreateInstanceAsync<TCoordinator>(
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, Task<object>> getStateAsync,
        IEnumerable<string> pathUrls,
        Func<TCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getStateAsync);
        ArgumentNullException.ThrowIfNull(pathUrls);

        var journey = _journeyRegistry.FindJourneyByCoordinatorType(typeof(TCoordinator)) ??
            throw new ArgumentException($"No journey is registered for the coordinator type '{typeof(TCoordinator).FullName}'.", nameof(TCoordinator));

        Func<JourneyCoordinator>? wrappedFactory = coordinatorFactory;

        return (TCoordinator)await CreateInstanceAsync(journey, routeValues, getStateAsync, pathUrls, wrappedFactory, httpContext);
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
    /// Creates a new journey instance for the specified coordinator type and journey name.
    /// </summary>
    public async Task<TCoordinator> CreateInstanceAsync<TCoordinator>(
        string journeyName,
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, Task<object>> getStateAsync,
        IEnumerable<string> pathUrls,
        Func<TCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
        where TCoordinator : JourneyCoordinator
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getStateAsync);
        ArgumentNullException.ThrowIfNull(pathUrls);

        Func<JourneyCoordinator>? wrappedFactory = coordinatorFactory;

        return (TCoordinator)await CreateInstanceAsync(journeyName, routeValues, getStateAsync, pathUrls, wrappedFactory, httpContext);
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
    /// Creates a new journey instance for the specified journey name.
    /// </summary>
    public async Task<JourneyCoordinator> CreateInstanceAsync(
        string journeyName,
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, Task<object>> getStateAsync,
        IEnumerable<string> pathUrls,
        Func<JourneyCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getStateAsync);

        var journey = _journeyRegistry.FindJourneyByName(journeyName) ??
            throw new ArgumentException($"No journey with the name '{journeyName}' is registered.", nameof(journeyName));

        return await CreateInstanceAsync(journey, routeValues, getStateAsync, pathUrls, coordinatorFactory, httpContext);
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

        var path = BuildPath(pathUrls);

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

    /// <summary>
    /// Creates a new journey instance for the specified journey.
    /// </summary>
    public async Task<JourneyCoordinator> CreateInstanceAsync(
        JourneyDescriptor journey,
        RouteValueDictionary routeValues,
        Func<JourneyInstanceId, Task<object>> getStateAsync,
        IEnumerable<string> pathUrls,
        Func<JourneyCoordinator>? coordinatorFactory = null,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(routeValues);
        ArgumentNullException.ThrowIfNull(getStateAsync);
        ArgumentNullException.ThrowIfNull(pathUrls);

        if (!JourneyInstanceId.TryCreateNew(journey, routeValues, out var instanceId))
        {
            throw new ArgumentException("Could not create a new JourneyInstanceId with the provided route values.", nameof(routeValues));
        }

        var state = await getStateAsync(instanceId);
        var stateType = state.GetType();
        if (!journey.IsStateTypeValid(stateType))
        {
            throw new ArgumentException(
                "State type is not valid; expected " +
                $"'{journey.StateType.FullName}', but got '{stateType.FullName}'.",
                nameof(getStateAsync));
        }

        var path = BuildPath(pathUrls);

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

    private static JourneyPath BuildPath(IEnumerable<string> pathUrls)
    {
        // Normalize each URL the same way the runtime does when it derives the current step
        // from the request URL (see JourneyCoordinator.CreateStepFromUrl). This strips the
        // _jid and returnUrl query parameters so that seeded StepIds match the StepIds the
        // runtime produces, letting callers pass plain page URLs.
        return new JourneyPath(pathUrls.Select(url =>
        {
            var normalizedUrl = JourneyCoordinator.NormalizeUrl(url);
            return new JourneyPathStep(normalizedUrl, normalizedUrl);
        }));
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
