# Changelog

## Unreleased

Fixed `JourneyHelper.CreateInstance`/`CreateInstanceAsync` (in `GovUk.Questions.AspNetCore.Testing`) seeding path steps whose `StepId` included the `_jid` query parameter. The runtime derives the current step's `StepId` with `_jid` and `returnUrl` stripped, so seeded steps never matched and every seeded page was treated as an invalid step (issuing a redirect via `OnInvalidStep`) instead of rendering. The seeded `pathUrls` are now normalized the same way the runtime normalizes request URLs, so plain page URLs can be passed directly.

## 1.0.0

Initial release.
