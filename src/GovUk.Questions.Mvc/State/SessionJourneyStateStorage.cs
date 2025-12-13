using System.Text.Json;
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
    public void DeleteState(JourneyInstanceId instanceId)
    {
        ArgumentNullException.ThrowIfNull(instanceId);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);
        httpContext.Session.Remove(key);
    }

    /// <inheritdoc cref="IJourneyStateStorage.GetState"/>
    public StateStorageEntry? GetState(JourneyInstanceId instanceId)
    {
        ArgumentNullException.ThrowIfNull(instanceId);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);

        if (!httpContext.Session.TryGetValue(key, out var data))
        {
            return null;
        }

        // TODO Should we catch errors here and return null if deserialization fails?
        var stateEntry = JsonSerializer.Deserialize<StateStorageEntry>(data, options.Value.StateSerializerOptions);
        return stateEntry;
    }

    /// <inheritdoc cref="IJourneyStateStorage.SetState"/>
    public void SetState(JourneyInstanceId instanceId, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(stateEntry);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);
        var data = JsonSerializer.SerializeToUtf8Bytes(stateEntry, options.Value.StateSerializerOptions);
        httpContext.Session.Set(key, data);
    }

    private static string GetSessionKey(JourneyInstanceId instanceId) =>
        $"_guq:{instanceId}";
}
