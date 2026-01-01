using System.Collections.Concurrent;
using System.Text.Json;
using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.AspNetCore.Testing.State;

/// <summary>
/// An in-memory implementation of <see cref="IJourneyStateStorage"/> for testing purposes.
/// </summary>
public class InMemoryJourneyStateStorage(IOptions<GovUkQuestionsOptions> optionsAccessor) : IJourneyStateStorage
{
    private readonly ConcurrentDictionary<(JourneyInstanceId, JourneyDescriptor), SerializableStateEntry> _storageEntries = new();

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

        if (!_storageEntries.TryGetValue((instanceId, journey), out var wrapper))
        {
            return null;
        }

        var stateType = Type.GetType(wrapper.StateTypeName) ??
            throw new InvalidOperationException($"Could not load type '{wrapper.StateTypeName}' from assembly.");
        var state = wrapper.State.Deserialize(stateType, optionsAccessor.Value.StateSerializerOptions);

        return state is not null ? new StateStorageEntry { State = state, Path = wrapper.Path } : null;
    }

    /// <inheritdoc/>
    public void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        var wrapper = new SerializableStateEntry(
            journey.StateType.AssemblyQualifiedName!,
            JsonSerializer.SerializeToElement(stateEntry.State, optionsAccessor.Value.StateSerializerOptions),
            stateEntry.Path);

        _storageEntries[(instanceId, journey)] = wrapper;
    }

    private record SerializableStateEntry(string StateTypeName, JsonElement State, JourneyPath Path);
}
