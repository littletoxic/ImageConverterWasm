# ADR 0003: Collapse Encoder Factory into Encoder Settings

## Status

Accepted

## Context

ADR-0002 introduced `IImageFormatEncoderFactory` plus an `IImageFormatEncoder` wrapper as the seam between app-level `EncoderSettings` and ImageSharp's `IImageEncoder`. Two issues surfaced as the codebase grew:

- The wrapper `IImageFormatEncoder` exposed `SixLabors.ImageSharp.Image` in its signature and reduced to a single `image.SaveAsync(stream, inner)` line — it abstracted nothing. The "seam" was real in structure but had no second implementation and could not have one without constructing a real `ImageDocument`.
- The factory was a 56-line switch-on-type dispatch. Every new field on a settings record required a parallel edit. The factory accepted both an external `FormatId` and `EncoderSettings.FormatId`, and when they disagreed it silently fell back to the catalog's default encoder — a quiet path that could mask real bugs.

Separately, `SkipMetadata` leaked across three layers: `ConversionPanel._stripMetadata`, an `EncoderOptionsBase.SkipMetadata` parameter, and a field on every `EncoderSettings` record. The three were kept in sync by a fragile first-render dance using `_initialized` and `_lastSkipMetadata` flags on the component base.

The Razor side also picked components via reflection (`[FormatEncoder]` attribute + `Dictionary<FormatId, Type>` map) and `DynamicComponent` with a `Dictionary<string, object>` parameter bag, which hid the component map from the compiler, jump-to-definition, and AI navigation.

## Decision

Fold encoder construction into `EncoderSettings` and lift `SkipMetadata` to the Conversion Session.

- `EncoderSettings` becomes an abstract record with `SkipMetadata { get; init; }` and an abstract `IImageEncoder BuildEncoder()`. Each format-specific record overrides `BuildEncoder()` to construct its matching ImageSharp encoder. `EncoderSettings.Default(FormatId)` is the single source of per-format defaults.
- `IImageFormatEncoderFactory`, `ImageSharpEncoderFactory`, `IImageFormatEncoder`, and `ImageSharpFormatEncoder` are deleted. `IFormatCatalog.GetEncoder` returns `IImageEncoder` directly. `ImageDocument.ConvertAsync` consumes `IImageEncoder`.
- The Conversion Session keeps a non-null `_encoderSettings`. `SetTargetFormat` resets it to `EncoderSettings.Default(formatId) with { SkipMetadata = _skipMetadata }`. A new `SetSkipMetadata(bool)` command handles the global preference and re-applies it to the active settings via `with`.
- Razor option components no longer know about `SkipMetadata`. `ConversionPanel` renders it as a single `MudSwitch` at the panel level and reports changes to `Home`, which routes them to the session. The DynamicComponent + reflection registry + dictionary parameters are replaced with a static `@switch` over `FormatId`.
- `EncoderOptionsBase` shrinks to 13 lines: a callback parameter, an abstract `BuildSettings()` method, and a `NotifySettingsChanged()` helper. No lifecycle hooks, no synchronization flags.

## Consequences

- Adding a new format means adding a settings record (with `override BuildEncoder()`), the matching `*EncoderOptions.razor`, an entry in `EncoderSettings.Default`, and a case in `ConversionPanel`'s `@switch`. All sites are statically discoverable.
- `EncoderSettings.FormatId` is gone. The session's `_targetFormatId` is the sole source, and mismatch between target format and active settings is structurally prevented by resetting settings on `SetTargetFormat`.
- `EncoderSettings` records reference ImageSharp encoder types in their `BuildEncoder` overrides. They are app-level inputs to commands, not snapshots or command results, so this does not relax ADR-0002's UI-facing restrictions.
- `SkipMetadata` survives format changes because it lives on the session, not on per-format Razor state.
- Tests can construct any `EncoderSettings` and call `BuildEncoder()` directly, with no Razor rendering and no factory wiring.

## Rejected Alternatives

- Keep `IImageFormatEncoderFactory` and only delete `IImageFormatEncoder`. Retains the switch-on-type dispatch and the dual-`FormatId` silent fallback path.
- Mirror every ImageSharp encoder enum into app-level enums to keep ImageSharp out of `EncoderSettings`. Doubles the surface for every new option with no testability gain, since the enums are stable and the boundary already passes through Razor option components.
- Render `EncoderOptions` via `DynamicComponent` and reflection registration. Hides the component map from the compiler, IDE, and AI navigation; trades static traceability for one fewer line of `@switch`.
- Keep `SkipMetadata` on each settings record and propagate via parameter to children. Requires a lifecycle dance to suppress duplicate notifications during first render and resets `SkipMetadata` when the format changes.
