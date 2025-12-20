namespace GovUk.Questions.Mvc.Description;

internal sealed record StartsJourneyMetadata
{
    private StartsJourneyMetadata()
    {
    }

    public static StartsJourneyAttribute Instance { get; } = new();
}
