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
        model.EndpointMetadata.Add(new JourneyNameMetadata(Name));
    }

    void IControllerModelConvention.Apply(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            foreach (var selector in action.Selectors)
            {
                if (selector.EndpointMetadata.OfType<JourneyNameMetadata>().Any())
                {
                    // An action-level JourneyNameAttribute takes precedence over a controller-level one.
                    continue;
                }

                selector.EndpointMetadata.Add(new JourneyNameMetadata(Name));
            }
        }
    }

    void IActionModelConvention.Apply(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            selector.EndpointMetadata.Add(new JourneyNameMetadata(Name));
        }
    }
}
