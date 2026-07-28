# Changelog

## 1.0.3

Fixes `AdvanceTo` redirecting to a `returnUrl` that isn't a step in the journey. A `returnUrl` provided when a journey is started says where to go once the journey is over, so honouring it on the first advance ended the journey before it began. It's now only honoured when it points at a step in the journey path.

`AdvanceTo` also now ignores `returnUrl` when called with `SetAsFirstStep` or `SetAsLastStep`, since those reshape the journey path around the new step.

Allows `State` and `Path` to be read after an instance has just been completed.

## 1.0.2

Fixes journeys that are started with a URL that includes a `returnUrl` query parameter.

## 1.0.1

Fixed `JourneyHelper.CreateInstance`/`CreateInstanceAsync` (in `GovUk.Questions.AspNetCore.Testing`) seeding path steps whose `StepId` included the `_jid` query parameter. The runtime derives the current step's `StepId` with `_jid` and `returnUrl` stripped, so seeded steps never matched and every seeded page was treated as an invalid step (issuing a redirect via `OnInvalidStep`) instead of rendering. The seeded `pathUrls` are now normalized the same way the runtime normalizes request URLs, so plain page URLs can be passed directly.

## 1.0.0

Initial release.
