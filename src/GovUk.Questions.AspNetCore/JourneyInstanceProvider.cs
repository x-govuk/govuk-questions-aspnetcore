using System.Diagnostics;
using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace GovUk.Questions.AspNetCore;

internal class JourneyInstanceProvider(
    IJourneyStateStorage journeyStateStorage,
    IJourneyCoordinatorActivator coordinatorActivator,
    JourneyRegistry journeyRegistry) : IJourneyInstanceProvider
{
    private const string HttpContextItemKey = "GovUk.Questions.AspNetCore.JourneyCoordinator";

    public JourneyCoordinator? GetJourneyInstance(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items.TryGetValue(HttpContextItemKey, out var existingObj) &&
            existingObj is JourneyCoordinator existingCoordinator)
        {
            return existingCoordinator;
        }

        if (GetJourneyMetadata(httpContext) is not { JourneyName: { } journeyName })
        {
            return null;
        }

        var journey = journeyRegistry.FindJourneyByName(journeyName);
        if (journey is null)
        {
            throw new InvalidOperationException($"No journey found with name '{journeyName}'.");
        }

        var valueProvider = CreateValueProvider(httpContext);
        var routeValues = GetRouteValues(journey, valueProvider);

        if (!JourneyInstanceId.TryCreate(journey, routeValues, out var instanceId))
        {
            return null;
        }

        var stateEntry = journeyStateStorage.GetState(instanceId, journey);
        if (stateEntry is null)
        {
            return null;
        }

        var coordinatorContext = new JourneyCoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = journeyStateStorage,
            HttpContext = httpContext
        };
        var coordinator = coordinatorActivator.CreateCoordinator(coordinatorContext);

        httpContext.Items[HttpContextItemKey] = coordinator;

        return coordinator;
    }

    public Task<JourneyCoordinator?> TryCreateNewInstanceAsync(
        HttpContext httpContext,
        Func<CreateNewInstanceStateContext, Task<object>> createInitialState)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(createInitialState);

        return TryCreateNewInstanceCoreAsync(
            httpContext,
            contextFilter: _ => true,
            (_, ctx) => createInitialState(ctx));
    }

    public Task<JourneyCoordinator?> TryCreateNewInstanceAsync(HttpContext httpContext)
    {
        return TryCreateNewInstanceCoreAsync(
            httpContext,
            contextFilter: EndpointStartsJourney,
            (coordinator, _) => coordinator.GetStartingStateSafeAsync());
    }

    private static bool EndpointStartsJourney(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();

        return endpoint?.Metadata.GetMetadata<EndpointJourneyMetadata>()?.StartsJourney is true;
    }

    private static RouteValueDictionary GetRouteValues(JourneyDescriptor journey, CompositeValueProvider valueProvider)
    {
        var routeValues = new RouteValueDictionary();

        foreach (var key in journey.RouteValueKeys.Append(JourneyInstanceId.KeyRouteValueName))
        {
            var valueProviderResult = valueProvider.GetValue(key);

            if (valueProviderResult.Length == 0)
            {
                continue;
            }

            routeValues.Add(key, valueProviderResult.Values);
        }

        return routeValues;
    }

    private CompositeValueProvider CreateValueProvider(HttpContext httpContext)
    {
        var routeValueProviderFactory = new RouteValueProvider(
            BindingSource.Path,
            httpContext.GetRouteData().Values);

        var queryStringValueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            httpContext.Request.Query,
            System.Globalization.CultureInfo.InvariantCulture);

        return new CompositeValueProvider([routeValueProviderFactory, queryStringValueProvider]);
    }

    private EndpointJourneyMetadata? GetJourneyMetadata(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.GetEndpoint()?.Metadata.GetMetadata<EndpointJourneyMetadata>();
    }

    private async Task<JourneyCoordinator?> TryCreateNewInstanceCoreAsync(
        HttpContext httpContext,
        Predicate<HttpContext> contextFilter,
        Func<JourneyCoordinator, CreateNewInstanceStateContext, Task<object>> createInitialStateAsync)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items.ContainsKey(HttpContextItemKey))
        {
            throw new InvalidOperationException("A journey instance has already been created for this request.");
        }

        if (GetJourneyMetadata(httpContext) is not { JourneyName: { } journeyName })
        {
            return null;
        }

        if (!contextFilter(httpContext))
        {
            return null;
        }

        var journey = journeyRegistry.FindJourneyByName(journeyName);
        if (journey is null)
        {
            throw new InvalidOperationException($"No journey found with name '{journeyName}'.");
        }

        var valueProvider = CreateValueProvider(httpContext);
        var routeValues = GetRouteValues(journey, valueProvider);

        if (!JourneyInstanceId.TryCreateNew(journey, routeValues, out var instanceId))
        {
            return null;
        }

        var firstStepUrl = QueryHelpers.AddQueryString(httpContext.Request.GetEncodedPathAndQuery(), JourneyInstanceId.KeyRouteValueName, instanceId.Key);
        var firstStep = JourneyCoordinator.CreateStepFromUrl(firstStepUrl);
        var path = new JourneyPath([firstStep]);

        var coordinatorContext = new JourneyCoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = journeyStateStorage,
            HttpContext = httpContext
        };
        var coordinator = coordinatorActivator.CreateCoordinator(coordinatorContext);

        var createNewInstanceStateContext = new CreateNewInstanceStateContext(instanceId, httpContext);
        var state = await createInitialStateAsync(coordinator, createNewInstanceStateContext);
        Debug.Assert(state.GetType() == journey.StateType);
        journeyStateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state, Path = path });

        httpContext.Items[HttpContextItemKey] = coordinator;

        return coordinator;
    }
}
