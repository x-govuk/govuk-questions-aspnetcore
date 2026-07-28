# Changelog

## Unreleased

Fixed the redirect that assigns an instance ID to a newly-started journey dropping the `returnUrl` query parameter. The redirect was built from the first step's normalized URL, which has `returnUrl` (and `_jid`) stripped to form the step's `StepId`, so linking into a journey's starting endpoint with a `returnUrl` lost it before the endpoint ever ran and `GetBackLink()` could never honour it. The redirect is now built from the requested URL with the instance ID appended.

## 1.0.1

Fixed `JourneyHelper.CreateInstance`/`CreateInstanceAsync` (in `GovUk.Questions.AspNetCore.Testing`) seeding path steps whose `StepId` included the `_jid` query parameter. The runtime derives the current step's `StepId` with `_jid` and `returnUrl` stripped, so seeded steps never matched and every seeded page was treated as an invalid step (issuing a redirect via `OnInvalidStep`) instead of rendering. The seeded `pathUrls` are now normalized the same way the runtime normalizes request URLs, so plain page URLs can be passed directly.

## 1.0.0

Initial release.
