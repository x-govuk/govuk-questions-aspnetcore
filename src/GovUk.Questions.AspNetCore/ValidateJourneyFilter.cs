using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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

        if (endpointJourneyMetadata is null || endpointJourneyMetadata.Excluded)
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
            // N.B. We can't use Path.Steps.First().GetUrl() here since the step's URL has been normalized;
            // redirecting to the requested URL keeps any query parameters that normalization removes (e.g. returnUrl).
            var requestUrl = JourneyCoordinator.GetUrlWithoutQueryParameters(
                httpContext.Request.GetEncodedPathAndQuery(),
                JourneyInstanceId.KeyRouteValueName);

            context.Result = new RedirectResult(newInstanceCoordinator.InstanceId.EnsureUrlHasKey(requestUrl));
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
