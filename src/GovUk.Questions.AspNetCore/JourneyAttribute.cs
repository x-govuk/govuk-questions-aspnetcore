using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Specifies the journey associated with this controller, action or page handler.
/// </summary>
/// <param name="journeyName">The journey name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class JourneyAttribute(string journeyName) : Attribute, IPageApplicationModelConvention, IControllerModelConvention, IActionModelConvention
{
    /// <summary>
    /// The name of the journey.
    /// </summary>
    public string JourneyName { get; } = journeyName;

    /// <summary>
    /// Gets or sets a value indicating whether the journey is optional.
    /// </summary>
    /// <remarks>
    /// If the journey is optional then requests that do not have an associated journey instance will still be allowed to proceed.
    /// </remarks>
    public bool Optional { get; set; }

    void IPageApplicationModelConvention.Apply(PageApplicationModel model)
    {
        model.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em =>
        {
            em.JourneyName = JourneyName;
            em.Optional = Optional;
        });
    }

    void IControllerModelConvention.Apply(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            foreach (var selector in action.Selectors)
            {
                if (selector.EndpointMetadata.OfType<EndpointJourneyMetadata>().Any())
                {
                    // An action-level JourneyNameAttribute takes precedence over a controller-level one.
                    continue;
                }

                selector.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em =>
                {
                    em.JourneyName = JourneyName;
                    em.Optional = Optional;
                });
            }
        }
    }

    void IActionModelConvention.Apply(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            selector.EndpointMetadata.CreateOrUpdateEndpointJourneyMetadata(em =>
            {
                em.JourneyName = JourneyName;
                em.Optional = Optional;
            });
        }
    }
}
