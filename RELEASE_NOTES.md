# 1.0.0.0 — initial preview

- Normal/cross, shared/class/job and PvP hotbar previews with game icons.
- Slot and whole-bar drag copy/move, selected-bar batch copy, staged previews, undo and explicit Apply.
- Built-in commands for eligible simple operations; local-only data editing otherwise. No custom packet code or action/macro execution.
- Snapshot conflict checks, destination-before-source verification, uncertain-result stop, UTC-partitioned pre-edit journal and restore preview.
- Seven languages, canonical language/support tabs, changed-only 60-second settings persistence with three valid backups and lifecycle flush.

Validated against API 15 with synthetic data and native ImGui rendering. Actual game HOTBAR.DAT disk persistence, relogin behavior and special hotbar contexts remain unverified. This is a public preview, not a claim of live-game validation.
