using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.Mvc.Description;

internal class JourneyInfoRegistry
{
    /// <summary>
    /// The collection of known journeys in the application, keyed by journey name.
    /// </summary>
    private readonly Dictionary<string, JourneyInfo> _journeys = new(JourneyDescriptor.JourneyNameComparer);

    public JourneyDescriptor? FindJourneyByCoordinatorType(Type coordinatorType)
    {
        ArgumentNullException.ThrowIfNull(coordinatorType);

        return _journeys.Values
            .FirstOrDefault(journeyInfo => journeyInfo.CoordinatorType == coordinatorType)
            ?.Descriptor;
    }

    public JourneyDescriptor? FindJourneyByName(string journeyName)
    {
        ArgumentNullException.ThrowIfNull(journeyName);

        return _journeys.TryGetValue(journeyName, out var journeyInfo)
            ? journeyInfo.Descriptor
            : null;
    }

    public void RegisterJourney(Type coordinatorType, JourneyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(coordinatorType);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_journeys.ContainsKey(descriptor.JourneyName))
        {
            throw new ArgumentException($"A journey with the name '{descriptor.JourneyName}' has already been registered.", nameof(descriptor));
        }

        var coordinatorFactory = ActivatorUtilities.CreateFactory(coordinatorType, []);

        _journeys.Add(descriptor.JourneyName, new JourneyInfo(descriptor, coordinatorType, coordinatorFactory));
    }

    private record JourneyInfo(JourneyDescriptor Descriptor, Type CoordinatorType, ObjectFactory CoordinatorFactory);
}
