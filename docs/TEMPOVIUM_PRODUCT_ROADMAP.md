# Tempovium Product Roadmap

## 1. Product Vision

Tempovium is a local-first desktop app for teachers and professors who use video and audio as teaching material. It should help educators organize media, prepare classes, annotate videos/audio, search their teaching knowledge, build lesson resources, and use local AI assistance without depending on cloud services.

The product should feel like a professional teaching workspace: reliable playback, searchable classroom resources, fast note capture, and privacy-respecting local files.

## 2. Core Product Principles

- Local-first.
- Cross-platform Windows/macOS.
- Teacher workflow first.
- Offline-friendly.
- Privacy-respecting.
- Professional UI.
- Stable playback/library/notes before advanced features.
- No feature should be added before its technical dependencies are ready.

## 3. Confirmed Product Scope

- Professional media library.
- Courses/classes/collections.
- Timestamped notes.
- Range-based notes.
- Note types: explanation, question, pause, activity, assignment, warning, definition, example.
- Global search across everything.
- Class mode.
- Virtual clips/segments.
- Lesson playlists.
- Export notes and class guides.
- Notes-only import/export for existing media items.
- Full media package import/export containing media and notes.
- Export/import complete local account.
- Managed local media library storage.
- Folder import review flow that previews detected media before import.
- Users can choose detected videos/audio and edit display names before import.
- Imported media is copied into an app-managed local media folder.
- External source paths are stored only as source metadata, not as the primary playable path.
- Expanded Windows import/library media extension support, including `.mkv`.
- Duplicate detection stronger than name, duration, or file size alone.
- Local/offline library.
- yt-dlp-based URL import module.
- Local AI transcription using pretrained models.
- Transcript-linked highlights.
- Transcript-linked notes.
- Search inside transcripts.
- Light/dark/system theme.
- Windows and macOS packaging.

### Validated Windows Baseline

Manual Windows validation after Phase 1 and the first Phase 2 note-jump work confirmed:

- App opens on Windows.
- Login works.
- Library opens.
- Folder import works.
- Four video files imported successfully.
- Selecting a video on Windows does not crash.
- Windows shows the unsupported playback placeholder.
- Active backend is shown as `Unsupported media backend`.
- No visible `TempoviumMacBridge` error appeared.
- Notes can be added.
- Notes persist after closing the app and logging in again with the same user.

Observed UX issue to address in the later UI redesign phase:

- The current interface is visually noisy and not professional enough.
- Login is too basic.
- Library/player/notes layout has too many borders and feels unpolished.
- Do not address this before the planned UI redesign phase.

## 4. Global Search Plan

Global search replaces keyboard-shortcut-first planning. Shortcuts can come later, but search is the primary navigation and retrieval feature for a teacher's media, notes, transcripts, and class resources.

Search must eventually cover:

- media titles
- file paths
- courses
- classes
- collections
- tags
- notes
- note types
- transcript text
- transcript highlights
- resources
- downloaded source URLs

Phased implementation:

1. Simple in-memory/database search first.
2. Search current media notes first.
3. Search library metadata.
4. Search courses/classes/collections.
5. Search transcript segments.
6. Add SQLite full-text search later if useful.
7. Group results by media, notes, transcript, classes, and resources.

## 5. Local AI Transcription Plan

Tempovium should not train a model. Use a pretrained local ASR model, keep the model local, and start with a .NET-friendly proof of concept such as Whisper.net/whisper.cpp. Model files can be downloaded/imported later.

Transcription must not block playback, must be cancellable, and must not corrupt existing media or notes if it fails. Treat "real-time" first as progressive/local transcription plus synchronized transcript playback, not as a hard live-streaming requirement.

Phases:

- Phase A: offline transcription for a selected media file.
- Phase B: progressive transcription with UI updates while processing.
- Phase C: synchronized transcript panel during playback.
- Phase D: select transcript text/range and create note or highlight.
- Phase E: search transcript across the library.
- Phase F: optional local AI helpers such as class summary, questions, and lesson guide generation.

## 6. Proposed Data Concepts

High-level models:

- `Course`
- `ClassGroup`
- `Collection`
- `LessonPlan`
- `LessonItem`
- `MediaTag`
- `MediaResource`
- `TranscriptSegment`
- `TranscriptHighlight`
- `DownloadJob`
- `DownloadSourceMetadata`

`MediaNote` may need:

- optional `EndSeconds`
- `NoteType`
- optional transcript segment link
- optional highlight link
- created/updated timestamps
- edited/reviewed state if linked to transcription

## 7. UI/UX Direction

Professional layout:

- sidebar
- library/search
- player
- notes
- transcript
- resources
- downloads
- settings
- class mode

Light/dark/system theme is a required product feature.

The UI should eventually support:

- teacher-focused dashboard
- media-first workspace
- transcript panel
- note/highlight creation from timeline and transcript
- searchable library
- download/import queue
- clean classroom presentation mode

Accepted login/account shell requirements:

- Initial screen shows cards for registered local users.
- Selecting a user card asks for that user's password.
- Offer an option to remember the session for a configurable time.
- Main app has a top bar.
- Top bar includes the current user/account indicator.
- Top bar includes logout.
- Top bar includes switch account.
- Top bar includes settings.
- Settings includes interface preferences.
- Settings includes light/dark/system theme.
- Settings includes future import/export options.
- Settings includes account backup/restore options.

Visual redesign requirements:

- Reduce noisy borders.
- Improve spacing.
- Modernize cards.
- Make the app look professional enough for classroom preparation work.
- Hide full internal AppData media paths from normal library cards.
- Show a short media identity value instead, such as the managed file name/hash without extension.
- Show a dimming full-screen import loading overlay for both `Importar archivo` and `Importar carpeta`.

Before implementing the full UI redesign, create visual mockups/designs for:

- user selection/login screen
- main library/player/notes workspace
- settings screen
- account backup/restore screen
- class mode screen

Next functional block:

- The next functional development block must be the full modern Avalonia UI recreation.
- Include user cards, password prompt, configurable remember-session, top bar, logout, switch account, settings, light/dark/system theme, import/export/account options, cleaner library/player/notes workspace, modern cards, reduced noisy borders, import overlay, and hidden internal AppData paths.

Do not implement UI changes in this task.

## 8. Managed Local Media Library

Tempovium should manage imported media files inside an app-controlled local storage directory.

- On Windows, managed media should live under the appropriate AppData location.
- On macOS, managed media should live under the appropriate app support/data directory.
- The original imported file path should be retained only as metadata for traceability/relinking.
- The library should point to the managed copy after import.
- Normal library cards should not show the full managed AppData path because it is an internal implementation detail.
- Prefer a short managed media identity, such as the managed file name/hash without extension, while keeping original path metadata available internally.
- This reduces broken library entries when a user deletes, renames, or moves original files.
- This applies to folder import and single-file import.
- This affects future yt-dlp imports, package imports, and account backup/restore.
- Do not export a folder by merely saving external file paths as the main dependency.
- If exporting media, package/copy the managed media files or clearly mark the export as metadata-only.

Do not implement managed local media storage before app data path stability, import result model improvements, user-scoped duplicate detection, single-file import support, conflict handling strategy, and a clear media identity/fingerprint model are ready.

## 9. Folder Import Review Flow

Intended folder import flow:

- User selects a folder.
- Tempovium scans supported video/audio files.
- Tempovium shows a modal/dialog with detected files.
- Modal shows original file name.
- Modal shows detected media type.
- Modal shows original path.
- Modal shows duration if available.
- Modal shows size.
- Modal shows duplicate/conflict status.
- Modal shows proposed display name.
- User can select/unselect files.
- User can edit display names before import.
- User can confirm import.
- User can cancel without changing the library.
- On confirm, copy selected files into app-managed media storage.
- On confirm, calculate fingerprints/hashes.
- On confirm, create library records.
- On confirm, show an import summary.
- During import, show a global loading overlay that dims the app and clearly indicates import is running.

Windows media extension compatibility:

- Windows import/library should support broader common media extensions, including `.mkv`.
- Playback can still depend on the active platform backend; MKV limits are mainly a macOS playback/backend concern, not a reason to block Windows import/library support.

## 10. Duplicate Detection Strategy

Tempovium should not rely only on name, duration, or file size for duplicate detection.

- Primary: full content hash when practical.
- Secondary: partial hashes for large files or faster pre-scan.
- Metadata comparison: normalized file name.
- Metadata comparison: file size.
- Metadata comparison: duration.
- Metadata comparison: media type.
- Metadata comparison: codec.
- Metadata comparison: resolution.
- Metadata comparison: frame count when available.
- Metadata comparison: bitrate when available.
- Future optional layer: audio/video fingerprinting if needed.
- Duplicate matching must be user-scoped unless a future global dedupe design explicitly changes that.
- If a duplicate is detected, the import modal should show it before import.
- If confidence is high, default to skipping duplicate media.
- If confidence is uncertain, ask the user.

## 11. Implementation Phases

### Phase 0: Documentation and Planning

Goal: lock the product direction and technical sequence.

What changes:

- Maintain `AGENTS.md`, this roadmap, and the technical audit.
- Keep the first coding task explicit.

What not to do yet:

- Do not change source code for planning-only tasks.
- Do not start UI redesign, yt-dlp, or AI transcription.

Validation checklist:

- Docs exist and reference the technical audit.
- Roadmap names dependencies and non-goals.
- `git status --short` shows only intended documentation changes.

Dependencies:

- Existing technical audit.

### Phase 1: Cross-Platform Foundation

Goal: make the app start cleanly on Windows and macOS without constructing the wrong media backend.

What changes:

- Choose media backend by OS.
- Add `UnsupportedMediaBackend`.
- Register `MacMediaBackend` only on macOS.
- Register `UnsupportedMediaBackend` on Windows/other OSes for now.
- Remove duplicate DI registrations in `Program.cs`.
- Add minimal backend selection tests.

What not to do yet:

- Do not implement Windows playback.
- Do not redesign the UI.
- Do not implement yt-dlp or AI transcription.

Validation checklist:

- Build succeeds.
- Backend selection tests pass.
- Windows path does not construct `MacMediaBackend`.
- macOS registration remains available.

Dependencies:

- Current `Tempovium.Media.Abstractions` and technical audit findings.

### Phase 2: Playback/Timeline/Notes Reliability

Goal: make playback state, seeking, and timestamped notes dependable.

What changes:

- Add tests for `PlaybackTimelineService`.
- Wire note jump to actual player seek.
- Share timeline behavior for audio and video.
- Add safer seek completion behavior.
- Handle media-ended state.

What not to do yet:

- Do not expand teacher organization features.
- Do not redesign the full shell.

Validation checklist:

- Timeline tests cover user seek and backend polling overwrite behavior.
- Jump-to-note seeks correctly.
- Audio and video expose consistent playback controls.
- Manual playback checklist passes.

Windows validation note:

- Basic timestamped note editing is validated: each note shows Edit, save/cancel work, edited text updates in the list, and edits persist after closing/relogin with the same user. `dotnet build Tempovium.sln` succeeded and `dotnet test Tempovium.sln --no-build` passed with 19 tests.

Dependencies:

- Phase 1 backend selection.

### Phase 3: App Data Path and Import Reliability

Goal: make local storage and imports safe for real desktop use.

What changes:

- Stabilize per-user app data paths.
- Add managed local media storage.
- Add folder import review modal.
- Add single-file import support.
- Improve import result model.
- Add user-scoped duplicate detection.
- Add media identity/fingerprint model.
- Add conflict handling strategy.
- Add progress/cancellation where useful.

What not to do yet:

- Do not add yt-dlp.
- Do not add bulk metadata systems beyond what import needs.
- Do not add full media packages or account backup/restore before this foundation is stable.

Validation checklist:

- Import tests pass.
- Missing files are handled cleanly.
- Database path is stable across app launches.
- Imported media points to managed local storage.
- Folder import can preview detected files before changing the library.
- Duplicate/conflict behavior is documented and tested.
- Import summary reports imported, skipped, duplicated, and conflicted items.

Windows validation note:

- App-data SQLite path is validated at `C:\Users\legarcia\AppData\Local\Tempovium\tempovium.db`; `Test-Path "$env:LOCALAPPDATA\Tempovium\tempovium.db"` returned `True`. App opens, existing user/library/notes load, new or edited notes persist after close/reopen, no visible `TempoviumMacBridge` error appeared, `dotnet build Tempovium.sln` succeeded, and `dotnet test Tempovium.sln --no-build` passed with 21/21 tests.
- Import result summary and user-scoped duplicate detection are validated on Windows. First import reported imported 1, duplicates 0, unsupported 9, errors 0; same-user reimport reported imported 0, duplicates 1, unsupported 9, errors 0; a different user could import the same media with imported 1, duplicates 0, unsupported 9, errors 0. Same-user library did not duplicate media, existing notes still loaded, no visible `TempoviumMacBridge` error appeared, `dotnet build Tempovium.sln` succeeded, and `dotnet test Tempovium.sln --no-build` passed with 24/24 tests.
- Single-file media import is validated on Windows. `Importar archivo` appears next to `Importar carpeta`; importing one supported file reported imported 1, duplicates 0, unsupported 0, errors 0; same-user reimport reported imported 0, duplicates 1, unsupported 0, errors 0; the library did not duplicate the media, no visible `TempoviumMacBridge` error appeared, `dotnet build Tempovium.sln` succeeded, and `dotnet test Tempovium.sln --no-build` passed with 29/29 tests.
- Managed media copy-on-import is validated on Windows. A new supported media file is copied under `C:\Users\legarcia\AppData\Local\Tempovium\Media`, the original source file remains in place, same-user reimport reports a duplicate without duplicating the library item, no visible `TempoviumMacBridge` error appeared, `dotnet build Tempovium.sln` succeeded, and `dotnet test Tempovium.sln --no-build` passed with 32/32 tests.

Dependencies:

- Phase 1 for platform paths.
- Phase 2 for reliable media loading.

### Phase 4: UI Redesign and Themes

Goal: introduce a professional teacher-focused shell after the foundation is stable.

What changes:

- Create visual mockups before implementation for login, main workspace, settings, account backup/restore, and class mode.
- Add local user card selection and password prompt flow.
- Add configurable remember-session option.
- Add main top bar with current user, logout, switch account, and settings.
- Add settings areas for interface preferences, theme, future import/export options, and account backup/restore.
- Add sidebar, library/search area, player, notes, resources, downloads, settings, and class-mode entry.
- Add light/dark/system theme using Avalonia resources/theme variants.
- Replace hardcoded colors with semantic resources.
- Reduce noisy borders and improve spacing/card polish.
- Hide internal managed AppData paths in normal library display.
- Add a full-screen dimming import loading overlay.
- Extract reusable controls where they remove real duplication.

What not to do yet:

- Do not build advanced AI or downloader workflows.
- Do not rewrite stable viewmodels without need.

Validation checklist:

- Mockups are reviewed before implementation.
- Login/account flow is visually clear and keyboard usable.
- Top bar account actions are visible.
- Light/dark/system theme works.
- Text fits on common desktop sizes.
- Existing import/playback/notes workflows still work.
- Manual UI checklist passes on Windows and macOS.

Dependencies:

- Phases 1-3.

### Phase 5: Teacher Organization Features

Goal: organize media around teaching work.

What changes:

- Add courses/classes/collections.
- Add lesson playlists and virtual clips/segments.
- Add note types and range-based notes.
- Add media resources.

What not to do yet:

- Do not add transcript-dependent features before transcription MVP.
- Do not add LMS or collaboration.

Validation checklist:

- Media can be assigned to courses/classes/collections.
- Notes support type and range.
- Lesson playlist ordering works.
- Existing library search still works.

Dependencies:

- Phase 3 import reliability.
- Phase 4 professional shell.

### Phase 6: Global Search

Goal: make search the primary way to retrieve teaching material.

What changes:

- Search current media notes.
- Search library metadata.
- Search courses/classes/collections/tags/resources.
- Group results by media, notes, classes, resources, and later transcript.
- Consider SQLite full-text search only after simple search is insufficient.

What not to do yet:

- Do not optimize prematurely.
- Do not add transcript search until transcript data exists.

Validation checklist:

- Search returns relevant media and notes.
- Results are grouped clearly.
- Search remains fast on a representative local library.

Dependencies:

- Phase 5 organization data.

### Phase 7: yt-dlp URL Import

Goal: support controlled URL import for allowed/owned media.

What changes:

- Add downloader contracts and job model.
- Run yt-dlp as a controlled external process with sanitized arguments.
- Add progress, cancellation, errors, FFmpeg handling, and import through the managed media path.
- Add licensing/distribution notes before bundling tools.

What not to do yet:

- Do not bypass DRM.
- Do not auto-download copyrighted media.
- Do not bundle tools before packaging/licensing decisions.

Validation checklist:

- Process execution avoids shell invocation.
- Cancellation kills the process safely.
- Failed downloads do not import partial files.
- Completed downloads import through the same managed import path.

Dependencies:

- Phase 3 app data/import reliability.
- Phase 12 packaging decisions for bundled tools.

### Phase 8: Local AI Transcription MVP

Goal: transcribe one selected media file locally with a pretrained model.

What changes:

- Choose a .NET-friendly local ASR proof of concept.
- Add model discovery/import.
- Run transcription in the background.
- Save transcript segments.
- Support cancellation and failure recovery.

What not to do yet:

- Do not train custom models.
- Do not require cloud services.
- Do not build AI summaries before transcript quality is acceptable.

Validation checklist:

- Selected media can be transcribed offline.
- Playback remains usable while transcribing.
- Cancellation leaves media and notes intact.
- Transcript segments persist.

Dependencies:

- Phases 2, 3, and 6.

### Phase 9: Transcript Highlights and Transcript-Linked Notes

Goal: make transcripts part of the teaching workflow.

What changes:

- Add transcript panel.
- Link notes to transcript segments.
- Create highlights from selected transcript text/ranges.
- Seek playback from transcript segments.

What not to do yet:

- Do not add complex AI generation until highlight/note workflows are stable.

Validation checklist:

- Transcript follows playback.
- Highlight creation persists.
- Transcript-linked note jumps to the right media time.
- Search includes transcript highlights.

Dependencies:

- Phase 8 transcript segments.
- Phase 6 global search.

### Phase 10: Exports and Lesson Guide Generation

Goal: turn prepared media notes into useful teaching outputs.

What changes:

- Export notes and class guides.
- Add first export formats.
- Add notes-only JSON import/export and Markdown export first.
- Add full `.tempovium.zip` media packages later.
- Keep complete account backup/restore separate from notes and media packages.
- Add optional local AI helpers for class summary, questions, and lesson guide generation.

What not to do yet:

- Do not add cloud AI.
- Do not add LMS integration.
- Do not add full ZIP packages before Phase 3 import/storage foundations are stable.
- Do not add account backup/restore before its dependency gates are met.

Validation checklist:

- Export output is readable and complete.
- Export includes timestamps, note types, ranges, and media titles.
- Import summaries report imported notes, skipped duplicates, and errors.
- AI-generated content is optional and clearly editable.

Dependencies:

- Phases 3, 5, 8, and 9.

### Import/Export Notes and Media Packages

This section covers two features only:

- Notes-only import/export for an existing media item.
- Full package import/export containing media and notes.

Complete account backup/restore is a separate feature. Implement notes/package export later, not now. Do not implement before playback/library/notes reliability, app data path stability, user-scoped duplicate detection, import result model improvements, single-file import support, conflict handling strategy, and a clear media identity/fingerprint model are ready.

Notes-only export/import:

- Export notes from one selected media item.
- Import notes into an existing media item.
- Use `.tempovium-notes.json` for round-trip import.
- Use Markdown for human-readable class notes.
- Optional later `.csv`.
- Avoid duplicate notes when importing.

Full package export/import:

- Export a `.tempovium.zip` package.
- Include `manifest.json`.
- Optionally include the media file.
- Include notes JSON.
- Optionally include a human-readable notes export.
- Later include resources, transcript segments, highlights, and lesson metadata.
- Import the package by reading the manifest.
- If the package media already exists in the library, do not duplicate, copy, or import the video again.
- Automatically import/link package notes to the existing media item when there is a safe match.
- Prefer matching by file hash.
- If hash is unavailable, fall back to title + duration + file name.
- If no safe match exists, import/copy the media if included.
- If multiple possible matches exist, ask the user to choose the target media item.
- Avoid duplicate notes when importing.
- Show summary: imported media, imported notes, duplicates skipped, errors.

First implementation:

- JSON export/import for notes.
- Markdown export for notes.
- No full ZIP package yet.

### Account Backup and Restore

This is separate from notes-only export and full media package export. It covers complete local account export/import.

Account export should eventually include:

- user profile/account metadata
- media library metadata
- notes
- courses/classes/collections
- lesson plans
- resources
- transcript segments
- transcript highlights
- download metadata
- app/user settings

Media files may be included or referenced depending on export mode. Account import must avoid overwriting existing data without confirmation, detect conflicts, and show a summary before applying changes.

Implement account backup/restore later, not now. Do not implement it before app data path stability, user-scoped duplicate detection, import result model improvements, single-file import support, notes reliability, clear account identity model, and conflict handling strategy are ready.

Phase A: Export/import account metadata only.

- Include user profile.
- Include settings.
- Include library metadata.
- Include notes.
- No media files yet.

Phase B: Export/import account with local media references.

- Preserve file references/metadata.
- Show missing media after import.
- Allow relink later.

Phase C: Export/import account archive with media files.

- Package media files together with account data.
- Avoid duplicates by hash/fingerprint.
- Show size estimate before export.
- Confirm before overwriting or merging.

Phase D: Full account migration.

- Restore on another machine.
- Include notes, courses, lesson plans, transcripts, highlights, resources, and settings.
- Show import summary with imported, skipped, duplicated, and conflicted items.

### Phase 11: Class Mode

Goal: provide a clean classroom presentation workspace.

What changes:

- Add distraction-free player view.
- Show selected class notes/prompts.
- Support lesson playlists and virtual clips.
- Keep private prep notes hidden when needed.

What not to do yet:

- Do not add remote classroom control or collaboration.

Validation checklist:

- Class mode launches from a lesson plan.
- Playback and notes are projector-safe.
- Exiting class mode returns to the workspace cleanly.

Dependencies:

- Phases 4, 5, and 10.

### Phase 12: Packaging/Release Readiness for Windows and macOS

Goal: make professional installable builds for both target platforms.

What changes:

- Add Windows and macOS publish/package scripts.
- Handle native media libraries, downloader tools, model files, app data paths, signing, and notarization.
- Add release validation checklists.

What not to do yet:

- Do not publish before playback/import/notes are reliable.

Validation checklist:

- Windows package installs and launches.
- macOS package signs/notarizes and launches.
- Native dependencies load from packaged output.
- Optional tools/models are included or discovered by documented policy.

Dependencies:

- All earlier foundations, especially Phases 1, 3, 7, and 8.

## 12. Current Coding Sequence After Baseline Validation

Phase 1 has a validated Windows baseline. Next coding should stay on foundation work:

- Continue Phase 2 playback/timeline/notes reliability.
- Then complete Phase 3 app data paths, managed media storage, import review, single-file import, import result model, duplicate detection, media fingerprinting, and conflict handling.
- Do not redesign UI before Phase 3 foundations are stable.
- Do not implement full media packages or account backup/restore before their gates are met.
- Do not implement yt-dlp before managed import flow, app data paths, process execution, and licensing notes are ready.
- Do not implement AI transcription yet.

## 13. Non-goals For Now

- No cloud AI.
- No LMS integration yet.
- No collaboration/sync accounts yet.
- No mobile app.
- No DRM bypass.
- No automatic download of copyrighted media.
- No training custom AI models.
- No full UI rewrite before playback/library/notes are stable.
- No downloader module before safe external process execution and licensing notes are planned.

## 14. Open Questions

- Which Windows playback backend should be used?
- Which local transcription engine should be selected after PoC?
- Should models be bundled or downloaded/imported by user?
- How large should bundled or optional model downloads be?
- Which exports are first: Markdown, PDF, HTML, CSV?
- Which media hash/fingerprint strategy should be used first?
- Should courses/classes be required or optional?
- Should yt-dlp be bundled, user-provided, or app-managed?
- Should FFmpeg be bundled or user-configured?
- What minimum macOS version should be supported?
- What minimum Windows version should be supported?

## 15. Roadmap Status Table

| Phase | Feature Area | Status | Depends On | Notes |
| --- | --- | --- | --- | --- |
| Phase 0 | Documentation and planning | Updated | Technical audit | Current roadmap records latest validated baseline and product direction. |
| Phase 1 | Cross-platform foundation | Windows baseline validated | Phase 0 | App opens, login/library/import work, Windows uses unsupported playback placeholder. |
| Phase 2 | Playback/timeline/notes reliability | Started | Phase 1 | Notes can be added and persist after closing/relogin; continue reliability work. |
| Phase 3 | App data path and import reliability | Next foundation | Phases 1-2 | Managed storage, import review, single-file import, duplicate detection, fingerprinting, conflicts. |
| Phase 4 | UI redesign and themes | Ready after Phase 3 | Phases 1-3 | Mockups first; reduce noisy borders and improve login/account shell. |
| Phase 5 | Teacher organization features | Ready after Phase 4 | Phase 4 | Courses, classes, collections, playlists. |
| Phase 6 | Global search | Ready after Phase 5 | Phase 5 | Search before shortcut-heavy planning. |
| Phase 7 | yt-dlp URL import | Blocked by foundation | Phases 3 and 12 decisions | Needs safe process execution and licensing. |
| Phase 8 | Local AI transcription MVP | Ready after Phase 6 | Phases 2, 3, 6 | Use pretrained local models only. |
| Phase 9 | Transcript highlights/linked notes | Ready after Phase 8 | Phase 8 | Depends on transcript segments. |
| Phase 10 | Exports and lesson guide generation | Ready after Phase 9 | Phases 3, 5, 8, 9 | Notes-only first; full packages and account backup later. |
| Phase 11 | Class mode | Ready after Phase 10 | Phases 4, 5, 10 | Classroom-safe presentation mode. |
| Phase 12 | Windows/macOS packaging | Future | Phases 1, 3, 7, 8 | Release readiness, not release yet. |
