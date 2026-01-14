using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GovUk.Questions.AspNetCore;

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

    /// <summary>
    /// Gets or sets a value indicating whether the journey is optional.
    /// </summary>
    /// <remarks>
    /// If the journey is optional then requests that do not have an associated journey instance will still be allowed to proceed.
    /// </remarks>
    public bool Optional { get; set; }

    void IPageApplicationModelConvention.Apply(PageApplicationModel model)
    {
        model.EndpointMetadata.Add(new JourneyNameMetadata(Name, Optional));
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

                selector.EndpointMetadata.Add(new JourneyNameMetadata(Name, Optional));
            }
        }
    }

    void IActionModelConvention.Apply(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            selector.EndpointMetadata.Add(new JourneyNameMetadata(Name, Optional));
        }
    }
}
