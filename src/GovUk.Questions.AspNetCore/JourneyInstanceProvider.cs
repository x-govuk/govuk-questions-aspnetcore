using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace GovUk.Questions.AspNetCore;

internal class JourneyInstanceProvider(IJourneyStateStorage journeyStateStorage, JourneyRegistry journeyRegistry) : IJourneyInstanceProvider
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

        if (!TryGetJourneyName(httpContext, out var journeyName))
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

        var coordinatorFactory = journeyRegistry.GetCoordinatorActivator(journey);
        var coordinatorContext = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = journeyStateStorage,
            HttpContext = httpContext
        };
        var coordinator = coordinatorFactory(httpContext.RequestServices, coordinatorContext);

        httpContext.Items[HttpContextItemKey] = coordinator;

        return coordinator;
    }

    public async Task<JourneyCoordinator?> TryCreateNewInstanceAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items.ContainsKey(HttpContextItemKey))
        {
            throw new InvalidOperationException("A journey instance has already been created for this request.");
        }

        if (!TryGetJourneyName(httpContext, out var journeyName))
        {
            return null;
        }

        if (!EndpointStartsJourney(httpContext))
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

        var coordinatorFactory = journeyRegistry.GetCoordinatorActivator(journey);
        var coordinatorContext = new CoordinatorContext
        {
            InstanceId = instanceId,
            Journey = journey,
            JourneyStateStorage = journeyStateStorage,
            HttpContext = httpContext
        };
        var coordinator = coordinatorFactory(httpContext.RequestServices, coordinatorContext);

        var state = await coordinator.GetStartingStateSafeAsync();
        Debug.Assert(state.GetType() == journey.StateType);
        journeyStateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state, Path = path });

        httpContext.Items[HttpContextItemKey] = coordinator;

        return coordinator;
    }

    public bool TryGetJourneyName(HttpContext httpContext, [NotNullWhen(true)] out string? journeyName)
    {
        journeyName = null;

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return false;
        }

        if (endpoint.Metadata.GetMetadata<JourneyNameMetadata>() is { } journeyNameMetadata)
        {
            journeyName = journeyNameMetadata.JourneyName;
            return true;
        }

        return false;
    }

    private static bool EndpointStartsJourney(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return false;
        }

        return endpoint.Metadata.GetMetadata<StartsJourneyMetadata>() is not null;
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
}
