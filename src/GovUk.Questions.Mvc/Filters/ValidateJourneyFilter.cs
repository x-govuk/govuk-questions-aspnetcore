using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GovUk.Questions.Mvc.Filters;

// ReSharper disable once ClassNeverInstantiated.Global
internal class ValidateJourneyFilter(JourneyInstanceProvider instanceProvider) : IAsyncResourceFilter
{
    public static int Order => -100;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (instanceProvider.TryGetJourneyName(httpContext, out _) && instanceProvider.GetJourneyInstance(httpContext) is null)
        {
            if (await instanceProvider.TryCreateNewInstanceAsync(httpContext) is JourneyCoordinator coordinator)
            {
                context.Result = new RedirectResult(coordinator.Path.Steps.First().Url);
            }
            else
            {
                // TODO Make this configurable
                context.Result = new BadRequestResult();
            }

            return;
        }

        await next();
    }
}
