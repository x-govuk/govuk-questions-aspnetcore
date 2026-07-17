# GOV.UK Question Pages for ASP.NET Core

[![NuGet version](https://img.shields.io/nuget/v/GovUk.Questions.AspNetCore.svg)](https://www.nuget.org/packages/GovUk.Questions.AspNetCore/)

This library helps you build the GOV.UK Design System [question pages pattern](https://design-system.service.gov.uk/patterns/question-pages/) in ASP.NET Core.

The pattern asks users **one thing per page** across a series of steps, with a back link on every page, a *Check your answers* page, and the ability to change earlier answers. This library looks after the parts of that pattern that live on the server:

- creating and tracking a **journey instance** for each user as they work through the questions;
- persisting their answers between requests;
- making sure users can only follow a valid path through the steps;
- generating back links and *Change* links, including the "return to check your answers" behaviour.

It manages journey state and navigation only. Pair it with [GovUk.Frontend.AspNetCore](https://github.com/x-govuk/govuk-frontend-aspnetcore) to render the GOV.UK Design System components themselves.

## Contents

- [Minimum requirements](#minimum-requirements)
- [Installation](#installation)
- [Getting started](#getting-started)
- [Key concepts](#key-concepts)
- [Setting up a journey with MVC](#setting-up-a-journey-with-mvc)
  - [1. Define the journey state](#1-define-the-journey-state)
  - [2. Create a coordinator](#2-create-a-coordinator)
  - [3. Create the controller](#3-create-the-controller)
  - [4. Render a question page](#4-render-a-question-page)
  - [5. Check your answers and changing answers](#5-check-your-answers-and-changing-answers)
  - [6. Complete the journey](#6-complete-the-journey)
- [Coordinator API](#coordinator-api)
- [Storing journey state](#storing-journey-state)
- [Configuring options](#configuring-options)
- [Testing](#testing)
- [Building the library](#building-the-library)
- [License](#license)

## Minimum requirements

- .NET 8.0 or later

## Installation

Install the package from [nuget.org](https://www.nuget.org/packages/GovUk.Questions.AspNetCore/).

Using the .NET CLI:

```sh
dotnet add package GovUk.Questions.AspNetCore
```

Using the Package Manager Console:

```powershell
Install-Package GovUk.Questions.AspNetCore
```

Or add a `PackageReference` to your project file:

```xml
<PackageReference Include="GovUk.Questions.AspNetCore" Version="*" />
```

## Getting started

Register the services in `Program.cs` and add the middleware. Journey state is stored in the ASP.NET Core [session](https://learn.microsoft.com/aspnet/core/fundamentals/app-state) by default, so you also need to add session support.

```csharp
using GovUk.Questions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSession();
builder.Services.AddGovUkQuestions();

var app = builder.Build();

app.UseRouting();
app.UseSession();

app.MapControllers();

app.Run();
```

`UseSession` must be added before the endpoint middleware (`MapControllers`) so that journey state is available while your actions run.

That's all the setup the library needs. The rest of this guide walks through building a journey.

## Key concepts

**Journey** — a named, multi-step flow of question pages, for example `"Apply"`. You declare a journey once, by writing a coordinator for it.

**Journey instance** — one user's run through a journey. Each instance is identified by a generated key, passed around as the `_jid` query string parameter. Only the key is sent to the browser; the answers stay on the server. An instance can also be scoped to one or more route values (for example an `id`) so that a user can have separate in-progress journeys for different resources.

**Coordinator** — a class deriving from `JourneyCoordinator<TState>` that represents a journey. You inject it into your controller and use it to read state, advance to the next step, generate back links, and end the journey. For each request that belongs to a journey, the coordinator is resolved from the current instance with its saved state loaded.

**State** — a plain C# object holding the user's answers so far. It is serialized to JSON and stored between requests.

**Path** — the ordered list of steps the user has actually visited. The library uses it to decide whether a requested step is valid and to work out back links. Users can't skip ahead to a step they haven't reached.

## Setting up a journey with MVC

This example builds a short "Apply" journey with two questions (name and date of birth) followed by a *Check your answers* page.

> The `[Journey]`, `[StartsJourney]` and `[ExcludeFromJourney]` attributes work with Razor Pages handlers too. This guide uses MVC controllers.

### 1. Define the journey state

The state is a plain class that holds the answers collected so far. It needs a public parameterless constructor and must round-trip through JSON.

```csharp
public class ApplyState
{
    public string? Name { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
```

### 2. Create a coordinator

Derive from `JourneyCoordinator<TState>` and annotate it with `[JourneyCoordinator]`, giving the journey a name and the route values that scope an instance (none, in this case).

```csharp
using GovUk.Questions.AspNetCore;

[JourneyCoordinator("Apply", [])]
public class ApplyJourneyCoordinator : JourneyCoordinator<ApplyState>
{
    // Override this to set the initial state for a new instance.
    // The default creates a new state object using its parameterless constructor.
    public override ApplyState GetStartingState() => new();
}
```

The coordinator is registered in the DI container automatically, so you can inject it into a controller — and even inject your own services into the coordinator's constructor.

### 3. Create the controller

Annotate the controller with `[Journey]` to associate its endpoints with the journey, and mark the first step with `[StartsJourney]`. Inject the coordinator through the constructor.

```csharp
using GovUk.Questions.AspNetCore;
using Microsoft.AspNetCore.Mvc;

[Route("apply")]
[Journey("Apply")]
public class ApplyController(ApplyJourneyCoordinator journey) : Controller
{
    [HttpGet("name")]
    [StartsJourney]
    public IActionResult Name()
    {
        ViewBag.BackLink = journey.GetBackLink();
        return View(new NameViewModel { Name = journey.State.Name });
    }

    [HttpPost("name")]
    public IActionResult Name(NameViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BackLink = journey.GetBackLink();
            return View(model);
        }

        return journey.AdvanceTo(
            Url.Action(nameof(DateOfBirth), journey.InstanceId.RouteValues)!,
            state => state.Name = model.Name);
    }

    [HttpGet("date-of-birth")]
    public IActionResult DateOfBirth()
    {
        ViewBag.BackLink = journey.GetBackLink();
        return View(new DateOfBirthViewModel { DateOfBirth = journey.State.DateOfBirth });
    }

    [HttpPost("date-of-birth")]
    public IActionResult DateOfBirth(DateOfBirthViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BackLink = journey.GetBackLink();
            return View(model);
        }

        return journey.AdvanceTo(
            Url.Action(nameof(CheckAnswers), journey.InstanceId.RouteValues)!,
            state => state.DateOfBirth = model.DateOfBirth);
    }

    [HttpGet("check-answers")]
    public IActionResult CheckAnswers()
    {
        ViewBag.BackLink = journey.GetBackLink();
        return View(journey.State);
    }

    [HttpPost("check-answers")]
    public IActionResult CheckAnswersPost()
    {
        // Persist the collected answers (journey.State) to your data store here.

        journey.DeleteInstance();

        return RedirectToAction(nameof(Confirmation));
    }

    // Shown after the instance has been deleted, so it opts out of the journey (see step 6).
    [HttpGet("confirmation")]
    [ExcludeFromJourney]
    public IActionResult Confirmation() => View();
}
```

The view models are ordinary classes with validation attributes:

```csharp
using System.ComponentModel.DataAnnotations;

public class NameViewModel
{
    [Required(ErrorMessage = "Enter your full name")]
    public string? Name { get; set; }
}

public class DateOfBirthViewModel
{
    [Required(ErrorMessage = "Enter your date of birth")]
    public DateOnly? DateOfBirth { get; set; }
}
```

**What happens on the first request?** When a user visits `/apply/name` without a `_jid`, the library creates a new instance (calling `GetStartingState`) and redirects them to the same page with the key appended, for example `/apply/name?_jid=aBcD…`. From then on the `_jid` is carried through every link and form post, so the coordinator always resolves to the right instance. Your action only runs once a valid instance exists.

**Moving to the next step.** `AdvanceTo` saves the state, records the step in the path, and returns a redirect (`AdvanceToResult` is an `IActionResult`) to the next page. Build the next step's URL with `Url.Action(..., journey.InstanceId.RouteValues)` — passing `InstanceId.RouteValues` carries the `_jid` (and any scoping route values) across.

### 4. Render a question page

Each question page has a back link, a single question (its label or legend is the page heading), and a **Continue** button. Use `GetBackLink()` for the back link — it returns the previous step's URL, or `null` on the first step (link to your start page instead).

These views render the GOV.UK Design System components with the [GovUk.Frontend.AspNetCore](https://github.com/x-govuk/govuk-frontend-aspnetcore) tag helpers. Install that package, call `AddGovUkFrontend()` and `UseGovUkFrontend()` in `Program.cs`, and register its tag helpers in `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, GovUk.Frontend.AspNetCore
```

The name step uses the text input component. Binding it with `for` wires up the input's `id`, `name`, `value` and validation error message from the model:

```cshtml
@model NameViewModel
@{
    ViewData["Title"] = "What is your name?";
}

@if (ViewBag.BackLink is not null)
{
    <govuk-back-link href="@ViewBag.BackLink" />
}

<div class="govuk-grid-row">
    <div class="govuk-grid-column-two-thirds">
        <form method="post">
            <govuk-input for="Name" autocomplete="name" input-class="govuk-!-width-two-thirds">
                <govuk-input-label is-page-heading="true" class="govuk-label--l">What is your name?</govuk-input-label>
            </govuk-input>

            <govuk-button type="submit">Continue</govuk-button>
        </form>
    </div>
</div>
```

The date of birth step uses the date input component, with the fieldset legend as the page heading:

```cshtml
@model DateOfBirthViewModel
@{
    ViewData["Title"] = "What is your date of birth?";
}

@if (ViewBag.BackLink is not null)
{
    <govuk-back-link href="@ViewBag.BackLink" />
}

<div class="govuk-grid-row">
    <div class="govuk-grid-column-two-thirds">
        <form method="post">
            <govuk-date-input for="DateOfBirth">
                <govuk-date-input-fieldset>
                    <govuk-date-input-fieldset-legend is-page-heading="true" class="govuk-fieldset__legend--l">
                        What is your date of birth?
                    </govuk-date-input-fieldset-legend>
                    <govuk-date-input-hint>For example, 27 3 2007</govuk-date-input-hint>
                </govuk-date-input-fieldset>
            </govuk-date-input>

            <govuk-button type="submit">Continue</govuk-button>
        </form>
    </div>
</div>
```

Because each field binds with `for`, its validation error message is rendered from `ModelState` automatically, and an error summary is added to the top of the page when there are errors — so the `if (!ModelState.IsValid) return View(model);` checks in the controller are all that's needed to show GOV.UK-styled validation messages.

### 5. Check your answers and changing answers

On the *Check your answers* page, each answer has a **Change** link back to the question that set it. To make the user return to the check-answers page after changing an answer (rather than continuing through the rest of the journey), add a `returnUrl` to the change link:

```csharp
var changeNameUrl = Url.Action(
    nameof(Name),
    new
    {
        _jid = journey.InstanceId.Key,
        returnUrl = Url.Action(nameof(CheckAnswers), journey.InstanceId.RouteValues)
    });
```

When the user re-submits that page, `AdvanceTo` sees the `returnUrl` and redirects straight back to *Check your answers* instead of advancing to the next step. `GetBackLink()` honours it too, so the back link on the changed page also returns to *Check your answers*. The `returnUrl` must be a local URL.

### 6. Complete the journey

When the user submits the *Check your answers* page, save their answers wherever they need to go, then call `DeleteInstance()` to discard the journey state and redirect to a confirmation page.

The confirmation page must not require a journey instance, because the instance no longer exists once it has been deleted. Mark its action with `[ExcludeFromJourney]` so it can live in the same `[Journey]` controller as the rest of the journey:

```csharp
[HttpGet("confirmation")]
[ExcludeFromJourney]
public IActionResult Confirmation() => View();
```

`[ExcludeFromJourney]` opts a single action — or a whole controller — out of the journey named by its `[Journey]` attribute. The library skips its usual instance checks for that endpoint, so it's reachable without a `_jid`. Because the instance is gone, don't read `journey.State` (or any other coordinator member) from the confirmation action; if the page needs to show something the user entered, stash it before calling `DeleteInstance()` (in `TempData`, for example).

Alternatively, the confirmation page can sit in its own controller with no `[Journey]` attribute at all:

```csharp
using Microsoft.AspNetCore.Mvc;

[Route("apply/confirmation")]
public class ConfirmationController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
```

### Scoping a journey to a resource

The `[JourneyCoordinator]` attribute's second argument lists route values that scope an instance. For example, to run a separate journey per application `id`:

```csharp
[JourneyCoordinator("Apply", ["id"])]
public class ApplyJourneyCoordinator : JourneyCoordinator<ApplyState>;
```

```csharp
[Route("applications/{id}/apply")]
[Journey("Apply")]
public class ApplyController(ApplyJourneyCoordinator journey) : Controller { /* … */ }
```

Every route value you list must appear in the controller's route. Because you build step URLs from `journey.InstanceId.RouteValues`, the `id` is carried through the journey automatically alongside the `_jid`.

## Coordinator API

The most useful members of `JourneyCoordinator<TState>`:

| Member | Purpose |
| --- | --- |
| `State` | The current, deserialized state. Changes to it are not saved unless you go through `AdvanceTo`/`UpdateState`. |
| `AdvanceTo(url, updateState)` / `AdvanceToAsync(…)` | Save the state and move to the next step, returning a redirect. |
| `UpdateState(updateState)` / `UpdateStateAsync(…)` | Save the state without moving to a different step. |
| `GetBackLink()` | The previous step's URL (or the `returnUrl`), or `null` on the first step. |
| `GetCurrentStep()`, `GetStepUrls()`, `Path` | Inspect the journey path. |
| `InstanceId` | The instance identity. `InstanceId.Key` is the `_jid`; `InstanceId.RouteValues` includes it for URL generation. |
| `DeleteInstance()` | End the journey and discard its state. |

You can override `GetStartingState()` / `GetStartingStateAsync()` to set the initial state, and `OnInvalidStep()` to customise what happens when a user requests a step that isn't valid for their path (by default they are redirected to the last valid step).

## Storing journey state

State is saved through an `IJourneyStateStorage` implementation. The library ships with:

- **Session storage** (default) — stores state in the ASP.NET Core session. Requires `AddSession()` and `UseSession()`, as shown in [Getting started](#getting-started). The browser only ever holds the opaque `_jid` key.
- **Development storage** — stores state as JSON files on the local filesystem, so in-progress journeys survive app restarts while you are developing:

  ```csharp
  if (builder.Environment.IsDevelopment())
  {
      builder.Services.AddDevelopmentJourneyStateStorage();
  }
  ```

To store state somewhere else (a database or distributed cache, say), implement `IJourneyStateStorage` — with `GetState`, `SetState` and `DeleteState` — and register it before calling `AddGovUkQuestions`:

```csharp
builder.Services.AddSingleton<IJourneyStateStorage, MyJourneyStateStorage>();
builder.Services.AddGovUkQuestions();
```

## Configuring options

Pass a delegate to `AddGovUkQuestions` to configure options. `StateSerializerOptions` controls how state is serialized to and from JSON:

```csharp
builder.Services.AddGovUkQuestions(options =>
{
    options.StateSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

## Testing

Install the companion testing package:

```sh
dotnet add package GovUk.Questions.AspNetCore.Testing
```

`AddGovUkQuestionsTestingServices()` registers an in-memory state store and a `JourneyHelper` that builds a coordinator with pre-seeded state and a path, so you can test controller and coordinator logic without driving a full HTTP journey:

```csharp
using GovUk.Questions.AspNetCore.Testing;
using Microsoft.AspNetCore.Routing;

var journeyHelper = services.GetRequiredService<JourneyHelper>();

var coordinator = journeyHelper.CreateInstance<ApplyJourneyCoordinator>(
    routeValues: new RouteValueDictionary(),
    getState: _ => new ApplyState { Name = "Ada Lovelace" },
    pathUrls: ["/apply/name", "/apply/date-of-birth"]);

// Act on and assert against `coordinator` here.
```

Pass the journey's scoping route values in `routeValues` (an empty dictionary for an unscoped journey), the initial state from `getState`, and the URLs of the steps the user has visited in `pathUrls`. An async `CreateInstanceAsync` overload is also available.

## Building the library

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download). The repository uses [`just`](https://github.com/casey/just) as a command runner:

```sh
just build    # build the solution
just test     # run the tests
just format   # format the C# code
just pack     # create the NuGet packages
```

The equivalent `dotnet build`, `dotnet test`, `dotnet format` and `dotnet pack` commands work too.

## License

Licensed under the [MIT License](LICENSE).
