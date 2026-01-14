using GovUk.Questions.AspNetCore.Description;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.AspNetCore;

internal class DefaultJourneyCoordinatorActivator(JourneyRegistry journeyRegistry) : IJourneyCoordinatorActivator
{
    public JourneyCoordinator CreateCoordinator(JourneyCoordinatorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var coordinatorType = journeyRegistry.GetCoordinatorType(context.Journey);

        var coordinator = (JourneyCoordinator)ActivatorUtilities.CreateInstance(context.HttpContext.RequestServices, coordinatorType);
        coordinator.Context = context;

        return coordinator;
    }
}
