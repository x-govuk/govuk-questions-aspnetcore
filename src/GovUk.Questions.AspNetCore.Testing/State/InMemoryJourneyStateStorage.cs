using System.Collections.Concurrent;
using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.AspNetCore.Testing.State;

/// <summary>
/// An in-memory implementation of <see cref="IJourneyStateStorage"/> for testing purposes.
/// </summary>
public class InMemoryJourneyStateStorage(IOptions<GovUkQuestionsOptions> optionsAccessor) : JsonJourneyStateStorage(optionsAccessor)
{
    private readonly ConcurrentDictionary<(JourneyInstanceId, JourneyDescriptor), byte[]> _storageEntries = new();

    /// <inheritdoc/>
    public override void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        _storageEntries.Remove((instanceId, journey), out _);
    }

    /// <inheritdoc/>
    public override StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        if (!_storageEntries.TryGetValue((instanceId, journey), out var data))
        {
            return null;
        }

        return DeserializeStateEntry(data);
    }

    /// <inheritdoc/>
    public override void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        var data = SerializeStateEntry(journey, stateEntry);
        _storageEntries[(instanceId, journey)] = data;
    }
}
