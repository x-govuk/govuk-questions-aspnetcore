namespace GovUk.Questions.Mvc.Description;

/// <summary>
/// Describes a journey.
/// </summary>
/// <param name="JourneyName">The name of the journey.</param>
/// <param name="RouteValueKeys">The route value keys to bind to instances of this journey.</param>
/// <param name="StateType">The state type for instances of this journey.</param>
public sealed record JourneyDescriptor(string JourneyName, IReadOnlyCollection<string> RouteValueKeys, Type StateType)
{
    internal static StringComparer JourneyNameComparer { get; } = StringComparer.OrdinalIgnoreCase;
}
