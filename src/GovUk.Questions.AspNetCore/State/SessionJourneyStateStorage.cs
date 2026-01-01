using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.AspNetCore.State;

/// <summary>
/// An implementation of <see cref="IJourneyStateStorage"/> that uses the ASP.NET Core session store.
/// </summary>
public class SessionJourneyStateStorage(IHttpContextAccessor httpContextAccessor, IOptions<GovUkQuestionsOptions> optionsAccessor) :
    JsonJourneyStateStorage(optionsAccessor)
{
    /// <inheritdoc cref="IJourneyStateStorage.DeleteState"/>
    public override void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);
        httpContext.Session.Remove(key);
    }

    /// <inheritdoc cref="IJourneyStateStorage.GetState"/>
    public override StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);

        if (!httpContext.Session.TryGetValue(key, out var data))
        {
            return null;
        }

        return DeserializeStateEntry(data);
    }

    /// <inheritdoc cref="IJourneyStateStorage.SetState"/>
    public override void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        var httpContext = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("No HttpContext.");

        var key = GetSessionKey(instanceId);
        var data = SerializeStateEntry(journey, stateEntry);
        httpContext.Session.Set(key, data);
    }

    private static string GetSessionKey(JourneyInstanceId instanceId) => $"_guq:{instanceId}";
}
