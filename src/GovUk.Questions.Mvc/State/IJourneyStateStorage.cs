using GovUk.Questions.Mvc.Description;

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
    /// <param name="journey">The journey descriptor.</param>
    void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey);

    /// <summary>
    /// Gets the state for the specified journey instance.
    /// </summary>
    /// <param name="instanceId">The journey instance.</param>
    /// <param name="journey">The journey descriptor.</param>
    /// <returns>The state entry if it exists; otherwise, <see langword="null"/>.</returns>
    StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey);

    /// <summary>
    /// Sets the state for the specified journey instance.
    /// </summary>
    /// <param name="instanceId">The journey instance.</param>
    /// <param name="journey">The journey descriptor.</param>
    /// <param name="stateEntry">The state entry.</param>
    void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry);
}
