# ADR 0002: Isolate ImageSharp and Archive Details Behind App Modules

## Status

Accepted

## Context

Image conversion depends on ImageSharp encoders, format metadata, image resize behavior, and ZIP packaging. Those details are useful implementation choices, but they should not define the UI contract. The UI needs stable app-level concepts: supported formats, encoder settings, previews, converted files, and packages.

## Decision

Keep ImageSharp and archive details behind narrow modules:

- `IFormatCatalog` owns supported format descriptors, default target format, accept strings, and output file naming.
- `IImageFormatEncoderFactory` maps app-level `EncoderSettings` and `FormatId` values to ImageSharp encoders.
- `IImagePreviewBuilder` owns thumbnail and preview data URL generation.
- `IImagePackageBuilder` owns ZIP archive creation from converted image streams.
- `ImageDocument` may hold the loaded ImageSharp image internally, but it is not part of the UI contract.

Razor components edit app-level settings and display app-level snapshots/results. They do not construct ImageSharp encoders, inspect ImageSharp formats, resize images, or write ZIP archives directly.

Browser download remains an adapter responsibility because it depends on JavaScript interop. The app may return streams or packages to that adapter, and the adapter is responsible for invoking the browser download path.

## Consequences

- Format discovery, output naming, encoder construction, preview generation, and package creation can be tested without rendering Razor components.
- Adding or changing ImageSharp package versions should mainly affect the format catalog, encoder factory, preview builder, or image document implementation.
- Razor option components should emit `EncoderSettings` records rather than ImageSharp encoder instances.
- Preview UI should consume `CreatePreviewResult`, including loading/error outcomes, rather than hiding failures as `null`.
- Package behavior such as ZIP entries, empty selections, and stream positioning belongs in `IImagePackageBuilder` tests.

## Rejected Alternatives

- Let UI components pass ImageSharp encoders through dynamic Razor lifecycle state.
- Let image cards generate thumbnails or large previews directly.
- Let Razor write ZIP archives manually.
- Treat supported format lists, accept strings, and output naming as separate UI constants.
