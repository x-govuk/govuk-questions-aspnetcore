using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// An <see cref="IResult"/> that redirects the user to a specified URL.
/// </summary>
public sealed class AdvanceToResult : IResult, IActionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdvanceToResult"/> class.
    /// </summary>
    public AdvanceToResult(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        Url = url;
    }

    /// <summary>
    /// Gets the URL to redirect to.
    /// </summary>
    public string Url { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext) => Results.Redirect(Url).ExecuteAsync(httpContext);

    Task IActionResult.ExecuteResultAsync(ActionContext context) => ExecuteAsync(context.HttpContext);
}
