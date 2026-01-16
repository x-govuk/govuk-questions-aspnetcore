using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Http;
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

        var endpointJourneyMetadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<EndpointJourneyMetadata>();

        if (endpointJourneyMetadata is null)
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
                context.Result = new HttpResultWrapper(coordinator.OnInvalidStep());
                return;
            }
        }
        else if (await instanceProvider.TryCreateNewInstanceAsync(httpContext) is JourneyCoordinator newInstanceCoordinator)
        {
            context.Result = new RedirectResult(newInstanceCoordinator.Path.Steps.First().GetUrl(newInstanceCoordinator.InstanceId));
            return;
        }
        else if (!endpointJourneyMetadata.Optional)
        {
            // Unable to get a journey instance
            // TODO Make this configurable
            context.Result = new BadRequestResult();
            return;
        }

        await next();
    }

    private class HttpResultWrapper(IResult result) : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context) => result.ExecuteAsync(context.HttpContext);
    }
}
