namespace GovUk.Questions.AspNetCore.Tests;

public class JourneyPathTests
{
    [Fact]
    public void ContainsStep_StepExistsInPath_ReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var stepToCheck = new JourneyPathStep("Step2", "/step-2?qs");

        // Act
        var result = journeyPath.ContainsStep(stepToCheck);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsStep_StepDoesNotExistInPath_ReturnsFalse()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var stepToCheck = new JourneyPathStep("Step4", "/step-4");

        // Act
        var result = journeyPath.ContainsStep(stepToCheck);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PushStep_CurrentStepDoesNotExistInPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("Step3", "/step-3");
        var nonExistentCurrentStep = new JourneyPathStep("NonExistentStep", "/non-existent-step");

        // Act
        var ex = Record.Exception(() => journeyPath.PushStep(newStep, nonExistentCurrentStep));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("The specified current step does not exist in the journey path.", ex.Message);
    }

    [Fact]
    public void PushStep_NewStepIsNotPartOfPathAndCurrentStepIsLastStep_AddsStep()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("Step3", "/step-3");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1]);

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url),
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_NewStepIsNotPartOfPathAndCurrentStepIsNotLastStep_RemovesTrailingStepsAddsStep()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("Step4", "/step-4");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1]);

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url),
            step => Assert.Equal("/step-4", step.Url));
    }

    [Fact]
    public void PushStep_StepIsLastStepInPath_DoesNotModifyPath()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("Step2", "/step-2");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[1]);

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url));
    }

    [Fact]
    public void PushStep_StepExistsInPathImmediatelyAfterCurrentStepAndSetAsLastStepIsFalse_DoesNotModifyPath()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("Step2", "/step-2");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[0], new PushStepOptions { SetAsLastStep = false });

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url),
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_StepExistsInPathImmediatelyAfterCurrentStepAndSetAsLastStepIsTrue_RemovesTrailingSteps()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("Step2", "/step-2");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[0], new PushStepOptions { SetAsLastStep = true });

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url));
    }

    [Fact]
    public void PushStep_StepExistsInPathBeforeCurrentStep_ThrowsInvalidOperationException()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("Step1", "/step-1");

        // Act
        var ex = Record.Exception(() => journeyPath.PushStep(existingStep, currentStep: initialSteps[2]));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("Cannot push a step that exists before the current step in the journey path.", ex.Message);
    }

    [Fact]
    public void PushStep_StepExistsInPathAfterCurrentStep_RemovesTrailingStepsAddsStep()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2"),
            new("Step3", "/step-3"),
            new("Step4", "/step-4")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("Step3", "/step-3");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[0]);

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_NewStepIsNotPartOfPathAndSetAsFirstStepTrue_AddsNewStepRemovesPrecedingSteps()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("Step3", "/step-3");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1], new PushStepOptions { SetAsFirstStep = true });

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_NewStepIsPartOfPathAndSetAsFirstStepTrue_AddsNewStepRemovesPrecedingSteps()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("Step1", "/step-1"),
            new("Step2", "/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("Step2", "/step-2");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1], new PushStepOptions { SetAsFirstStep = true });

        // Assert
        Assert.Collection(
            result.Steps,
            step => Assert.Equal("/step-2", step.Url));
    }
}
