namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Annotates a <see cref="JourneyCoordinator"/> with metadata about the journey it coordinates.
/// </summary>
/// <param name="name">The name of the journey.</param>
/// <param name="routeValueKeys">The route value keys bound to the journey instance.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
#pragma warning disable CA1019
public sealed class JourneyCoordinatorAttribute(string name, string[] routeValueKeys) : Attribute
#pragma warning restore CA1019
{
    /// <summary>
    /// Gets the name of the journey.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the route value keys to bound to the journey instance.
    /// </summary>
    public IReadOnlyCollection<string> RouteValueKeys { get; } = routeValueKeys.ToArray();
}
