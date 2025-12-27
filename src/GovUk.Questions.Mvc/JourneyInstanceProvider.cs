using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.Mvc;

internal class JourneyInstanceProvider(IJourneyStateStorage journeyStateStorage, IOptions<GovUkQuestionsOptions> optionsAccessor)
{
    public async Task<JourneyCoordinator?> GetJourneyInstanceAsync(ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        if (!TryGetJourneyName(actionContext, out var journeyName))
        {
            return null;
        }

        var journey = optionsAccessor.Value.Journeys.FindJourneyByName(journeyName);
        if (journey is null)
        {
            throw new InvalidOperationException($"No journey found with name '{journeyName}'.");
        }

        var valueProvider = await CreateValueProviderAsync(actionContext);
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

        var coordinatorFactory = optionsAccessor.Value.Journeys.GetCoordinatorFactory(journey);
        var coordinator = coordinatorFactory(actionContext.HttpContext.RequestServices);
        coordinator.InstanceId = instanceId;
        coordinator.Journey = journey;
        coordinator.StateStorage = journeyStateStorage;

        return coordinator;
    }

    public async Task<JourneyCoordinator?> TryCreateNewInstanceAsync(ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        if (!TryGetJourneyName(actionContext, out var journeyName))
        {
            return null;
        }

        if (!ActionStartsJourney(actionContext))
        {
            return null;
        }

        var journey = optionsAccessor.Value.Journeys.FindJourneyByName(journeyName);
        if (journey is null)
        {
            throw new InvalidOperationException($"No journey found with name '{journeyName}'.");
        }

        var valueProvider = await CreateValueProviderAsync(actionContext);
        var routeValues = GetRouteValues(journey, valueProvider);

        if (!JourneyInstanceId.TryCreateNew(journey, routeValues, out var instanceId))
        {
            return null;
        }

        var coordinatorFactory = optionsAccessor.Value.Journeys.GetCoordinatorFactory(journey);
        var coordinator = coordinatorFactory(actionContext.HttpContext.RequestServices);
        coordinator.InstanceId = instanceId;
        coordinator.Journey = journey;
        // Don't assign StateStorage yet; we don't want GetStartingState*() implementations to be manipulating state

        var state = await coordinator.GetStartingStateSafeAsync(new GetStartingStateContext(actionContext));
        Debug.Assert(state.GetType() == journey.StateType);
        journeyStateStorage.SetState(instanceId, journey, new StateStorageEntry { State = state });
        coordinator.StateStorage = journeyStateStorage;

        return coordinator;
    }

    public bool TryGetJourneyName(ActionContext actionContext, [NotNullWhen(true)] out string? journeyName)
    {
        if (actionContext.ActionDescriptor.Properties.TryGetValue(
                ActionDescriptorPropertiesKeys.JourneyName,
                out var journeyNameMetadataObj) &&
            journeyNameMetadataObj is JourneyNameMetadata journeyNameMetadata)
        {
            journeyName = journeyNameMetadata.JourneyName;
            return true;
        }
        else
        {
            journeyName = null;
            return false;
        }
    }

    private static bool ActionStartsJourney(ActionContext actionContext)
    {
        return actionContext.ActionDescriptor.Properties.TryGetValue(
               ActionDescriptorPropertiesKeys.StartsJourney,
               out var startsJourneyMetadataObj) &&
           startsJourneyMetadataObj is StartsJourneyMetadata;
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

    private async Task<CompositeValueProvider> CreateValueProviderAsync(ActionContext actionContext)
    {
        var valueProviderFactoryContext = new ValueProviderFactoryContext(actionContext);

        foreach (var valueProviderFactory in optionsAccessor.Value.ValueProviderFactories)
        {
            await valueProviderFactory.CreateValueProviderAsync(valueProviderFactoryContext);
        }

        return new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);
    }
}
