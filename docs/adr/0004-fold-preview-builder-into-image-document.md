# ADR 0004: Fold Preview Builder into Image Document

## Status

Accepted

## Context

ADR-0002 placed thumbnail and large-preview generation behind `IImagePreviewBuilder`. The interface accepted an `ImageDocument` and reached into its `internal Image LoadedImage` to clone, resize, and encode. Two observations made the seam pay back less than its overhead.

- The signature already required `ImageDocument`. Any second implementation would still need to construct a real `ImageDocument` and access its ImageSharp `Image`. The seam was structural but semantically un-substitutable — there is no realistic second implementation distinct from "use ImageSharp on the loaded image."
- The Conversion Session called the builder in two places: `LoadImageAsync` to produce a thumbnail and `CreatePreview` to produce the on-demand large preview. Both passed the same `item.Job` through. The builder was a thin function in a separate file, not a module with its own state.

`ImageDocument` already encapsulates "the loaded ImageSharp image plus its conversion behavior" and exposes only app-level types (`FileName`, `Width`, `Height`, `IsLoaded`, `ConvertAsync`). Adding preview generation to it concentrates "things one can do with a loaded image" in one place.

Separately, `ImageDocument` previously took `targetFormatId` and `IFormatCatalog` in its constructor so that `ConvertAsync` could call `catalog.GetOutputFileName(...)` at the end. The Conversion Session already owned `_targetFormatId` as the source of truth and re-set the document's mirror in two places (`SetTargetFormat` and the start of `ConvertImageAsync`), creating double ownership for a single piece of state.

## Decision

Remove `IImagePreviewBuilder` and `ImageSharpImagePreviewBuilder`. `ImageDocument` gains a `CreatePreview(int maxSize) → string` method and a `ThumbnailMaxSize` public constant. Output filenaming moves out of `ImageDocument` and into the Conversion Session.

- `Models/Imaging/ImagePreviewBuilder.cs` is deleted. The `internal Image LoadedImage` accessor on `ImageDocument` is also gone — no caller needs it.
- `ImageDocument`'s constructor takes only `string fileName`. It does not hold `TargetFormatId` or `IFormatCatalog`.
- `ImageDocument.ConvertAsync(IImageEncoder)` returns a new `EncodedImage(MemoryStream Stream, long Size)` record — no output filename. The session computes the filename via `formatCatalog.GetOutputFileName(item.File.FileName, _targetFormatId)` after the convert succeeds and stores it on the session item alongside the encoded bytes.
- The Conversion Session no longer injects `IImagePreviewBuilder`. `LoadImageAsync` calls `item.Job.CreatePreview(ImageDocument.ThumbnailMaxSize)`; the `CreatePreview` command calls `item.Job.CreatePreview(maxSize)`.
- The Razor side is unchanged: cards still ask the Conversion Session for `CreatePreviewResult` (a command-specific union); downloads still go through `OpenConvertedImage` / `BuildConvertedPackageAsync` returning filename + stream.

This reverses one bullet of ADR-0002's decision list (the dedicated preview builder module) but does not relax its broader rule: ImageSharp types remain inside app-level modules and are not exposed to Razor or to command results.

## Consequences

- The `Models/Imaging/` namespace shrinks to a single file. "Things one can do with a loaded image" — load, convert, preview, dispose — live together.
- `ImageDocument` no longer has the dual-ownership mirror of the target format. The session is the sole holder, and `SetTargetFormat` does not have to walk every document to keep the mirrors in sync.
- A future preview backend (e.g. a worker-side resizer) would have to be introduced by adding a method to `ImageDocument` or splitting it. That cost is acceptable; the previous seam did not enable a real alternative either, since its signature was already ImageSharp-bound.
- Tests that previously had to mock `IImagePreviewBuilder` now call `ImageDocument.CreatePreview` against a loaded image. The test surface is narrower and closer to real behavior.

## Rejected Alternatives

- Keep `IImagePreviewBuilder` but drop `ImageDocument` from its signature, taking `Stream` or `byte[]` instead. Forces the builder to re-decode the image, duplicating ImageSharp work that `ImageDocument` already did during `LoadAsync`.
- Move preview generation into a static helper invoked from the session. Replaces the seam with a free function operating on `ImageDocument` internals — the same problem in a different shape.
- Have `LoadAsync` also produce the thumbnail and store it on `ImageDocument`. Couples a derived view (the thumbnail data URL) to the document's lifecycle and forces every caller, including any future "load without thumbnail" path, to pay for it.
- Keep `ImageDocument`'s `TargetFormatId` and `IFormatCatalog` so that `ConvertAsync` can return a fully-named result. Keeps the seam tidy at the cost of double-owned state and two synchronization sites in the session.
