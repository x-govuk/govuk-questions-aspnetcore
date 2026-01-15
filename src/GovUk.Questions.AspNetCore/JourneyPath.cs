using System.Text.Json;
using System.Text.Json.Serialization;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Represents the sequence of steps in a journey that are valid for the user to visit.
/// </summary>
[JsonConverter(typeof(JourneyPathJsonConverter))]
public class JourneyPath
{
    /// <summary>
    /// Creates a new instance of <see cref="JourneyPath"/>.
    /// </summary>
    /// <param name="steps">The sequence of steps.</param>
    public JourneyPath(IEnumerable<JourneyPathStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        Steps = steps.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the sequence of steps in the journey path.
    /// </summary>
    public IReadOnlyCollection<JourneyPathStep> Steps { get; }

    /// <summary>
    /// Determines whether the journey path contains the specified step.
    /// </summary>
    public bool ContainsStep(JourneyPathStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return ContainsStep(step.StepId);
    }

    /// <summary>
    /// Determines whether the journey path contains a step with the specified step ID.
    /// </summary>
    public bool ContainsStep(string stepId)
    {
        ArgumentNullException.ThrowIfNull(stepId);

        return Steps.Any(s => s.StepId == stepId);
    }

    /// <summary>
    /// Adds a new step onto the journey path relative to the specified current step.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <param name="currentStep">The current step.</param>
    /// <param name="options">Options to control how adjacent steps to the added step are modified.</param>
    /// <returns>A new <see cref="JourneyPath"/> with the new step added.</returns>
    public JourneyPath PushStep(JourneyPathStep step, JourneyPathStep currentStep, PushStepOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(step);

        var newSteps = new List<JourneyPathStep>(Steps);

        var currentStepIndex = newSteps.FindIndex(0, s => s.StepId == currentStep.StepId);
        if (currentStepIndex == -1)
        {
            throw new InvalidOperationException("The specified current step does not exist in the journey path.");
        }

        // Check if the step already exists in the path
        var stepIndex = newSteps.FindIndex(s => s.StepId == step.StepId);
        if (stepIndex != -1 && stepIndex < currentStepIndex)
        {
            throw new InvalidOperationException("Cannot push a step that exists before the current step in the journey path.");
        }

        if (stepIndex == currentStepIndex + 1 || stepIndex == currentStepIndex)
        {
            // Step is already the current step or the step immediately after the current step
        }
        else
        {
            // Remove any steps after the current step and add the new step
            newSteps.RemoveRange(currentStepIndex + 1, newSteps.Count - (currentStepIndex + 1));
            newSteps.Add(step);
            stepIndex = newSteps.Count - 1;
        }

        if (options.SetAsFirstStep)
        {
            newSteps.RemoveRange(0, stepIndex);
        }

        if (options.SetAsLastStep)
        {
            newSteps.RemoveRange(stepIndex + 1, newSteps.Count - (stepIndex + 1));
        }

        return new JourneyPath(newSteps);
    }
}

/// <summary>
/// Represents a step in a journey path.
/// </summary>
/// <param name="StepId">The ID of the step.</param>
/// <param name="Url">The URL of the step.</param>
public record JourneyPathStep(string StepId, string Url);

/// <summary>
/// Options for configuring the <see cref="JourneyPath.PushStep"/> method.
/// </summary>
public record struct PushStepOptions
{
    /// <summary>
    /// Whether the new step should be set as the first step in the journey path.
    /// </summary>
    public bool SetAsFirstStep { get; set; }

    /// <summary>
    /// Whether the new step should be set as the last step in the journey path.
    /// </summary>
    public bool SetAsLastStep { get; set; }
}

internal class JourneyPathJsonConverter : JsonConverter<JourneyPath>
{
    public override JourneyPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var steps = JsonSerializer.Deserialize<List<JourneyPathStep>>(ref reader, options);
        return steps is not null ? new JourneyPath(steps) : null;
    }

    public override void Write(Utf8JsonWriter writer, JourneyPath value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Steps, options);
    }
}
