using GovUk.Questions.Mvc.Description;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.Mvc.ModelBinding;

internal class JourneyCoordinatorModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var journeyInfoRegistry = context.Services.GetRequiredService<JourneyInfoRegistry>();

        var modelType = context.Metadata.ModelType;

        if (JourneyCoordinator.IsActivatableJourneyCoordinator(modelType) &&
            journeyInfoRegistry.FindJourneyByCoordinatorType(modelType) is JourneyDescriptor journey)
        {
            return ActivatorUtilities.CreateInstance<JourneyCoordinatorModelBinder>(context.Services, journey);
        }

        return null;
    }
}
