namespace GovUk.Questions.Mvc.State;

/// <summary>
/// Represents a per-user storage mechanism for journey state.
/// </summary>
public interface IJourneyStateStorage
{
    /// <summary>
    /// Deletes the state for the specified journey instance.
    /// </summary>
    /// <param name="instanceId">The journey instance.</param>
    void DeleteState(JourneyInstanceId instanceId);

    /// <summary>
    /// Gets the state for the specified journey instance.
    /// </summary>
    /// <param name="instanceId">The journey instance.</param>
    /// <returns>The state entry if it exists; otherwise, <see langword="null"/>.</returns>
    StateStorageEntry? GetState(JourneyInstanceId instanceId);

    /// <summary>
    /// Sets the state for the specified journey instance.
    /// </summary>
    /// <param name="instanceId">The journey instance.</param>
    /// <param name="stateEntry">The state entry.</param>
    void SetState(JourneyInstanceId instanceId, StateStorageEntry stateEntry);
}
