namespace GovUk.Questions.Mvc;

#pragma warning disable CA1054, CA1056
/// <summary>
/// Represents the sequence of steps in a journey that are valid for the user to visit.
/// </summary>
public class JourneyPath
{
#pragma warning disable CA1859
    private static readonly IEqualityComparer<JourneyPathStep> _stepComparer = EqualityComparer<JourneyPathStep>.Default;
#pragma warning restore CA1859

    private readonly List<JourneyPathStep> _steps;

    /// <summary>
    /// Creates a new instance of <see cref="JourneyPath"/>.
    /// </summary>
    /// <param name="steps">The sequence of steps.</param>
    public JourneyPath(IEnumerable<JourneyPathStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        _steps = steps.ToList();
    }

    /// <summary>
    /// Gets the sequence of steps in the journey path.
    /// </summary>
    public IReadOnlyList<JourneyPathStep> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Adds a new step onto the journey path relative to the specified current step.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <param name="currentStep">The current step.</param>
    /// <param name="options">Options to control how adjacent steps to the added step are modified.</param>
    /// <returns><see langword="true"/> if the steps were modified; otherwise, <see langword="false"/>.</returns>
    public bool PushStep(JourneyPathStep step, JourneyPathStep currentStep, PushStepOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(step);

        var currentStepIndex = _steps.FindIndex(0, s => _stepComparer.Equals(s, currentStep));
        if (currentStepIndex == -1)
        {
            throw new InvalidOperationException("The specified current step does not exist in the journey path.");
        }

        // Check if the step already exists in the path
        var stepIndex = _steps.FindIndex(s => _stepComparer.Equals(s, step));
        if (stepIndex != -1 && stepIndex < currentStepIndex)
        {
            throw new InvalidOperationException("Cannot push a step that exists before the current step in the journey path.");
        }

        var stepsUpdated = false;

        if (stepIndex == currentStepIndex + 1 || stepIndex == currentStepIndex)
        {
            // Step is already the current step or the step immediately after the current step
        }
        else
        {
            // Remove any steps after the current step and add the new step
            _steps.RemoveRange(currentStepIndex + 1, _steps.Count - (currentStepIndex + 1));
            _steps.Add(step);
            stepIndex = _steps.Count - 1;
            stepsUpdated = true;
        }

        if (options.SetAsFirstStep)
        {
            _steps.RemoveRange(0, stepIndex);
            stepsUpdated = true;
        }

        if (options.SetAsLastStep)
        {
            _steps.RemoveRange(stepIndex + 1, _steps.Count - (stepIndex + 1));
            stepsUpdated = true;
        }

        return stepsUpdated;
    }
}

/// <summary>
/// Represents a step in a journey path.
/// </summary>
/// <param name="Url">The URL of the step.</param>
public record JourneyPathStep(string Url);

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

#pragma warning restore CA1054, CA1056
