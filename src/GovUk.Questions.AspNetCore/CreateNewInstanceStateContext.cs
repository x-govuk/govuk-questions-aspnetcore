using Microsoft.AspNetCore.Http;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Contains information about the new journey instance being created.
/// </summary>
public record CreateNewInstanceStateContext(JourneyInstanceId InstanceId, HttpContext HttpContext);
