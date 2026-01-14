using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;

namespace GovUk.Questions.AspNetCore;

internal record JourneyCoordinatorContext
{
    public required JourneyInstanceId InstanceId { get; init; }
    public required JourneyDescriptor Journey { get; init; }
    public required IJourneyStateStorage JourneyStateStorage { get; init; }
    public required HttpContext HttpContext { get; init; }
}
