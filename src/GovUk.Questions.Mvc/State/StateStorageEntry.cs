namespace GovUk.Questions.Mvc.State;

/// <summary>
/// An entry in journey state storage.
/// </summary>
public sealed record StateStorageEntry
{
    /// <summary>
    /// The journey instance state.
    /// </summary>
    public required object State { get; init; }
}
