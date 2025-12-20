using System.Text.Json;
using GovUk.Questions.Mvc.Description;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.Mvc.State;

/// <summary>
/// An implementation of <see cref="IJourneyStateStorage"/> that uses the ASP.NET Core session store.
/// </summary>
public class SessionJourneyStateStorage(IHttpContextAccessor httpContextAccessor, IOptions<GovUkQuestionsOptions> options) :
    IJourneyStateStorage
{
    /// <inheritdoc cref="IJourneyStateStorage.DeleteState"/>
    public void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);
        httpContext.Session.Remove(key);
    }

    /// <inheritdoc cref="IJourneyStateStorage.GetState"/>
    public StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);

        if (!httpContext.Session.TryGetValue(key, out var data))
        {
            return null;
        }

        var wrapper = JsonSerializer.Deserialize<SerializableStateEntry>(data);
        if (wrapper is null)
        {
            return null;
        }

        var stateType = Type.GetType(wrapper.StateTypeName) ??
            throw new InvalidOperationException($"Could not load type '{wrapper.StateTypeName}' from assembly.");
        var state = wrapper.State.Deserialize(stateType, options.Value.StateSerializerOptions);

        return state is not null ? new StateStorageEntry { State = state } : null;
    }

    /// <inheritdoc cref="IJourneyStateStorage.SetState"/>
    public void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);
        var wrapper = new SerializableStateEntry(
            journey.StateType.AssemblyQualifiedName!,
            JsonSerializer.SerializeToElement(stateEntry.State, options.Value.StateSerializerOptions));
        var data = JsonSerializer.SerializeToUtf8Bytes(wrapper);
        httpContext.Session.Set(key, data);
    }

    private static string GetSessionKey(JourneyInstanceId instanceId) =>
        $"_guq:{instanceId}";

    private record SerializableStateEntry(string StateTypeName, JsonElement State);
}
