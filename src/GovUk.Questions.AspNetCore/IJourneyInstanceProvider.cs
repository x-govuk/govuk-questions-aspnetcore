using Microsoft.AspNetCore.Http;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Provides journey instances for the current request.
/// </summary>
public interface IJourneyInstanceProvider
{
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
