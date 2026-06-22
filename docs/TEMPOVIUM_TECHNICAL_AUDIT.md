# Tempovium Technical Audit

This report is based only on the repository contents inspected in this audit. No implementation changes are included here.

## Verification Snapshot

- `dotnet build Tempovium.sln` succeeds after NuGet restore.
- `dotnet test Tempovium.sln --no-build` passes, but the suite contains only one empty placeholder test.
- Build warnings include high-severity NuGet vulnerability warnings for transitive packages `SQLitePCLRaw.lib.e_sqlite3` and `Tmds.DBus.Protocol`, plus small compiler warnings around unused events and nullable native handle metadata.
- The repository has no `docs/` folder before this report, no publish scripts, no installer scripts, and no CI configuration.
- `tempovium.db-shm`, `tempovium.db-wal`, and Xcode `xcuserdata` files are tracked in Git.

## A. Current Architecture Summary

### Projects and Responsibilities

| Area | Responsibility | Notes |
| --- | --- | --- |
| `src/Tempovium.App` | Avalonia desktop app, DI startup, navigation, views, viewmodels, playback UI, timeline coordination. | Main executable. Currently references the macOS media project directly. |
| `src/Tempovium.Core` | Domain entities, simple app services, repository/service interfaces, media file type detection. | Mostly platform-neutral. It also contains UI-adjacent state services such as `NavigationService` and `SelectedMediaService`. |
| `src/Tempovium.Infrastructure` | EF Core SQLite persistence, repositories, file hashing, folder import, infrastructure DI. | Uses a relative SQLite path: `Data Source=tempovium.db`. |
| `src/Tempovium.Media.Abstractions` | Shared media backend contracts and backend kind enum. | Good boundary, but currently too small for native host lifecycle and async seek/load behavior. |
| `src/Tempovium.Media.Mac` | macOS media backend using Avalonia `NativeControlHost` and P/Invoke into `TempoviumMacBridge`. | macOS-specific implementation. Exposes `MacMediaBackend` and `MacVideoHost`. |
| `src/Tempovium.Api` | ASP.NET template API. | Contains weather forecast sample code. Not integrated with the desktop app. |
| `native/macos/TempoviumMacBridge` | Swift/AppKit/AVFoundation dynamic library project. | Builds `libTempoviumMacBridge.dylib`; no .NET publish integration is present. |
| `tests/Tempovium.Tests` | xUnit test project. | References only `Tempovium.Core`; contains only an empty `UnitTest1`. |

### Project Dependencies

- `Tempovium.App` -> `Tempovium.Core`, `Tempovium.Infrastructure`, `Tempovium.Media.Abstractions`, `Tempovium.Media.Mac`.
- `Tempovium.Infrastructure` -> `Tempovium.Core`.
- `Tempovium.Media.Mac` -> `Tempovium.Media.Abstractions`, `Avalonia`.
- `Tempovium.Api` -> `Tempovium.Core`, `Tempovium.Infrastructure`.
- `Tempovium.Tests` -> `Tempovium.Core`.
- The native macOS bridge is outside the .NET solution and is loaded by name at runtime through `DllImport("TempoviumMacBridge")`.

### Good Boundaries

- Domain entities are simple and platform-neutral: `User`, `MediaItem`, `MediaNote`.
- Repository interfaces keep EF Core out of the app layer.
- `IMediaBackend` gives the app a first abstraction over playback.
- `MediaFileTypeDetector` keeps supported extension logic out of import UI code.
- EF Core configurations define useful indexes, including `MediaItems(UserId, FileHash)` and `MediaNotes(MediaItemId, TimestampSeconds)`.

### Leaks and Tight Coupling

- `Tempovium.App` directly references `Tempovium.Media.Mac`.
- `Program.cs` always registers `MacMediaBackend` for `IMediaBackend`.
- `MediaPlayerViewModel` exposes `MacBackend => _mediaBackend as MacMediaBackend`.
- `MediaPlayerView.axaml` directly imports `Tempovium.Media.Mac` and instantiates `mac:MacVideoHost`.
- `MacVideoHost` hardcodes an `NSView` native handle descriptor.
- View constructors pull services from `Program.AppHost`, which couples views to the global composition root.
- `NotesPanelView` and `MediaPlayerView` rely on code-behind event handlers for app actions.
- App startup and infrastructure both register some of the same singleton services.
- `PlaybackControlService.SeekRequested` is emitted by notes but is not subscribed anywhere in the inspected code.

## B. Windows/macOS Compatibility Audit

### Current Windows Blockers

- The app registers `MacMediaBackend` on every OS. On Windows, the first construction of that backend will try to call the macOS native library through P/Invoke and should fail with a missing library/runtime error.
- The player XAML directly depends on `MacVideoHost`; there is no Windows native video host.
- There is no `Tempovium.Media.Windows` project and no Windows playback implementation.
- The app project copies macOS media assemblies into the app output because of the direct project reference.
- Native media hosting is modeled as an `NSView`, not an OS-neutral native surface.
- No Windows packaging script exists for runtime identifiers, native dependencies, app data paths, icons, signing, or installer output.
- Database storage uses the process working directory, which is fragile for installed desktop apps.

### Current macOS-Specific Dependencies

- `Tempovium.Media.Mac` uses `DllImport("TempoviumMacBridge")`.
- `MacNative` exports map to C symbols implemented by Swift.
- `MacMediaBackend` wraps AVFoundation state and events.
- `MacVideoHost` returns an Avalonia `PlatformHandle` with descriptor `NSView`.
- `native/macos/TempoviumMacBridge` uses AppKit, AVKit, and AVFoundation.
- The Xcode project builds a dynamic library named `libTempoviumMacBridge.dylib`.
- The Xcode project has `MACOSX_DEPLOYMENT_TARGET = 26.0`, `SWIFT_VERSION = 6.0`, automatic code signing, and `SKIP_INSTALL = YES`.

### Required Cross-Platform Shape

- The app layer should depend on media abstractions, not concrete platform media projects.
- Backend selection should happen in the composition root based on the current OS.
- Platform-specific native host controls should be behind a shared app-level surface contract or selected through OS-specific templates.
- The app should have an unsupported-media backend for platforms without an implementation, so Windows startup can be validated before Windows playback is complete.
- Database and downloaded media paths should move to per-user app data folders, not the working directory.
- Publish output should be RID-specific so Windows builds do not ship macOS-only native assets and macOS builds include the dylib.

### Backend Selection Strategy

Use one composition-root method, for example `AddMediaBackendForCurrentPlatform(IServiceCollection services)`, with this behavior:

- `OperatingSystem.IsMacOS()` registers `MacMediaBackend` and the macOS host adapter.
- `OperatingSystem.IsWindows()` registers a future `WindowsMediaBackend` and Windows host adapter.
- Otherwise register `UnsupportedMediaBackend`, which implements `IMediaBackend`, reports `IsLoaded = false`, and raises a clear failure message when asked to load media.

This keeps platform selection out of viewmodels. The first version can be simple OS checks; no plugin loader is needed yet.

### Media Project Recommendation

Yes, Tempovium should keep `Tempovium.Media.Mac`, add `Tempovium.Media.Windows`, and keep shared contracts in `Tempovium.Media.Abstractions`.

Practical split:

- `Tempovium.Media.Abstractions`: playback contract, backend info, native host contract if needed, media state models, seek/load result types.
- `Tempovium.Media.Mac`: AVFoundation backend, macOS native host, dylib loader/copy assumptions.
- `Tempovium.Media.Windows`: Windows backend, likely Media Foundation or a controlled native helper, Windows native host.
- `Tempovium.App`: app shell, viewmodels, and UI selection. It should not cast to `MacMediaBackend`.

The short path is not a plugin system. Start with OS-aware DI and one unsupported backend; add a real Windows backend only after startup is clean on Windows.

### Avalonia Native-Hosting Concerns

- `NativeControlHost` creates real platform child controls. Overlays, clipping, z-order, focus, DPI, and transparency can behave differently from pure Avalonia controls.
- macOS must create and manipulate AppKit/AVFoundation objects on the main thread. The Swift bridge already uses `MainActor`, which is the right direction.
- Windows will need an `HWND`-based host or equivalent. Media Foundation usually needs careful COM initialization, handle recreation handling, resize handling, and UI-thread ownership.
- Both platforms need explicit create/destroy lifecycle handling when Avalonia recreates native controls.
- macOS publishing must copy, sign, and notarize `libTempoviumMacBridge.dylib` inside the app bundle.
- Windows publishing must include any native DLLs or external media runtime dependencies next to the app or in a known app-local folder.

## C. Media Playback/Timeline Audit

### Current Playback Flow

1. `LibraryViewModel.SelectedMedia` writes to `SelectedMediaService.SelectedMedia`.
2. `MediaPlayerViewModel` observes `SelectedMediaService`.
3. `ApplySelectedMedia` calls `_mediaBackend.Load(media.FilePath)`.
4. `MacMediaBackend.Load` calls the Swift bridge to load an `AVPlayerItem`.
5. `MediaPlayerViewModel` starts a 200 ms polling loop.
6. Each poll calls `_mediaBackend.UpdateState()`.
7. The backend reads native position, duration, and readiness.
8. `PlaybackTimelineService.ApplyBackendState` updates playback, display position, duration, and playing state.
9. `MediaOpened` is raised once after native readiness is observed.
10. Video currently auto-plays on open; audio becomes ready for manual play.

### Current Seek Flow

- Pointer press on the audio timeline calls `BeginUserSeek`, suspends polling, and starts showing user-controlled display time.
- Slider value changes call `UpdateUserSeek`.
- Pointer release calls `SeekToAsync`.
- `SeekToAsync` clamps the target, commits it in `PlaybackTimelineService`, calls backend `Seek`, and starts a 450 ms delay before polling resumes.
- The Swift bridge keeps a `pendingSeekSeconds` value and reports the target as effective position until the native player settles within 0.35 seconds.

### Desynchronization Risks

- Notes jump does not work yet because `PlaybackControlService.SeekRequested` has no subscriber.
- Seek completion is timer-based in the viewmodel, not acknowledged by the backend contract.
- The Swift bridge masks position during a pending seek; useful, but if a seek fails or stalls the UI can keep showing the target.
- `IMediaBackend.Seek` is void, so callers cannot know whether the seek was accepted, completed, or rejected.
- Polling overwrites are mostly guarded by `IsUserSeeking` and `IsSeekPending`, but these guards live above a backend that has no real seek state.
- `MacMediaBackend.IsPlaying` is maintained by C# calls, not by querying AVPlayer state.
- `MediaEnded` exists but is never raised.
- `Stop` pauses and raises a zero position event, but does not call native seek-to-zero.
- `PositionChanged` is raised from the backend but the viewmodel mainly uses polling.
- The timeline slider is currently inside the audio UI branch; video has no equivalent visible timeline in `MediaPlayerView.axaml`.
- Console logging inside 200 ms polling is too noisy for production and can hide timing problems.

### Safe Fix Plan

1. Add focused tests for `PlaybackTimelineService`: user seek blocks polling overwrite, pending seek holds target, pending clears near target, clamp and sanitize behavior.
2. Wire `PlaybackControlService.SeekRequested` to `MediaPlayerViewModel.SeekToAsync`, or delete the event service and call a single playback controller directly.
3. Add backend-level seek acknowledgement, for example `Task SeekAsync(TimeSpan position, CancellationToken ct)` or a `SeekCompleted` event.
4. Keep polling as a fallback, but let backend seek completion clear pending seek state.
5. Make audio and video use the same timeline control.
6. Add real `MediaEnded` handling and native stop-to-zero behavior.
7. Replace polling `Console.WriteLine` calls with optional logging after the timing bugs are fixed.

## D. Notes and Teacher Workflow Audit

### Current Notes Behavior

- `MediaNote` stores `UserId`, `MediaItemId`, `TimestampSeconds`, `Content`, `CreatedAt`, and optional `UpdatedAt`.
- Notes are loaded by media and ordered by timestamp.
- Adding a note uses the current display position, trims content, saves to the repository, and inserts in timestamp order.
- Active note is the latest note with timestamp less than or equal to the current playback display time.
- Notes can be deleted.
- Jump-to-note is modeled but not functional yet because the seek event has no subscriber.

### Teacher Workflow Fit

The current model is a good base for teachers because a timestamped note can represent:

- A discussion prompt tied to a video moment.
- A lecture explanation.
- A question to ask in class.
- A reminder to pause playback.
- A content warning or context note.
- A quick chapter marker.

The model is not yet enough for professional classroom use because notes are isolated to a media item and have no course, class, lesson, export, or presentation context.

### Missing Teacher Features

- Edit notes after creation.
- Working jump-to-note.
- Search within notes and across a library.
- Tags or note types such as question, explanation, activity, assignment, warning.
- Courses/classes and playlists.
- Lesson plans that combine multiple videos/resources.
- Class mode with distraction-free playback and selected notes.
- Export to Markdown, PDF, CSV, or lesson handouts.
- Keyboard shortcuts for add note, pause, seek back, seek forward.
- Playback speed and resume position.
- Attachments/resources per media item.
- Transcript/caption support.
- Bulk import organization.
- Missing-file repair and relink flow.

### Teacher-Focused Roadmap

Phase 1: make current notes dependable.

- Wire jump-to-note.
- Add edit note.
- Add search/filter in current media notes.
- Add export notes to Markdown.
- Add playback shortcuts.
- Save last playback position per media item.

Phase 2: organize teaching material.

- Add courses/classes.
- Add playlists or lesson collections.
- Add note tags/types.
- Add resource links/files per media item.
- Add library search by title, path, type, tag, and course.

Phase 3: classroom use.

- Add class mode with player-first layout, selected notes, and hidden library clutter.
- Add pause prompts and discussion markers.
- Add printable/exportable lesson notes.
- Add projector-safe light/dark mode.

Phase 4: advanced teaching aids.

- Add captions/transcripts if available.
- Add transcript-linked notes.
- Add worksheet/export templates.
- Consider LMS export only after the core workflow is stable.

## E. Library/Import Audit

### Current Local Import Flow

- `LibraryViewModel` opens Avalonia's folder picker.
- The selected folder path is passed to `IMediaImportService.ImportFolderAsync`.
- `MediaImportService` recursively scans all files with `Directory.GetFiles`.
- Supported extensions are detected by `MediaFileTypeDetector`.
- Supported files are hashed with SHA-256.
- Existing media is checked by hash.
- New media is saved with title, absolute file path, hash, type, zero duration, and creation time.
- The library reloads from `IMediaRepository.GetByUserAsync`.

### Duplicate Detection

- The database has a unique index on `(UserId, FileHash)`.
- The repository method used during import is `GetByHashAsync(hash)`, which is not user-scoped.
- That means user B can be blocked from importing a file already imported by user A, even though the schema allows per-user duplicates.
- `MediaImportResult` exists but the import interface returns only `List<MediaItem>`, so duplicate/unsupported/missing counts are not surfaced.

### Selection and Loading

- Selecting a library item updates `SelectedMediaService`.
- `MediaPlayerViewModel` loads the absolute file path immediately.
- Missing files are detected only when the backend load fails.
- No metadata extraction runs during import, so `DurationSeconds` stays zero until playback backend state is available.

### Large Library Improvements

- Switch from `Directory.GetFiles` to streaming enumeration when adding progress/cancellation.
- Add cancellation and progress reporting to import.
- Scope duplicate checks to user, or make global dedupe explicit in the data model.
- Batch repository writes instead of saving once per file.
- Use file size and last modified time as a cheap prefilter before hashing known files.
- Persist import status: missing, available, moved, downloaded, external.
- Add an import result model to the UI.
- Add `ImportFileAsync` for direct single-file import from downloads.
- Add search/filter/sort and avoid replacing large lists all at once.
- Add relink/repair flow for moved files.
- Store app data under platform app data folders.

## F. yt-dlp Integration Plan

Do not implement yt-dlp until media backends, import, and app data paths are stable.

### Proposed Module Shape

Start with contracts and a controlled process implementation:

- `IVideoDownloadService`
- `IDownloadJobStore`
- `IDownloadToolLocator`
- `IExternalProcessRunner`
- `YtDlpDownloadService`
- `YtDlpProcessRunner`
- `FfmpegLocator`
- `DownloadImportService`

Keep this simple at first. A separate `Tempovium.Downloads.Abstractions` and `Tempovium.Downloads.YtDlp` split is reasonable when the downloader work starts. Before then, a small contracts file and implementation under infrastructure is enough.

### Models

- `DownloadRequest`
  - `Uri SourceUri`
  - `Guid UserId`
  - `Guid? CourseId`
  - `string? PreferredTitle`
  - `DownloadMediaKind MediaKind`
  - `DownloadQualityPreference Quality`
  - `bool ImportWhenComplete`
  - `string DestinationDirectory`
- `DownloadJob`
  - `Guid Id`
  - `DownloadStatus Status`
  - `DownloadRequest Request`
  - `DateTime CreatedAt`
  - `DateTime? CompletedAt`
- `DownloadProgress`
  - `Guid JobId`
  - `double? Percent`
  - `long? DownloadedBytes`
  - `long? TotalBytes`
  - `double? SpeedBytesPerSecond`
  - `TimeSpan? Eta`
  - `string Stage`
- `DownloadResult`
  - `bool Success`
  - `string? FilePath`
  - `string? Title`
  - `TimeSpan? Duration`
  - `DownloadError? Error`
- `DownloadError`
  - `DownloadErrorKind Kind`
  - `string Message`
  - `int? ExitCode`
  - `string? ToolOutput`

### Service Contract

Use an async API with progress and cancellation:

```csharp
Task<DownloadResult> DownloadAsync(
    DownloadRequest request,
    IProgress<DownloadProgress> progress,
    CancellationToken cancellationToken);
```

Cancellation should kill the process tree and mark the job as cancelled. It should not leave partial files imported into the library.

### Safe Process Execution

- Never invoke a shell.
- Use `ProcessStartInfo` with `UseShellExecute = false`.
- Use `ProcessStartInfo.ArgumentList`; do not concatenate command strings.
- Validate the URL as a URL, not as a command fragment.
- Generate output paths inside an app-controlled download directory.
- Sanitize file names and reject path traversal.
- Prefer app-controlled yt-dlp options; expose only a small set of safe user choices.
- Capture stdout/stderr asynchronously with bounded logs.
- Parse machine-readable progress where possible.
- Treat nonzero exit codes as structured errors.

### FFmpeg Handling

- Detect whether FFmpeg is bundled or configured by the user.
- Pass `--ffmpeg-location` as a single argument when needed.
- Verify the binary exists and can report a version.
- Keep FFmpeg optional for formats that do not require merging.
- Show a clear missing-FFmpeg error before starting downloads that need it.

### Direct Import

- Download into a staging directory.
- On success, call a future `IMediaImportService.ImportFileAsync(userId, filePath, sourceMetadata)`.
- Store source metadata separately from local media metadata: original URL, extractor, downloaded date, selected format, and license/usage note if entered by the user.
- Only move from staging to the library after hash and metadata extraction succeed.

### Packaging

Windows:

- Ship `yt-dlp.exe` and optional `ffmpeg.exe` in an app-local tools folder, or support a user-configured path.
- Sign the app and be aware of SmartScreen impact when bundling executables.
- Keep tool updates explicit and user-visible.

macOS:

- Put tools in the `.app` bundle resources or an app-managed tools directory.
- Ensure executable bits are set.
- Codesign and notarize bundled binaries.
- Avoid quarantine failures from downloaded tools.
- Account for arm64/x64/universal binaries.

### Licensing and Distribution

- yt-dlp and FFmpeg have separate licenses and distribution requirements. Verify exact license obligations before bundling.
- FFmpeg builds vary; LGPL/GPL choices matter.
- Include notices and source/license references where required.
- Do not position the feature as bypassing DRM or site restrictions.
- UI copy should make users responsible for downloading only allowed, owned, or permitted media.

## G. UI/UX Redesign Plan

### Current UI Structure

- `MainWindow` hosts the current view through `NavigationService.CurrentView`.
- `LoginView` is a simple centered form.
- `LibraryView` is a two-column layout: media list on the left, selected media/player/notes on the right.
- `MediaPlayerView` contains media metadata, macOS native video host, audio placeholder visuals, and an audio-only timeline.
- `NotesPanelView` contains note input, add button, notes list, jump, and delete.
- Styling uses default Fluent theme plus hardcoded `White`, `Gray`, `Black`, and hex colors.

### Professional Teacher Layout

Recommended app shell:

- Left sidebar: Library, Courses, Downloads, Class Mode, Settings.
- Library pane: search, filters, import, media list, missing-file indicators.
- Main player pane: selected media title, video/audio surface, shared timeline, playback controls, speed, fullscreen/class mode.
- Right notes/resources pane: timestamped notes, resource links/files, note search, tags.
- Bottom or contextual status area: import/download progress and backend status.
- Settings: theme, library location, media backend diagnostics, downloader tool paths.

The first screen after login should be the usable library shell, not a landing page.

### Theme System

- Keep `FluentTheme`.
- Use Avalonia `ThemeVariant` and `RequestedThemeVariant` for system/light/dark.
- Add resource dictionaries for semantic brushes: background, surface, border, text primary, text secondary, accent, danger, warning.
- Use `DynamicResource` in XAML.
- Remove hardcoded colors from views.
- Persist theme preference in settings later; default to system.

### Reusable Components

- `AppShellView`
- `SidebarNavItem`
- `LibraryPane`
- `MediaListItem`
- `MediaPlayerPanel`
- `TimelineControl`
- `PlaybackControls`
- `NotesPanel`
- `NoteListItem`
- `ResourcePanel`
- `ImportProgressDialog`
- `SettingsPanel`
- `BackendStatusBadge`
- `EmptyState`

Do not build all of these at once. Start by extracting `TimelineControl` and theme resources because they reduce real duplication and unblock both audio and video.

## H. Testing Plan

### Missing Tests

- No meaningful domain tests.
- No import tests.
- No persistence tests.
- No timeline/seek tests.
- No note workflow tests.
- No backend selection tests.
- No UI smoke checklist in docs.
- No packaging validation.

### First Tests to Add

1. `PlaybackTimelineServiceTests`
   - user seek prevents backend overwrite.
   - pending seek holds display target.
   - pending seek clears near backend target.
   - invalid values sanitize to zero.
2. `MediaFileTypeDetectorTests`
   - supported audio/video extensions.
   - unsupported extension returns null.
   - case-insensitive extension handling.
3. `NotesPanelViewModelTests`
   - notes load sorted.
   - active note updates with timeline.
   - add note inserts in order.
   - jump emits expected seek request, or calls the replacement playback controller.
4. `MediaImportServiceTests`
   - skips unsupported files.
   - imports supported files.
   - detects duplicate for same user.
   - allows or intentionally blocks duplicates across users, depending on chosen policy.
5. `BackendSelectionTests`
   - Windows registers Windows or unsupported backend.
   - macOS registers macOS backend.
   - unsupported OS does not crash startup.

### Practical UI/Manual Validation

- Launch app on Windows without macOS native libraries and confirm no startup crash.
- Login/create local user.
- Import folder with audio and video.
- Select media item.
- Play, pause, seek, and stop.
- Add note while playing.
- Jump to note.
- Delete note.
- Move/delete a media file and confirm the UI shows a recoverable error.
- Switch light/dark/system theme.
- Restart app and confirm database path and library persist.

## I. First Implementation Slices

1. Add OS-aware media backend registration and an `UnsupportedMediaBackend`. Keep the existing macOS backend untouched. Remove duplicate service registrations in `Program.cs`. This is the lowest-risk first cross-platform task.
2. Add timeline service tests before changing seek behavior.
3. Wire note jump to the player through the existing `PlaybackControlService` or replace it with one direct playback controller.
4. Move database path to a per-user app data location with one small migration-safe change.
5. Make import results explicit by using `MediaImportResult` and fixing user-scoped duplicate detection.
6. Extract a shared `TimelineControl` used by both audio and video.
7. Introduce theme resource dictionaries and replace hardcoded view colors.
8. Add a `Tempovium.Media.Windows` skeleton only after the app can start on Windows without the macOS backend.
9. Add downloader contracts without bundling yt-dlp.

## Recommended first coding prompt

Make Tempovium choose its media backend by OS without implementing Windows playback yet. Add an `UnsupportedMediaBackend`, register `MacMediaBackend` only on macOS, register the unsupported backend on Windows/other OSes, remove duplicate DI registrations in `Program.cs`, and add the smallest tests needed to prove Windows registration does not construct the macOS backend. Do not redesign the UI in this task.

## Risks before adding yt-dlp

- App data paths are not ready for managed downloads.
- Import has no single-file path, progress, cancellation, or rich result model.
- Duplicate detection is currently hash-only and not correctly user-scoped in service logic.
- Media metadata such as duration is not extracted during import.
- There is no job store or process supervision model.
- Packaging/signing strategy for bundled executables is absent.
- Legal/license notices and user-facing allowed-use language are absent.

## Risks before UI redesign

- Playback and note-jump behavior are not reliable enough to build a larger workflow around.
- The player is macOS-specific in XAML and viewmodel shape.
- Theme resources do not exist; hardcoded colors are spread across views.
- View constructors depend on `Program.AppHost`, making design-time and test-time UI work harder.
- The current media player mixes metadata, native host, audio placeholder, timeline, loading state, and controls in one view.
- Large library behavior is not tested or optimized.

## Windows validation checklist

- `dotnet build Tempovium.sln` succeeds.
- App launches on Windows without loading `TempoviumMacBridge`.
- Login view opens.
- Library view opens after login.
- Unsupported media backend shows a clear message instead of crashing.
- Folder picker returns usable local paths.
- Import stores media in SQLite under the intended app data path.
- Missing file load shows a recoverable error.
- Light/dark/system theme works.
- Publish output contains only Windows-appropriate native/tool assets.

## macOS validation checklist

- Native bridge builds as `libTempoviumMacBridge.dylib`.
- The dylib is copied into the app bundle/output where .NET can load it.
- The dylib and app are codesigned together.
- App launches on macOS.
- AVPlayerView appears inside Avalonia.
- Video loads, plays, pauses, seeks, and closes without native crashes.
- Audio loads and shares the same timeline behavior.
- AppKit/AVFoundation calls remain on the main thread.
- Light/dark/system theme works.
- Publish output contains the macOS backend and no Windows-only native assets.
