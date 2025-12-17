namespace GovUk.Questions.Mvc.Description;

internal sealed class StartsJourneyMetadata
{
    private StartsJourneyMetadata()
    {
    }

    public static StartsJourneyAttribute Instance { get; } = new();
}
