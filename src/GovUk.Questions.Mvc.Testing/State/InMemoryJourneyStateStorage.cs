using System.Collections.Concurrent;
using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;

namespace GovUk.Questions.Mvc.Testing.State;

/// <summary>
/// An in-memory implementation of <see cref="IJourneyStateStorage"/> for testing purposes.
/// </summary>
public class InMemoryJourneyStateStorage : IJourneyStateStorage
{
    private readonly ConcurrentDictionary<(JourneyInstanceId, JourneyDescriptor), StateStorageEntry> _storageEntries = new();

    /// <inheritdoc/>
    public void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        _storageEntries.Remove((instanceId, journey), out _);
    }

    /// <inheritdoc/>
    public StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        return _storageEntries.GetValueOrDefault((instanceId, journey));
    }

    /// <inheritdoc/>
    public void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        _storageEntries[(instanceId, journey)] = stateEntry;
    }
}
