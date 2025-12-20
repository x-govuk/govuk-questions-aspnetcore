using GovUk.Questions.Mvc.Description;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GovUk.Questions.Mvc;

/// <summary>
/// Specifies the name of the journey associated with this controller, action or page handler.
/// </summary>
/// <param name="name">The journey name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class JourneyNameAttribute(string name) : Attribute, IPageApplicationModelConvention, IControllerModelConvention, IActionModelConvention
{
    /// <summary>
    /// The name of the journey.
    /// </summary>
    public string Name { get; } = name;

    void IPageApplicationModelConvention.Apply(PageApplicationModel model)
    {
        model.Properties[ActionDescriptorPropertiesKeys.JourneyName] = new JourneyNameMetadata(Name);
    }

    void IControllerModelConvention.Apply(ControllerModel controller)
    {
        controller.Properties[ActionDescriptorPropertiesKeys.JourneyName] = new JourneyNameMetadata(Name);
    }

    void IActionModelConvention.Apply(ActionModel action)
    {
        action.Properties[ActionDescriptorPropertiesKeys.JourneyName] = new JourneyNameMetadata(Name);
    }
}
