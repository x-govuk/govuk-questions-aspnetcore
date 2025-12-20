using System.Text.Json;
using GovUk.Questions.Mvc.Description;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GovUk.Questions.Mvc;

/// <summary>
/// Options for configuring GovUk.Questions.Mvc.
/// </summary>
public class GovUkQuestionsOptions
{
    /// <summary>
    /// Creates a new instance of <see cref="GovUkQuestionsOptions"/>.
    /// </summary>
    public GovUkQuestionsOptions()
    {
        Journeys = new JourneyInfoRegistry();

        StateSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);

        ValueProviderFactories =
        [
            new RouteValueProviderFactory(),
            new QueryStringValueProviderFactory()
        ];
    }

    /// <summary>
    /// Serializer options used to serialize and deserialize journey state.
    /// </summary>
    public JsonSerializerOptions StateSerializerOptions { get; }

    internal JourneyInfoRegistry Journeys { get; }

    internal IReadOnlyCollection<IValueProviderFactory> ValueProviderFactories { get; }
}
