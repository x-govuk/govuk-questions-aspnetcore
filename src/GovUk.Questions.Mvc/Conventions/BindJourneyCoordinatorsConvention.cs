using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GovUk.Questions.Mvc.Conventions;

internal class BindJourneyCoordinatorsConvention : IPageApplicationModelConvention, IControllerModelConvention, IActionModelConvention
{
    void IPageApplicationModelConvention.Apply(PageApplicationModel model)
    {
        var coordinatorProperties = model.HandlerProperties.Where(p => IsCoordinatorType(p.ParameterType));

        foreach (var property in coordinatorProperties)
        {
            property.BindingInfo ??= new BindingInfo();
        }
    }

    void IControllerModelConvention.Apply(ControllerModel controller)
    {
        var coordinatorProperties = controller.ControllerProperties.Where(p => IsCoordinatorType(p.ParameterType));

        foreach (var property in coordinatorProperties)
        {
            property.BindingInfo ??= new BindingInfo();
        }
    }

    void IActionModelConvention.Apply(ActionModel action)
    {
        var coordinatorProperties = action.Parameters.Where(p => IsCoordinatorType(p.ParameterType));

        foreach (var property in coordinatorProperties)
        {
            property.BindingInfo ??= new BindingInfo();
        }
    }

    private static bool IsCoordinatorType(Type type) => typeof(JourneyCoordinator).IsAssignableFrom(type);
}
