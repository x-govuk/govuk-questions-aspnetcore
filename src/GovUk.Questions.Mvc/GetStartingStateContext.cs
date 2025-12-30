using Microsoft.AspNetCore.Http;

namespace GovUk.Questions.Mvc;

/// <summary>
/// A context that contains information for getting the initial state for a newly-started journey instance.
/// </summary>
public class GetStartingStateContext(HttpContext httpContext)
{
    /// <summary>
    /// Gets the <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext HttpContext { get; } = httpContext;
}
