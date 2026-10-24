# 1.0.0.1 — local PvE/PvP transfers

- Allow PvE to PvP (and reverse) slot/whole-bar copy and move, including multiple destinations.
- Every cross-mode operation bypasses the command planner/sender and uses the existing local hotbar storage path. No custom packet sending or action/macro execution.
- Preserve exact slot type and ID. No PvE-skill exclusion and no automatic conversion to PvP actions; placement does not imply usability in that mode.
- Keep snapshot conflict detection and destination verification before clearing moved sources. Restore previews also use local writes to preserve exact IDs.
- Seven-language explanation appears when source and destination modes differ.

Release build: zero warnings/errors. 476 managed checks and 2,496 native-ImGui/glyph/production-flow checks passed. Live-game persistence and relogin behavior remain unverified.

# 1.0.0.0 — initial preview

- Normal/cross, shared/class/job and PvP hotbar previews with game icons.
- Slot and whole-bar drag copy/move, selected-bar batch copy, staged previews, undo and explicit Apply.
- Built-in commands for eligible simple operations; local-only data editing otherwise. No custom packet code or action/macro execution.
- Snapshot conflict checks, destination-before-source verification, uncertain-result stop, UTC-partitioned pre-edit journal and restore preview.
- Seven languages, canonical language/support tabs, changed-only 60-second settings persistence with three valid backups and lifecycle flush.

Validated against API 15 with synthetic data and native ImGui rendering. Actual game HOTBAR.DAT disk persistence, relogin behavior and special hotbar contexts remain unverified. This is a public preview, not a claim of live-game validation.
