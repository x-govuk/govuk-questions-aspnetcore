using GovUk.Questions.Mvc.Description;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GovUk.Questions.Mvc;

/// <summary>
/// Indicates that this action or page handler starts a new journey instance.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class StartsJourneyAttribute : Attribute, IActionModelConvention, IPageHandlerModelConvention
{
    void IActionModelConvention.Apply(ActionModel action)
    {
        action.Properties[ActionDescriptorPropertiesKeys.StartsJourney] = StartsJourneyMetadata.Instance;
    }

    void IPageHandlerModelConvention.Apply(PageHandlerModel model)
    {
        model.Properties[ActionDescriptorPropertiesKeys.StartsJourney] = StartsJourneyMetadata.Instance;
    }
}
