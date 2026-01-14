using Microsoft.AspNetCore.Http;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Provides journey instances for the current request.
/// </summary>
public interface IJourneyInstanceProvider
{
    /// <summary>
    /// Returns the information about the journey associated with the specified <see cref="HttpContext"/>, if any.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="RequestJourneyInfo"/> instance if the request has an associated journey; otherwise <see langword="null"/>.</returns>
    RequestJourneyInfo? GetJourneyInfo(HttpContext httpContext);

    /// <summary>
    /// Gets the current journey instance for the specified <see cref="HttpContext"/>, if one exists.
    /// </summary>
    /// <param name="httpContext">The current <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="JourneyCoordinator{TState}"/> for the instance, if one exists; otherwise <see langword="null"/>.</returns>
    JourneyCoordinator? GetJourneyInstance(HttpContext httpContext);

    /// <summary>
    /// Tries to create a new journey instance for the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The current <see cref="HttpContext"/>.</param>
    /// <returns>>The newly created <see cref="JourneyCoordinator{TState}"/>, if one could be created; otherwise <see langword="null"/>.</returns>
    Task<JourneyCoordinator?> TryCreateNewInstanceAsync(HttpContext httpContext);

    /// <summary>
    /// Tries to create a new journey instance for the specified <see cref="HttpContext"/>
    /// with initial state provided by the specified delegate.
    /// </summary>
    /// <param name="httpContext">The current <see cref="HttpContext"/>.</param>
    /// <param name="createInitialState">A delegate to create the initial state for the journey instance.</param>
    /// <returns>>The newly created <see cref="JourneyCoordinator{TState}"/>, if one could be created; otherwise <see langword="null"/>.</returns>
    Task<JourneyCoordinator?> TryCreateNewInstanceAsync(HttpContext httpContext, Func<CreateNewInstanceStateContext, Task<object>> createInitialState);
}

/// <summary>
/// Information about the journey associated with a request.
/// </summary>
/// <param name="JourneyName">The journey name.</param>
/// <param name="Optional">Whether the journey is optional.</param>
public sealed record RequestJourneyInfo(string JourneyName, bool Optional);
