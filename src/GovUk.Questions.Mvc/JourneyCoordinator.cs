using System.Diagnostics;
using System.Reflection;
using GovUk.Questions.Mvc.Description;
using GovUk.Questions.Mvc.State;

namespace GovUk.Questions.Mvc;

/// <summary>
/// Base class for coordinating a journey's state and behavior.
/// </summary>
public abstract class JourneyCoordinator
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
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
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
            _journey = value;
        }
    }

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
    /// The state for this journey instance.
    /// </summary>
    /// <remarks>
    /// Any modifications to the state object returned by this property will not be persisted.
    /// Use <see cref="UpdateState(Func{object, object})"/> or <see cref="UpdateStateAsync(Func{object, Task{object}})"/> to persist changes.
    /// </remarks>
    public object State => GetStateStorageEntry().State;

    /// <summary>
    /// Gets the <see cref="JourneyPath"/> for this journey instance.
    /// </summary>
    public JourneyPath Path => GetStateStorageEntry().Path;

    internal async Task<object> GetStartingStateSafeAsync(GetStartingStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var state = await GetStartingStateCoreAsync(context);
        ThrowIfStateTypeIsInvalid(state.GetType());
        return state;
    }

    /// <summary>
    /// Gets the initial state for a newly-started journey instance.
    /// </summary>
    private protected virtual object GetStartingStateCore(GetStartingStateContext context)
    {
        Debug.Assert(_journey is not null);
        Debug.Assert(_instanceId is not null);

        ArgumentNullException.ThrowIfNull(context);

        var stateType = _journey.StateType;
        var defaultConstructor = stateType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, []);
        if (defaultConstructor is null)
        {
            throw new InvalidOperationException(
                $"No default constructor found on type '{stateType.FullName}'. " +
                $"Add a default constructor or override '{nameof(GetStartingStateCoreAsync)}' on '{GetType().FullName}'.");
        }

        var state = Activator.CreateInstance(stateType)!;
        return state;
    }

    /// <summary>
    /// Asynchronously gets the initial state for a newly-started journey instance.
    /// </summary>
    private protected virtual Task<object> GetStartingStateCoreAsync(GetStartingStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var state = GetStartingStateCore(context);
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

        StateStorage.DeleteState(InstanceId, Journey);
        _deleted = true;
    }

    /// <summary>
    /// Updates the journey state by applying the specified <paramref name="updateState"/> function and persisting the changes.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void UpdateState(Action<object> updateState)
    {
        ArgumentNullException.ThrowIfNull(updateState);

        UpdateState(state =>
        {
            updateState(state);
            return state;
        });
    }

    /// <summary>
    /// Updates the journey state by applying the specified <paramref name="getNewState"/> function and persisting the result.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void UpdateState(Func<object, object> getNewState)
    {
        ArgumentNullException.ThrowIfNull(getNewState);

        ThrowIfDeleted();

        var stateStorageEntry = GetStateStorageEntry();
        var state = stateStorageEntry.State;
        state = getNewState(state);
        ThrowIfStateTypeIsInvalid(state.GetType());
        StateStorage.SetState(InstanceId, Journey, stateStorageEntry with { State = state });
    }

    /// <summary>
    /// Updates the journey state by applying the specified asynchronous <paramref name="updateState"/> function and persisting the changes.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public Task UpdateStateAsync(Func<object, Task> updateState)
    {
        ArgumentNullException.ThrowIfNull(updateState);

        return UpdateStateAsync(async s =>
        {
            await updateState(s);
            return s;
        });
    }

    /// <summary>
    /// Updates the journey state by applying the specified asynchronous <paramref name="getNewState"/> function and persisting the result.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public async Task UpdateStateAsync(Func<object, Task<object>> getNewState)
    {
        ArgumentNullException.ThrowIfNull(getNewState);

        ThrowIfDeleted();

        var stateStorageEntry = GetStateStorageEntry();
        var state = stateStorageEntry.State;
        state = await getNewState(state);
        ThrowIfStateTypeIsInvalid(state.GetType());
        StateStorage.SetState(InstanceId, Journey, stateStorageEntry with { State = state });
    }

    private StateStorageEntry GetStateStorageEntry() => StateStorage.GetState(InstanceId, Journey)!;

    private void ThrowIfDeleted()
    {
        if (_deleted)
        {
            throw new InvalidOperationException("Journey instance has been deleted.");
        }
    }

    private void ThrowIfStateTypeIsInvalid(Type type)
    {
        Debug.Assert(Journey is not null);

        if (!Journey.IsStateTypeValid(type))
        {
            throw new InvalidOperationException(
                "State type is not valid; expected " +
                $"'{Journey.StateType.FullName}', but got '{type.FullName}'.");
        }
    }
}

/// <inheritdoc/>
/// <typeparam name="TState">The type of the journey's state.</typeparam>
public abstract class JourneyCoordinator<TState> : JourneyCoordinator where TState : class
{
    /// <summary>
    /// The <see cref="JourneyDescriptor"/> that describes the journey.
    /// </summary>
    public new JourneyDescriptor Journey
    {
        get => base.Journey;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        internal set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.StateType != typeof(TState))
            {
                throw new InvalidOperationException(
                    $"Journey state type '{value.StateType.FullName}' does not match the generic type parameter '{typeof(TState).FullName}' " +
                    $"on '{GetType().FullName}'.");
            }

            base.Journey = value;
        }
    }

    /// <summary>
    /// The state for this journey instance.
    /// </summary>
    /// <remarks>
    /// Any modifications to the state object returned by this property will not be persisted.
    /// Use <see cref="UpdateState(Func{TState, TState})"/> or <see cref="UpdateStateAsync(Func{TState, Task{TState}})"/> to persist changes.
    /// </remarks>
    public new TState State => (TState)base.State;

    /// <summary>
    /// Asynchronously gets the initial state for a newly-started journey instance.
    /// </summary>
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    public virtual Task<TState> GetStartingStateAsync(GetStartingStateContext context)
    {
        return Task.FromResult(GetStartingState(context));
    }

    /// <summary>
    /// Gets the initial state for a newly-started journey instance.
    /// </summary>
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    public virtual TState GetStartingState(GetStartingStateContext context)
    {
        return (TState)base.GetStartingStateCore(context);
    }

    private protected override async Task<object> GetStartingStateCoreAsync(GetStartingStateContext context)
    {
        return await GetStartingStateAsync(context);
    }

    /// <inheritdoc cref="JourneyCoordinator.UpdateState(Action{object})"/>
    // ReSharper disable once UnusedMember.Global
    public void UpdateState(Action<TState> updateState)
    {
        ArgumentNullException.ThrowIfNull(updateState);

        UpdateState(state =>
        {
            updateState(state);
            return state;
        });
    }

    /// <inheritdoc cref="JourneyCoordinator.UpdateState(Func{object, object})"/>
    // ReSharper disable once UnusedMember.Global
    public void UpdateState(Func<TState, TState> getNewState)
    {
        ArgumentNullException.ThrowIfNull(getNewState);

        base.UpdateState(state => getNewState((TState)state));
    }

    /// <inheritdoc cref="JourneyCoordinator.UpdateStateAsync(Func{object, Task})"/>
    // ReSharper disable once UnusedMember.Global
    public Task UpdateStateAsync(Func<TState, Task> updateState)
    {
        ArgumentNullException.ThrowIfNull(updateState);

        return UpdateStateAsync(async s =>
        {
            await updateState(s);
            return s;
        });
    }

    /// <inheritdoc cref="JourneyCoordinator.UpdateStateAsync(Func{object, Task{object}})"/>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public Task UpdateStateAsync(Func<TState, Task<TState>> getNewState)
    {
        ArgumentNullException.ThrowIfNull(getNewState);

        return base.UpdateStateAsync(async state => await getNewState((TState)state));
    }
}
