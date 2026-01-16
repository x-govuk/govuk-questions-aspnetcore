using System.Diagnostics;
using System.Reflection;
using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Base class for coordinating a journey's state and behavior.
/// </summary>
public abstract class JourneyCoordinator
{
    /// <summary>
    /// The name of the query parameter used to specify the return URL.
    /// </summary>
    public const string ReturnUrlQueryParameterName = "returnUrl";

    private JourneyCoordinatorContext? _context;

    private bool _deleted;

    internal JourneyCoordinatorContext Context
    {
        get => _context ?? throw new InvalidOperationException("Coordinator context has not been initialized.");
        set => _context = value;
    }

    /// <summary>
    /// Gets the unique identifier for this journey instance.
    /// </summary>
    public JourneyInstanceId InstanceId => Context.InstanceId;

    /// <summary>
    /// Gets the <see cref="JourneyDescriptor"/> for this journey.
    /// </summary>
    public JourneyDescriptor Journey => Context.Journey;

    /// <summary>
    /// Gets the <see cref="HttpContext"/> for the current request.
    /// </summary>
    public HttpContext HttpContext => Context.HttpContext;

    /// <summary>
    /// Gets the current path for this journey instance.
    /// </summary>
    public JourneyPath Path => GetStateStorageEntry().Path;

    internal IJourneyStateStorage StateStorage => Context.JourneyStateStorage;

    /// <summary>
    /// Gets the state for this journey instance.
    /// </summary>
    /// <remarks>
    /// Any modifications to the state object returned by this property will not be persisted.
    /// </remarks>
    public object State => GetStateStorageEntry().State;

    /// <summary>
    /// Advances the journey to the specified <paramref name="nextStepUrl"/> without modifying the state.
    /// </summary>
    public AdvanceToResult AdvanceTo(
        string nextStepUrl,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);

        var vt = AdvanceToCoreAsync(
            nextStepUrl,
            ValueTask.FromResult,
            pushStepOptions);
        Debug.Assert(vt.IsCompleted);
#pragma warning disable VSTHRD002
        return vt.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Advances the journey to the specified <paramref name="nextStepUrl"/>, updating the state using the provided <paramref name="updateState"/> action.
    /// </summary>
    public AdvanceToResult AdvanceTo(
        string nextStepUrl,
        Action<object> updateState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(updateState);

        var vt = AdvanceToCoreAsync(
            nextStepUrl,
            s =>
            {
                updateState(s);
                return ValueTask.FromResult(s);
            },
            pushStepOptions);
        Debug.Assert(vt.IsCompleted);
#pragma warning disable VSTHRD002
        return vt.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Advances the journey to the specified <paramref name="nextStepUrl"/>, updating the state using the provided <paramref name="getNewState"/> function.
    /// </summary>
    public AdvanceToResult AdvanceTo(
        string nextStepUrl,
        Func<object, object> getNewState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(getNewState);

        var vt = AdvanceToCoreAsync(nextStepUrl, s => ValueTask.FromResult(getNewState(s)), pushStepOptions);
        Debug.Assert(vt.IsCompleted);
#pragma warning disable VSTHRD002
        return vt.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Advances the journey to the specified <paramref name="nextStepUrl"/>, updating the state using the provided <paramref name="updateState"/> function.
    /// </summary>
    public Task<AdvanceToResult> AdvanceToAsync(
        string nextStepUrl,
        Func<object, Task> updateState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(updateState);

        return AdvanceToCoreAsync(
                nextStepUrl,
                async s =>
                {
                    await updateState(s);
                    return s;
                },
                pushStepOptions)
            .AsTask();
    }

    /// <summary>
    /// Advances the journey to the specified <paramref name="nextStepUrl"/>, updating the state using the provided <paramref name="getNewState"/> function.
    /// </summary>
    public Task<AdvanceToResult> AdvanceToAsync(
        string nextStepUrl,
        Func<object, Task<object>> getNewState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(getNewState);

        return AdvanceToCoreAsync(
                nextStepUrl,
                async s => await getNewState(s),
                pushStepOptions)
            .AsTask();
    }

    internal async Task<object> GetStartingStateSafeAsync()
    {
        var state = await GetStartingStateCoreAsync();
        ThrowIfStateTypeIsInvalid(state.GetType());
        return state;
    }

    /// <summary>
    /// Gets the initial state for a newly-started journey instance.
    /// </summary>
    private protected virtual object GetStartingStateCore()
    {
        var stateType = Context.Journey.StateType;
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
    private protected virtual Task<object> GetStartingStateCoreAsync()
    {
        var state = GetStartingStateCore();
        return Task.FromResult(state);
    }

    /// <summary>
    /// Deletes the journey instance and its associated state.
    /// </summary>
    public void DeleteInstance()
    {
        if (_deleted)
        {
            return;
        }

        StateStorage.DeleteState(InstanceId, Journey);
        _deleted = true;
    }

    /// <summary>
    /// Creates a <see cref="JourneyPathStep"/> from the specified <paramref name="url"/>.
    /// </summary>
    public virtual JourneyPathStep CreateStepFromUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        var stepId = GetUrlWithoutQueryParameters(url, ReturnUrlQueryParameterName, JourneyInstanceId.KeyRouteValueName);
        return new JourneyPathStep(stepId, url);
    }

    internal JourneyPathStep CreateStepFromHttpContext(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var url = httpContext.Request.GetEncodedPathAndQuery();
        return CreateStepFromUrl(url);
    }

    /// <summary>
    /// Finds the current step in the journey path based on the current request.
    /// </summary>
    /// <returns>The <see cref="JourneyPathStep"/> if the step was found; otherwise <see langword="null"/>.</returns>
    public virtual JourneyPathStep? GetCurrentStep()
    {
        var currentStep = CreateStepFromHttpContext(HttpContext);
        return Path.ContainsStep(currentStep.StepId) ? currentStep : null;
    }

    /// <summary>
    /// Invoked when the current step is not valid for the journey instance.
    /// </summary>
    /// <remarks>
    /// The default implementation redirects to the last step in the journey path.
    /// </remarks>
    public virtual IResult OnInvalidStep()
    {
        return Path.Steps.Count > 0 ?
            Results.Redirect(Path.Steps.Last().GetUrl(InstanceId)) :
            Results.BadRequest();
    }

    /// <summary>
    /// Determines whether the specified <paramref name="step"/> is valid for the current journey path.
    /// </summary>
#pragma warning disable CA1716
    public virtual bool StepIsValid(JourneyPathStep step)
#pragma warning restore CA1716
    {
        ArgumentNullException.ThrowIfNull(step);

        return Path.ContainsStep(step.StepId);
    }

    /// <summary>
    /// Sets the journey path directly, bypassing any validation.
    /// </summary>
    public void UnsafeSetPath(JourneyPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var vt = UpdateStateStorageEntryCoreAsync(e => ValueTask.FromResult(e with { Path = path }));
        Debug.Assert(vt.IsCompleted);
#pragma warning disable VSTHRD002
        vt.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Updates the journey state by applying the specified <paramref name="updateState"/> function and persisting the changes.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void UpdateState(Action<object> updateState)
    {
        ArgumentNullException.ThrowIfNull(updateState);

        var vt = UpdateStateStorageEntryCoreAsync(e =>
        {
            var state = e.State;
            updateState(state);
            return ValueTask.FromResult(e with { State = state });
        });
        Debug.Assert(vt.IsCompleted);
#pragma warning disable VSTHRD002
        vt.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Updates the journey state by applying the specified <paramref name="getNewState"/> function and persisting the result.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public void UpdateState(Func<object, object> getNewState)
    {
        ArgumentNullException.ThrowIfNull(getNewState);

        var vt = UpdateStateStorageEntryCoreAsync(e => ValueTask.FromResult(e with { State = getNewState(e.State) }));
        Debug.Assert(vt.IsCompleted);
#pragma warning disable VSTHRD002
        vt.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Updates the journey state by applying the specified asynchronous <paramref name="updateState"/> function and persisting the changes.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public Task UpdateStateAsync(Func<object, Task> updateState)
    {
        ArgumentNullException.ThrowIfNull(updateState);

        return UpdateStateStorageEntryCoreAsync(async e =>
        {
            var state = e.State;
            await updateState(state);
            return e with { State = state };
        }).AsTask();
    }

    /// <summary>
    /// Updates the journey state by applying the specified asynchronous <paramref name="getNewState"/> function and persisting the result.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public Task UpdateStateAsync(Func<object, Task<object>> getNewState)
    {
        ArgumentNullException.ThrowIfNull(getNewState);

        return UpdateStateStorageEntryCoreAsync(async e => e with { State = await getNewState(e.State) }).AsTask();
    }

    // internal for testing
    internal static string GetUrlWithoutQueryParameters(string url, params string[] queryParameterNamesToRemove)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(queryParameterNamesToRemove);

        var queryStringStartIndex = url.IndexOf('?', StringComparison.Ordinal);

        if (queryStringStartIndex == -1)
        {
            return url;
        }

        var qs = QueryHelpers.ParseQuery(url[queryStringStartIndex..]);

        foreach (var param in queryParameterNamesToRemove)
        {
            qs.Remove(param);
        }

        return QueryHelpers.AddQueryString(url[..queryStringStartIndex], qs);
    }

    private static bool IsLocalUrl(string url)
    {
        // https://source.dot.net/#Microsoft.AspNetCore.Http.Results/src/Shared/ResultsHelpers/SharedUrlHelper.cs

        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Allows "/" or "/foo" but not "//" or "/\".
        if (url[0] == '/')
        {
            // url is exactly "/"
            if (url.Length == 1)
            {
                return true;
            }

            // url doesn't start with "//" or "/\"
            if (url[1] != '/' && url[1] != '\\')
            {
                return !HasControlCharacter(url.AsSpan(1));
            }

            return false;
        }

        // Allows "~/" or "~/foo" but not "~//" or "~/\".
        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
        {
            // url is exactly "~/"
            if (url.Length == 2)
            {
                return true;
            }

            // url doesn't start with "~//" or "~/\"
            if (url[2] != '/' && url[2] != '\\')
            {
                return !HasControlCharacter(url.AsSpan(2));
            }

            return false;
        }

        return false;

        static bool HasControlCharacter(ReadOnlySpan<char> readOnlySpan)
        {
            // URLs may not contain ASCII control characters.
            for (var i = 0; i < readOnlySpan.Length; i++)
            {
                if (char.IsControl(readOnlySpan[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private async ValueTask<AdvanceToResult> AdvanceToCoreAsync(
        string nextStepUrl,
        Func<object, ValueTask<object>> getNewState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(getNewState);

        await UpdateStateStorageEntryCoreAsync(async e =>
        {
            var currentStep = GetCurrentStep() ?? throw new InvalidOperationException("Current step not found in journey path.");

            var nextStep = CreateStepFromUrl(nextStepUrl);
            var newPath = e.Path.PushStep(nextStep, currentStep, pushStepOptions);

            var newState = await getNewState(e.State);

            return new StateStorageEntry { State = newState, Path = newPath };
        });

        // Check if there's an explicit return URL provided
        if (HttpContext.Request.Query.TryGetValue(ReturnUrlQueryParameterName, out var returnUrlValues) &&
            returnUrlValues.ToString() is string returnUrl && IsLocalUrl(returnUrl))
        {
            return new AdvanceToResult(returnUrl);
        }

        return new AdvanceToResult(nextStepUrl);
    }

    private StateStorageEntry GetStateStorageEntry() => StateStorage.GetState(InstanceId, Journey)!;

    private async ValueTask UpdateStateStorageEntryCoreAsync(
        Func<StateStorageEntry, ValueTask<StateStorageEntry>> getNewEntry)
    {
        ArgumentNullException.ThrowIfNull(getNewEntry);

        ThrowIfDeleted();

        var stateStorageEntry = GetStateStorageEntry();
        var newEntry = await getNewEntry(stateStorageEntry);
        ThrowIfStateTypeIsInvalid(newEntry.State.GetType());
        StateStorage.SetState(InstanceId, Journey, newEntry);
    }

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
    /// The state for this journey instance.
    /// </summary>
    /// <remarks>
    /// Any modifications to the state object returned by this property will not be persisted.
    /// </remarks>
    public new TState State => (TState)base.State;

    /// <inheritdoc cref="JourneyCoordinator.AdvanceTo(string,System.Action{object},GovUk.Questions.AspNetCore.PushStepOptions)"/>
    public AdvanceToResult AdvanceTo(
        string nextStepUrl,
        Action<TState> updateState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(updateState);

        return base.AdvanceTo(
            nextStepUrl,
            state => updateState((TState)state),
            pushStepOptions);
    }

    /// <inheritdoc cref="JourneyCoordinator.AdvanceTo(string,System.Func{object,object},GovUk.Questions.AspNetCore.PushStepOptions)"/>
    public AdvanceToResult AdvanceTo(
        string nextStepUrl,
        Func<TState, TState> getNewState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(getNewState);

        return base.AdvanceTo(
            nextStepUrl,
            state => getNewState((TState)state),
            pushStepOptions);
    }

    /// <inheritdoc cref="JourneyCoordinator.AdvanceToAsync(string,System.Func{object,System.Threading.Tasks.Task},GovUk.Questions.AspNetCore.PushStepOptions)"/>
    public Task<AdvanceToResult> AdvanceToAsync(
        string nextStepUrl,
        Func<TState, Task> updateState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(updateState);

        return base.AdvanceToAsync(
            nextStepUrl,
            async state => await updateState((TState)state),
            pushStepOptions);
    }

    /// <inheritdoc cref="JourneyCoordinator.AdvanceToAsync(string,System.Func{object,System.Threading.Tasks.Task{object}},GovUk.Questions.AspNetCore.PushStepOptions)"/>
    public Task<AdvanceToResult> AdvanceToAsync(
        string nextStepUrl,
        Func<TState, Task<TState>> getNewState,
        PushStepOptions pushStepOptions = default)
    {
        ArgumentNullException.ThrowIfNull(nextStepUrl);
        ArgumentNullException.ThrowIfNull(getNewState);

        return base.AdvanceToAsync(
            nextStepUrl,
            async state => await getNewState((TState)state),
            pushStepOptions);
    }

    /// <summary>
    /// Asynchronously gets the initial state for a newly-started journey instance.
    /// </summary>
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    public virtual Task<TState> GetStartingStateAsync()
    {
        return Task.FromResult(GetStartingState());
    }

    /// <summary>
    /// Gets the initial state for a newly-started journey instance.
    /// </summary>
    // ReSharper disable once VirtualMemberNeverOverridden.Global
    public virtual TState GetStartingState()
    {
        return (TState)base.GetStartingStateCore();
    }

    private protected override async Task<object> GetStartingStateCoreAsync()
    {
        return await GetStartingStateAsync();
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
