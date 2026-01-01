namespace GovUk.Questions.AspNetCore.Description;

internal sealed record StartsJourneyMetadata
{
    private StartsJourneyMetadata()
    {
    }

    public static StartsJourneyMetadata Instance { get; } = new();
}
