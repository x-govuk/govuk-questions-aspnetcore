using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.AspNetCore.Description;

internal class JourneyRegistry
{
    /// <summary>
    /// The collection of known journeys in the application, keyed by journey name.
    /// </summary>
    private readonly Dictionary<string, JourneyInfo> _journeys = new(JourneyDescriptor.JourneyNameComparer);

    public JourneyDescriptor? FindJourneyByCoordinatorType(Type coordinatorType)
    {
        ArgumentNullException.ThrowIfNull(coordinatorType);

        return _journeys.Values.SingleOrDefault(j => j.CoordinatorType == coordinatorType)?.Descriptor;
    }

    public JourneyDescriptor? FindJourneyByName(string journeyName)
    {
        ArgumentNullException.ThrowIfNull(journeyName);

        return _journeys.TryGetValue(journeyName, out var journeyInfo)
            ? journeyInfo.Descriptor
            : null;
    }

    public IReadOnlyCollection<Type> GetAllCoordinatorFactoryTypes() => _journeys.Values.Select(v => v.CoordinatorType).ToList().AsReadOnly();

    public Func<IServiceProvider, CoordinatorContext, JourneyCoordinator> GetCoordinatorActivator(JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(journey);

        return _journeys.TryGetValue(journey.JourneyName, out var journeyInfo)
            ? journeyInfo.CoordinatorActivator
            : throw new ArgumentException($"No journey with the name '{journey.JourneyName}' is registered.", nameof(journey));
    }

    public void RegisterJourney(Type coordinatorType, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(coordinatorType);
        ArgumentNullException.ThrowIfNull(journey);

        if (_journeys.ContainsKey(journey.JourneyName))
        {
            throw new ArgumentException($"A journey with the name '{journey.JourneyName}' has already been registered.", nameof(journey));
        }

        var coordinatorObjectFactory = ActivatorUtilities.CreateFactory(coordinatorType, []);

        var journeyInfo = new JourneyInfo(journey, coordinatorType, ActivateCoordinator);
        _journeys.Add(journey.JourneyName, journeyInfo);

        JourneyCoordinator ActivateCoordinator(IServiceProvider serviceProvider, CoordinatorContext context)
        {
            var coordinator = (JourneyCoordinator)coordinatorObjectFactory(serviceProvider, []);
            coordinator.Context = context;
            return coordinator;
        }
    }

    private record JourneyInfo(
        JourneyDescriptor Descriptor,
        Type CoordinatorType,
        Func<IServiceProvider, CoordinatorContext, JourneyCoordinator> CoordinatorActivator);
}
