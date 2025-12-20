using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.Mvc.ModelBinding;

internal class JourneyCoordinatorModelBinder(
    JourneyDescriptor journeyDescriptor,
    IJourneyStateStorage journeyStateStorage,
    IOptions<GovUkQuestionsOptions> optionsAccessor) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProvider = await CreateValueProviderAsync(bindingContext);

        var routeValues = new RouteValueDictionary();
        foreach (var key in journeyDescriptor.RouteValueKeys.Append(JourneyInstanceId.KeyRouteValueName))
        {
            var valueProviderResult = valueProvider.GetValue(key);

            if (valueProviderResult.Length == 0)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            routeValues.Add(key, valueProviderResult.Values);
        }

        if (!JourneyInstanceId.TryCreate(journeyDescriptor, routeValues, out var instanceId))
        {
            // TODO Add ModelState error?
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var stateEntry = journeyStateStorage.GetState(instanceId);
        if (stateEntry is null)
        {
            // TODO Add ModelState error?
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var coordinatorFactory = optionsAccessor.Value.Journeys.GetCoordinatorFactory(journeyDescriptor);
        var coordinator = coordinatorFactory(bindingContext.HttpContext.RequestServices);
        coordinator.InstanceId = instanceId;
        coordinator.Journey = journeyDescriptor;
        coordinator.StateStorage = journeyStateStorage;

        bindingContext.Result = ModelBindingResult.Success(coordinator);
    }

    private async Task<CompositeValueProvider> CreateValueProviderAsync(ModelBindingContext bindingContext)
    {
        var valueProviderFactoryContext = new ValueProviderFactoryContext(bindingContext.ActionContext);

        foreach (var valueProviderFactory in optionsAccessor.Value.ValueProviderFactories)
        {
            await valueProviderFactory.CreateValueProviderAsync(valueProviderFactoryContext);
        }

        return new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);
    }
}
