using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace GovUk.Questions.Mvc.Description;

internal class JourneyRegistryProvider
{
    public JourneyRegistry CreateRegistry(ApplicationPartManager partManager)
    {
        ArgumentNullException.ThrowIfNull(partManager);

        var parts = partManager.ApplicationParts;
        var journeyRegistry = new JourneyRegistry();

        foreach (var part in parts.OfType<IApplicationPartTypeProvider>())
        {
            foreach (var type in part.Types)
            {
                if (IsJourneyCoordinator(type.AsType(), out var journey))
                {
                    journeyRegistry.RegisterJourney(type, journey);
                }
            }
        }

        return journeyRegistry;
    }

    private bool IsJourneyCoordinator(
        Type type,
        [NotNullWhen(true)] out JourneyDescriptor? journey)
    {
        Type stateType = null!;
        journey = null;

        if (type.GetCustomAttribute<JourneyAttribute>() is not { } journeyAttribute)
        {
            return false;
        }

        if (!type.IsClass)
        {
            return false;
        }

        if (type.IsAbstract)
        {
            return false;
        }

        if (!type.IsPublic)
        {
            return false;
        }

        if (type.ContainsGenericParameters)
        {
            return false;
        }

        var baseType = type.GetTypeInfo().BaseType;
        while (baseType is not null)
        {
            if (baseType.GetTypeInfo().IsGenericType &&
                baseType.GetGenericTypeDefinition() == typeof(JourneyCoordinator<>))
            {
                stateType = baseType.GetGenericArguments()[0];
                break;
            }

            baseType = baseType.GetTypeInfo().BaseType;
        }

        if (baseType is null)
        {
            return false;
        }

        journey = new JourneyDescriptor(
            journeyAttribute.Name,
            journeyAttribute.RouteValueKeys.ToArray(),
            stateType);

        return true;
    }
}
