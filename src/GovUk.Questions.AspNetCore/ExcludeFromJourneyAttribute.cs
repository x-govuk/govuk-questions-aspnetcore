using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Opts this action or page handler out of the journey specified by its controller or page.
/// </summary>
/// <remarks>
/// Use this on an endpoint within a controller marked with <see cref="JourneyAttribute"/> when that endpoint
/// should not require a journey instance — for example a confirmation page shown after the journey instance has
/// been deleted.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ExcludeFromJourneyAttribute : Attribute, IPageApplicationModelConvention, IControllerModelConvention, IActionModelConvention
{
    void IPageApplicationModelConvention.Apply(PageApplicationModel model)
    {
        model.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em => em.Excluded = true);
    }

    void IControllerModelConvention.Apply(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            foreach (var selector in action.Selectors)
            {
                selector.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em => em.Excluded = true);
            }
        }
    }

    void IActionModelConvention.Apply(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            selector.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em => em.Excluded = true);
        }
    }
}
