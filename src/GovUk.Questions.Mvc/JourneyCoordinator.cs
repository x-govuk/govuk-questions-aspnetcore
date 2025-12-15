using System.Diagnostics;
using System.Reflection;
using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;

namespace GovUk.Questions.Mvc;

/// <summary>
/// Base class for coordinating a journey's state and behavior.
/// </summary>
/// <typeparam name="TState">The type of the journey's state.</typeparam>
public abstract class JourneyCoordinator<TState> where TState : class
{
    private JourneyInstanceId? _instanceId;
    private JourneyDescriptor? _journey;
    private IJourneyStateStorage? _stateStorage;

    private bool _deleted;

    /// <summary>
    /// The unique identifier for this journey instance.
    /// </summary>
    public JourneyInstanceId InstanceId
    {
        get => _instanceId ?? throw new InvalidOperationException($"{nameof(InstanceId)} has not been initialized.");
        internal set
        {
            ArgumentNullException.ThrowIfNull(value);
            _instanceId = value;
        }
    }

    /// <summary>
    /// The <see cref="JourneyDescriptor"/> that describes the journey.
    /// </summary>
    public JourneyDescriptor Journey
    {
        get => _journey ?? throw new InvalidOperationException($"{nameof(Journey)} has not been initialized.");
        internal set
        {
            ArgumentNullException.ThrowIfNull(value);
            Debug.Assert(value.StateType == typeof(TState));
            _journey = value;
        }
    }

    /// <summary>
    /// The state for this journey instance.
    /// </summary>
    /// <remarks>
    /// Any modifications to the state object returned by this property will not be persisted.
    /// Use <see cref="UpdateState(Action{TState})"/> or <see cref="UpdateStateAsync(Func{TState, Task})"/> to persist changes.
    /// </remarks>
    public TState State => (TState)StateStorage.GetState(InstanceId)!.State;

    internal IJourneyStateStorage StateStorage
    {
        get => _stateStorage ?? throw new InvalidOperationException($"{nameof(StateStorage)} has not been initialized.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _stateStorage = value;
        }
    }

    /// <summary>
    /// Gets the initial state for a newly-started journey instance.
    /// </summary>
    public virtual Task<TState> GetStartingStateAsync(GetStartingStateContext context)
    {
        Debug.Assert(_journey is not null);
        Debug.Assert(_instanceId is not null);
        Debug.Assert(Journey.StateType == typeof(TState));

        ArgumentNullException.ThrowIfNull(context);

        var stateType = typeof(TState);
        var defaultConstructor = stateType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, []);
        if (defaultConstructor is null)
        {
            throw new InvalidOperationException(
                $"No default constructor found on type '{stateType.FullName}'. " +
                $"Add a default constructor or override '{nameof(GetStartingStateAsync)}' on '{GetType().FullName}'.");
        }

        var state = Activator.CreateInstance<TState>();
        return Task.FromResult(state);
    }

    /// <summary>
    /// Deletes the journey instance and its associated state.
    /// </summary>
    public void Delete()
    {
        if (_deleted)
        {
            return;
        }

        StateStorage.DeleteState(InstanceId);
        _deleted = true;
    }

    /// <summary>
    /// Updates the journey state by applying the specified <paramref name="updateAction"/> and persists the changes.
    /// </summary>
    public void UpdateState(Action<TState> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        ThrowIfDeleted();

        var state = State;
        updateAction(state);
        StateStorage.SetState(InstanceId, new() { State = state });
    }

    /// <summary>
    /// Asynchronously updates the journey state by applying the specified <paramref name="updateAction"/> and persists the changes.
    /// </summary>
    public async Task UpdateStateAsync(Func<TState, Task> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        ThrowIfDeleted();

        var state = State;
        await updateAction(state);
        StateStorage.SetState(InstanceId, new() { State = state });
    }

    private void ThrowIfDeleted()
    {
        if (_deleted)
        {
            throw new InvalidOperationException("Journey instance has been deleted.");
        }
    }
}
