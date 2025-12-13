using System.Text.Json;
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
        ValueProviderFactories =
        [
            new RouteValueProviderFactory(),
            new QueryStringValueProviderFactory()
        ];

        StateSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
    }

    internal IReadOnlyCollection<IValueProviderFactory> ValueProviderFactories { get; }

    /// <summary>
    /// Serializer options used to serialize and deserialize journey state.
    /// </summary>
    public JsonSerializerOptions StateSerializerOptions { get; }
}
