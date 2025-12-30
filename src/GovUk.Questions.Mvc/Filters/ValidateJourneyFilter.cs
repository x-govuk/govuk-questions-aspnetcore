using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace GovUk.Questions.Mvc.Filters;

// ReSharper disable once ClassNeverInstantiated.Global
internal class ValidateJourneyFilter(JourneyInstanceProvider instanceProvider, LinkGenerator linkGenerator) : IAsyncResourceFilter
{
    public static int Order => -100;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (instanceProvider.TryGetJourneyName(httpContext, out _) && instanceProvider.GetJourneyInstance(httpContext) is null)
        {
            if (await instanceProvider.TryCreateNewInstanceAsync(httpContext) is JourneyCoordinator coordinator)
            {
                // Issue a redirect back to the same action that includes the new instance's Key in the query string
                var allRouteValues = context.RouteData.Values;
                allRouteValues.Add(JourneyInstanceId.KeyRouteValueName, coordinator.InstanceId.Key);
                var url = linkGenerator.GetPathByRouteValues(routeName: null, values: context.RouteData.Values)!;
                context.Result = new RedirectResult(url);
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
