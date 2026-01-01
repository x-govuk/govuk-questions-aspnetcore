namespace GovUk.Questions.Mvc.Tests;

public class JourneyPathTests
{
    [Fact]
    public void PushStep_CurrentStepDoesNotExistInPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("/step-3");
        var nonExistentCurrentStep = new JourneyPathStep("/non-existent-step");

        // Act
        var ex = Record.Exception(() => journeyPath.PushStep(newStep, nonExistentCurrentStep));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("The specified current step does not exist in the journey path.", ex.Message);
    }

    [Fact]
    public void PushStep_NewStepIsNotPartOfPathAndCurrentStepIsLastStep_AddsStepAndReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("/step-3");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1]);

        // Assert
        Assert.True(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url),
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_NewStepIsNotPartOfPathAndCurrentStepIsNotLastStep_RemovesTrailingStepsAddsStepAndReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2"),
            new("/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("/step-4");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1]);

        // Assert
        Assert.True(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url),
            step => Assert.Equal("/step-4", step.Url));
    }

    [Fact]
    public void PushStep_StepIsLastStepInPath_DoesNotModifyPathAndReturnsFalse()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("/step-2");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[1]);

        // Assert
        Assert.False(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url));
    }

    [Fact]
    public void PushStep_StepExistsInPathImmediatelyAfterCurrentStepAndSetAsLastStepIsFalse_DoesNotModifyPathAndReturnsFalse()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2"),
            new("/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("/step-2");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[0], new PushStepOptions { SetAsLastStep = false });

        // Assert
        Assert.False(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url),
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_StepExistsInPathImmediatelyAfterCurrentStepAndSetAsLastStepIsTrue_RemovesTrailingStepsAndReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2"),
            new("/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("/step-2");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[0], new PushStepOptions { SetAsLastStep = true });

        // Assert
        Assert.True(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-2", step.Url));
    }

    [Fact]
    public void PushStep_StepExistsInPathBeforeCurrentStep_ThrowsInvalidOperationException()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2"),
            new("/step-3")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("/step-1");

        // Act
        var ex = Record.Exception(() => journeyPath.PushStep(existingStep, currentStep: initialSteps[2]));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("Cannot push a step that exists before the current step in the journey path.", ex.Message);
    }

    [Fact]
    public void PushStep_StepExistsInPathAfterCurrentStep_RemovesTrailingStepsAddsStepAndReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2"),
            new("/step-3"),
            new("/step-4")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var existingStep = new JourneyPathStep("/step-3");

        // Act
        var result = journeyPath.PushStep(existingStep, currentStep: initialSteps[0]);

        // Assert
        Assert.True(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-1", step.Url),
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_NewStepIsNotPartOfPathAndSetAsFirstStepTrue_AddsNewStepRemovesPrecedingStepsAndReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("/step-3");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1], new PushStepOptions { SetAsFirstStep = true });

        // Assert
        Assert.True(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-3", step.Url));
    }

    [Fact]
    public void PushStep_NewStepIsPartOfPathAndSetAsFirstStepTrue_AddsNewStepRemovesPrecedingStepsAndReturnsTrue()
    {
        // Arrange
        var initialSteps = new List<JourneyPathStep>
        {
            new("/step-1"),
            new("/step-2")
        };
        var journeyPath = new JourneyPath(initialSteps);
        var newStep = new JourneyPathStep("/step-2");

        // Act
        var result = journeyPath.PushStep(newStep, currentStep: initialSteps[1], new PushStepOptions { SetAsFirstStep = true });

        // Assert
        Assert.True(result);
        Assert.Collection(
            journeyPath.Steps,
            step => Assert.Equal("/step-2", step.Url));
    }
}
