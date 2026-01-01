using System.Text.Json;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Options for configuring GovUk.Questions.AspNetCore.
/// </summary>
public class GovUkQuestionsOptions
{
    /// <summary>
    /// Creates a new instance of <see cref="GovUkQuestionsOptions"/>.
    /// </summary>
    public GovUkQuestionsOptions()
    {
        StateSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
    }

    /// <summary>
    /// Serializer options used to serialize and deserialize journey state.
    /// </summary>
    public JsonSerializerOptions StateSerializerOptions { get; }
}
