# Verification — 2026-10-24

## Results

- Release build: .NET 10, Dalamud SDK/API 15; zero warnings/errors. NuGet restore/audit retried with approved network access and succeeded.
- 460 managed assertions: normal/cross and PvE/PvP editing, single/whole copy/move, same-slot no-op, undo, cross-type single slot, bounds, snapshot isolation, concurrent-game-change refusal, failed destination preserving source, no retry, localization completeness and persistence.
- 2,432 native-ImGui/glyph/production-flow assertions: seven language renders, all localized glyphs across four CJK font faces, real drag/drop acceptance, staged move, apply and journal, successful command verification, timeout without local fallback or retry. Synthetic hotbar provider; no game was attached. Dynamic action-name glyph additions use a bounded cache; this runtime atlas rebuild has not been game-tested.
- UI renders inspected in Japanese for normal and cross layouts. Harness icons are numbered placeholders; production loads game textures using their icon IDs. No screen control or foreground window was used.
- Canonical StandardPluginTabs is byte-identical to the project template.

## Storage and access measurements

- 600 configuration edits: zero immediate writes; one save on the next 60-second check. Eight unchanged flushes: zero additional writes.
- Persistence fixture including seven language changes: 9 writes / 271 logical UTF-8 bytes. Three backups retained; corrupt-primary recovery tested; injected failure stops after three attempts, including disposal.
- Native UI fixture: unchanged repeated renders do not reread hotbars. Successful uncached previews read only the selected 12/16 slots. History reads up to 20 file names for the requested UTC day/page; bodies are read only on explicit restore. Per-operation files are capped at 512 KiB on restore.
- Settings, preview and name caches are bounded: 128 bars per editing session, 100 undo steps, 4,096 icon/name entries. No HTTP/DB/polling code. Game reads run on Dalamud's framework/UI thread.
- Local move batch: 2 changed slot writes, 1 request to the game's local file writer. These are synthetic callback counts, not physical SSD writes. Before-mutation and verified-memory journal entries each flush once. No automatic replay after uncertain results.

## Boundaries

No custom packet creation/sending, sockets, action execution or macro execution code is present. Built-in chat commands are limited to generated hotbar copy/set/general commands with validated sheet-derived names. Local fallback changes exact saved hotbar slots, refreshes live slots where applicable, and calls the game user-file save API once per changed batch. Shared flags are not toggled. Inactive jobs and inactive PvP/PvE groups remain local saved data.

Whole-bar transfer across normal/cross types is rejected because their slot counts differ. PvE/PvP cross-mode transfer is rejected. Unknown/transient slot types, invalid PvP job groups, combat/zoning and stale snapshots are rejected.

## Unverified

Actual game execution, native hotbar address/function behavior for the installed game build, HOTBAR.DAT persistence completion, relogin retention, special overridden hotbars, dynamic icon details, normal-exit save timing and physical SSD writes have not been validated in a live game. The UI harness does not establish those properties. File-save request and memory readback are not a disk durability acknowledgment. Journals support user-driven restore and are not a promise of atomic rollback. No installed plugin or real character data was modified during development.

## Provenance

FFXIVClientStructs SDK APIs and upstream source documentation were consulted; local shared persistence/font and software ImGui renderer patterns were reused from Command Atlas (Roxyz0501, MIT). No other plugin repository was cloned. Original icon is reproducible with make-icon.ps1.
