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

    /// <summary>
    /// Indicates whether the endpoint has opted out of the journey.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/> the endpoint is treated as though it is not part of a journey, even if a
    /// journey has been specified for the controller, action or page handler.
    /// </remarks>
    public bool Excluded { get; set; }
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
