using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Http;

namespace GovUk.Questions.Mvc;

internal record CoordinatorContext
{
    public required JourneyInstanceId InstanceId { get; init; }
    public required JourneyDescriptor Journey { get; init; }
    public required IJourneyStateStorage JourneyStateStorage { get; init; }
    public required HttpContext HttpContext { get; init; }
}
