using System.Text;
using GovUk.Questions.AspNetCore.Description;
using Microsoft.Extensions.Options;

namespace GovUk.Questions.AspNetCore.State;

/// <summary>
/// An implementation of <see cref="IJourneyStateStorage"/> that uses the local filesystem for development purposes.
/// </summary>
public class DevelopmentJourneyStateStorage(IOptions<GovUkQuestionsOptions> optionsAccessor) : JsonJourneyStateStorage(optionsAccessor)
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GovUkQuestionsAspNetCore",
        "DevelopmentJourneyStateStorage");

    /// <inheritdoc/>
    public override void DeleteState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        EnsureDirectory();

        var fileName = GetFileName(instanceId);
        var filePath = Path.Combine(_directory, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <inheritdoc/>
    public override StateStorageEntry? GetState(JourneyInstanceId instanceId, JourneyDescriptor journey)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);

        EnsureDirectory();

        var fileName = GetFileName(instanceId);
        var filePath = Path.Combine(_directory, fileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var data = File.ReadAllBytes(filePath);
        return DeserializeStateEntry(data);
    }

    /// <inheritdoc/>
    public override void SetState(JourneyInstanceId instanceId, JourneyDescriptor journey, StateStorageEntry stateEntry)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(stateEntry);

        EnsureDirectory();

        var fileName = GetFileName(instanceId);
        var filePath = Path.Combine(_directory, fileName);

        var data = SerializeStateEntry(journey, stateEntry);
        File.WriteAllBytes(filePath, data);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(_directory);

    private string GetFileName(JourneyInstanceId instanceId) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(instanceId.ToString())) + ".json";
}
