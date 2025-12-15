using Microsoft.AspNetCore.Mvc;

namespace GovUk.Questions.Mvc;

/// <summary>
/// A context that contains information for getting the initial state for a newly-started journey instance.
/// </summary>
public class GetStartingStateContext(ActionContext actionContext)
{
    /// <summary>
    /// Gets the <see cref="ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; } = actionContext;
}
