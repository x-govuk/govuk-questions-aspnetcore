namespace GovUk.Questions.Mvc.Description;

internal static class ActionDescriptorPropertiesKeys
{
    public static object JourneyName { get; } = typeof(JourneyNameMetadata);
    public static object StartsJourney { get; } = typeof(StartsJourneyMetadata);
}
