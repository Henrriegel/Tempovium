# Tempovium Codex Instructions

Tempovium is a local-first, cross-platform Windows/macOS desktop app built with .NET and Avalonia. Development currently happens on Windows, but macOS is a first-class target.

Product direction: a professional media library, video/audio player, timestamped notes, lesson preparation, classroom resource, and local AI-assisted tool for teachers/professors.

## Durable Rules

- Use `docs/TEMPOVIUM_TECHNICAL_AUDIT.md` for current technical risks.
- Use `docs/TEMPOVIUM_PRODUCT_ROADMAP.md` for product scope and implementation phases.
- Keep changes small, focused, and verifiable.
- Do not implement speculative architecture.
- Prefer boring, maintainable code over clever abstractions.
- Avoid large rewrites unless the task explicitly asks for them.
- Do not create commits unless explicitly requested.
- If a task asks for a report or roadmap, do not modify source code.
- If something is blocked or uncertain, document the blocker instead of inventing behavior.
- Validate code changes with the smallest useful build/test check.

## Task Summary Requirements

- Every future coding task summary must state whether a visual change is expected.
- If a visual change is expected, describe what should look different.
- If manual validation is needed, include how to compile/run the app and a manual validation checklist.
- If no visual change is expected, say so explicitly.
- Do not ask the user to visually validate when a task only adds tests/docs and no UI/runtime behavior changed.
- When a task changes UI/runtime behavior, include Windows validation steps because current development happens on Windows.
- When the user provides manual validation results or screenshots, record validated behavior in the roadmap if it affects product direction, release readiness, or accepted UX requirements.

## Product Sequencing

- Phase 1 OS-aware media backend registration has a validated Windows baseline; keep future coding on the roadmap's foundation sequence.
- Do not redesign the UI before the playback/library/notes foundation is stable.
- Do not implement yt-dlp before media backends, import flow, app data paths, process execution, and licensing notes are ready.
- Do not implement local AI transcription before playback, notes, library, and search foundations are stable.
- Do not train custom AI models. Use pretrained local models only when that phase starts.
- Prefer local/offline features and privacy-respecting behavior.
- Windows and macOS must both be considered in architecture, app data paths, media handling, packaging, and future tool/model distribution.
- Imported media should eventually be copied into app-managed local storage instead of relying on external source paths as the primary library path.
- Source paths may be kept as metadata only.
- Do not implement managed local media storage before the roadmap gates are satisfied.
