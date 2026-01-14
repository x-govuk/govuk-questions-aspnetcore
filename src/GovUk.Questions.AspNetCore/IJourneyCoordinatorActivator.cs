namespace GovUk.Questions.AspNetCore;

internal interface IJourneyCoordinatorActivator
{
    JourneyCoordinator CreateCoordinator(JourneyCoordinatorContext context);
}
