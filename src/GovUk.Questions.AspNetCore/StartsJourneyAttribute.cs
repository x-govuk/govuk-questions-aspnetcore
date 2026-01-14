using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Indicates that this action or page handler starts a new journey instance.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class StartsJourneyAttribute : Attribute, IActionModelConvention, IPageApplicationModelConvention
{
    void IActionModelConvention.Apply(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            selector.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em =>
            {
                em.StartsJourney = true;
            });
        }
    }

    void IPageApplicationModelConvention.Apply(PageApplicationModel model)
    {
        model.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em =>
        {
            em.StartsJourney = true;
        });
    }
}
