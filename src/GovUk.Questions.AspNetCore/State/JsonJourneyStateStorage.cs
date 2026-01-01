using System.Text.Json;
using GovUk.Questions.AspNetCore.Description;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.AspNetCore.State;

/// <summary>
/// An base class for implementations of <see cref="IJourneyStateStorage"/> that use JSON serialization.
/// </summary>
public abstract class JsonJourneyStateStorage(IOptions<GovUkQuestionsOptions> optionsAccessor) : IJourneyStateStorage
{
    /// <inheritdoc/>
    public abstract void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey);

    /// <inheritdoc/>
    public abstract StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey);

    /// <inheritdoc/>
    public abstract void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry);

    /// <summary>
    /// Serializes a <see cref="StateStorageEntry"/> to a byte array using JSON.
    /// </summary>
    protected byte[] SerializeStateEntry(JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        var wrapper = new SerializableStateEntry(
            journey.StateType.AssemblyQualifiedName!,
            JsonSerializer.SerializeToElement(stateEntry.State, optionsAccessor.Value.StateSerializerOptions),
            stateEntry.Path);

        return JsonSerializer.SerializeToUtf8Bytes(wrapper, optionsAccessor.Value.StateSerializerOptions);
    }

    /// <summary>
    /// Deserializes a <see cref="StateStorageEntry"/> from a byte array using JSON.
    /// </summary>
    protected StateStorageEntry DeserializeStateEntry(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var wrapper = JsonSerializer.Deserialize<SerializableStateEntry>(data, optionsAccessor.Value.StateSerializerOptions);
        if (wrapper is null)
        {
            throw new InvalidOperationException("Failed to deserialize state entry.");
        }

        var stateType = Type.GetType(wrapper.StateTypeName) ??
            throw new InvalidOperationException($"Could not load type '{wrapper.StateTypeName}' from assembly.");
        var state = wrapper.State.Deserialize(stateType, optionsAccessor.Value.StateSerializerOptions);

        if (state is null)
        {
            throw new InvalidOperationException("Failed to deserialize state.");
        }

        return new StateStorageEntry { State = state, Path = wrapper.Path };
    }

    private record SerializableStateEntry(string StateTypeName, JsonElement State, JourneyPath Path);
}
