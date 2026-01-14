namespace GovUk.Questions.AspNetCore.Description;

/// <summary>
/// Metadata describing the journey-related behavior of an endpoint.
/// </summary>
public sealed record EndpointJourneyMetadata
{
    /// <summary>
    /// The journey name associated with the endpoint.
    /// </summary>
    public string? JourneyName { get; set; }

    /// <summary>
    /// Indicates whether the endpoint starts a new journey instance.
    /// </summary>
    public bool StartsJourney { get; set; }

    /// <summary>
    /// Indicates whether a journey instance is optional for accessing the endpoint.
    /// </summary>
    public bool Optional { get; set; }
}

internal static class EndpointMetadataExtensions
{
    public static void CreateOrUpdateEndpointJourneyMetadata(this IList<object> endpointMetadata, Action<EndpointJourneyMetadata> updateAction)
    {
        var journeyMetadata = endpointMetadata.OfType<EndpointJourneyMetadata>().FirstOrDefault();

        if (journeyMetadata == null)
        {
            journeyMetadata = new EndpointJourneyMetadata();
            endpointMetadata.Add(journeyMetadata);
        }

        updateAction(journeyMetadata);
    }
}
