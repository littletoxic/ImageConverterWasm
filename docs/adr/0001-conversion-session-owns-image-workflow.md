# ADR 0001: Conversion Session Owns Image Workflow

## Status

Accepted

## Context

The original Blazor page mixed browser event handling, mutable item state, image loading, conversion, preview, download eligibility, batch progress, and disposal rules. That made small UI changes risky because workflow state and rendering state were the same thing.

The project now targets .NET 11 preview and uses C# union declarations, so workflow commands can return precise app-level outcomes instead of temporary enum/status patterns.

## Decision

Use `IConversionSession` as the app-level workflow boundary for the in-browser conversion session.

The Conversion Session owns:

- Added image items and their state transitions.
- Loading, pending, converting, done, and error states.
- Current target format ID and encoder settings.
- Target-format changes, including converted-result invalidation.
- Single-image conversion.
- Batch conversion and progress state.
- Preview command exposure.
- Converted result stream lookup.
- Remove, clear, and disposal rules.
- Read-only `ConversionSessionSnapshot` and `ImageItemSnapshot` values for the UI.
- Command-specific union result types for expected workflow outcomes.

Blazor owns:

- Browser file input, drag/drop, and event wiring.
- Mapping `IBrowserFile` to app-level `BrowserImageFile`.
- Calling session commands with item IDs.
- Rendering MudBlazor UI from snapshots and command results.
- JavaScript download interop and JS module disposal.

All mutation goes through commands. The UI renders snapshots and passes item IDs back to commands.

## Consequences

- Workflow tests can exercise state transitions without rendering Razor components.
- Late implementation slices should extend the session boundary or add narrow application modules behind it instead of moving rules back into the page.
- Command results are command-specific unions, such as `CreatePreviewResult`, `ConvertImageResult`, and `OpenConvertedImageResult`.
- Expected failures are represented as union cases. Unexpected exceptions are caught at the workflow boundary where practical, reflected into item state when appropriate, logged, and returned as failure cases.
- UI-facing state and results stay app-level and must not expose ImageSharp types, `IBrowserFile`, `ImageDocument`, or mutable item objects.

## Rejected Alternatives

- Keep mutable image item objects in Razor and let child components call image-processing methods.
- Use one generic command result enum for all operations.
- Let Blazor own batch conversion counters and eligibility rules.
- Let target-format changes leave stale converted streams attached to items.
