# Image Converter Context

This app is a Blazor WebAssembly image format converter. Users add image files, the app loads them in the browser, shows thumbnails and metadata, converts them to a selected target format, and downloads one converted file or a ZIP package.

## Vocabulary

- **Conversion Session**: The application workflow boundary for the current browser session. It owns image items, loading/conversion state, target format, encoder settings, batch progress, preview commands, converted-result lookup, removal, clearing, and disposal of session-owned resources.
- **Blazor Adapter**: Razor components and pages that handle browser events, MudBlazor rendering, `IBrowserFile` mapping, JavaScript download interop, and JS module disposal. Blazor calls application commands and renders snapshots; it should not own image workflow rules.
- **Image Item**: One uploaded image inside the Conversion Session. The UI sees it as an `ImageItemSnapshot`, not as a mutable image-processing object.
- **Image Document**: The loaded ImageSharp image plus conversion behavior for one item. This is internal model state and is not exposed to Razor components or command results.
- **Format Catalog**: The app-level catalog of supported formats. It owns format descriptors, upload accept strings, default target format, output file extensions, and output file naming.
- **Format ID**: Stable app-level identifier for a supported format, such as `png`, `jpeg`, or `webp`. UI and workflow code use format IDs instead of ImageSharp format objects.
- **Encoder Settings**: App-level data records edited by Razor option components. Each settings type knows how to build its ImageSharp encoder. The Conversion Session always holds a non-null instance and folds its metadata-skip preference in.
- **Preview Builder**: The image-work module that creates thumbnail and large-preview data URLs. Cards ask for preview command results; they do not run ImageSharp resize logic.
- **Package Builder**: The archive module that creates ZIP packages from converted image streams. Browser download remains a Blazor adapter concern.
- **Snapshot**: Read-only UI state returned by the Conversion Session. Snapshots are safe for rendering and should not expose mutable workflow objects.
- **Command Result**: A command-specific C# union result that represents expected workflow outcomes, such as success, item not found, item not loaded, empty package, or command failure.

## Current Workflow Boundaries

The Conversion Session owns workflow state and workflow decisions:

- Adding app-level `BrowserImageFile` values.
- Loading images and producing initial thumbnails.
- Tracking item status: loading, pending, converting, done, and error.
- Tracking target format, encoder settings, and the metadata-skip preference.
- Invalidating converted results when the target format changes.
- Converting one image or all eligible images.
- Reporting batch conversion progress.
- Creating preview command results through the Preview Builder.
- Opening converted result streams for browser adapters.
- Removing items, clearing the session, and disposing image/result resources.

Blazor owns browser integration:

- File picker and drag/drop events.
- Mapping `IBrowserFile` into `BrowserImageFile`.
- Calling Conversion Session commands.
- Rendering MudBlazor UI from snapshots and command results.
- Calling JavaScript download interop.
- Disposing JavaScript module resources.

ImageSharp details stay behind app-level modules. UI-facing snapshots and command results must not expose `ImageDocument`, `IBrowserFile`, `IImageFormat`, `IImageEncoder`, ImageSharp `Image`, or mutable item state.

## Rejected Directions

- Do not make Razor components own conversion workflow state with mutable `ImageItem` objects.
- Do not return one generic enum or boolean from commands. Use command-specific union results.
- Do not expose ImageSharp types through UI-facing snapshots or command result cases.
- Do not put resize/preview generation in image cards.
- Do not create ZIP archives directly in Razor code. Use the Package Builder and keep JavaScript download as the browser adapter.
- Do not make the Format Catalog a UI helper only; output naming and accept strings belong there too.
