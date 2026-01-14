using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GovUk.Questions.AspNetCore;

// ReSharper disable once ClassNeverInstantiated.Global
internal class ValidateJourneyFilter(IJourneyInstanceProvider instanceProvider) : IAsyncResourceFilter
{
    public static int Order => -100;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (instanceProvider.GetJourneyInfo(httpContext) is not { } journeyInfo)
        {
            // Endpoint is not part of a journey
            await next();
            return;
        }

        if (instanceProvider.GetJourneyInstance(httpContext) is { } coordinator)
        {
            var currentStep = coordinator.GetCurrentStep();

            if (currentStep is null || !coordinator.StepIsValid(currentStep))
            {
                context.Result = coordinator.OnInvalidStep();
                return;
            }
        }
        else if (await instanceProvider.TryCreateNewInstanceAsync(httpContext) is JourneyCoordinator newInstanceCoordinator)
        {
            context.Result = new RedirectResult(newInstanceCoordinator.Path.Steps.First().Url);
            return;
        }
        else if (!journeyInfo.Optional)
        {
            // Unable to get a journey instance
            // TODO Make this configurable
            context.Result = new BadRequestResult();
            return;
        }

        await next();
    }
}
