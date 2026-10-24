# Hotbar Studio

Original Dalamud API 15 plugin by **Roxyz0501**. Open with `/hotbarstudio`.

## 日本語

通常ホットバー／クロスホットバーの共通・各クラス／ジョブ・PvP保存内容を左右にプレビューし、アイコン1枠またはバー全体をコピー・移動します。コピー先の番号を複数選ぶと同じ内容をまとめてコピーできます。

1. コピー元・コピー先のモード、ジョブ、バー番号を選択。
2. アイコンまたはバー見出しをドラッグ。コピー／移動を切り替えられます。
3. 変更した枠とコピー先プレビューを確認し「変更を適用」。コピー先は上書きされます。
4. 「操作を戻す」は未適用の編集を戻します。「破棄して再読込」はゲームから取得し直します。
5. 変更履歴はUTC日付指定・20件ページ単位で閲覧できます。変更前の内容をプレビューへ復元し、確認して適用できます。

利用できる単独バーコピー・通常バーの一部アクション設定はゲーム内コマンドを使用します。それ以外（別ジョブへの1枠コピー、マクロ参照、複数バー、移動など）はゲームのローカルホットバーデータとローカル保存処理を使います。アクションやマクロ本文の実行、独自パケットの構築・送信、外部通信は行いません。

通常／クロス間は1枠ずつ転送可能です。PvE⇔PvPのコピー・移動にも対応し、この場合は常にローカル転送します。アクションIDは変換しないため、転送先のモードで使えないアクションは使用可能になりません。枠数が違うバー全体の相互コピーは対象外です。共有設定そのものは変更しません。キャラクターをまたぐ転送は行いません。無効なPvPジョブ、一時的なドラッグ用データは適用できません。

**初版プレビュー。ゲーム実機でのHOTBAR.DAT保存、再ログイン後の保持、特殊バー状態は未検証です。** 合成データの編集・保存・失敗時の動作と、実際のImGui描画／ドラッグ処理をバックグラウンドで検証しています。実機未検証を検証済みとは扱いません。

## English

Side-by-side previews of normal/cross, shared/job and PvP hotbars. Drag slots or entire bar headers to stage copies or moves. Select multiple destination bar numbers to copy a bar to all of them. Apply explicitly after reviewing; existing destination contents are overwritten. No action execution or custom packet sending.

PvE/PvP transfers always use the local path, preserving exact slot types and IDs without converting actions or making them usable in the other mode. Supported same-mode simple edits use built-in commands. Other edits use local saved hotbar structures and the game's local HOTBAR.DAT writer. Source/target snapshots are checked before applying. Non-empty destinations are verified before any source is cleared. Failed or uncertain writes are never automatically replayed. Prepared snapshots are flushed to a per-operation UTC-date journal before changing game state. History restore is staged and still needs Apply. This does not promise transactional rollback or confirmed physical disk persistence.

Settings use changed-only 60-second saves, logout/shutdown flushes, and three valid backup generations. Previews are cached until explicit reload and cleared on character changes. No background network requests or whole-file polling. History is retained locally; older days are read only on demand. Journals may contain the local character content ID and hotbar action IDs; they are never uploaded by this plugin.

### Installation

Use the shared repository: `https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json`

Or select the release `HotbarStudio.dll` in Dalamud's development plugin settings. Do not install both copies at once.

### Build and checks

Requires the Dalamud API 15 development SDK, .NET 10, and the game's local structures supplied by Dalamud.

```powershell
dotnet build HotbarStudio/HotbarStudio.csproj -c Release
dotnet run --project Tests/Tests.csproj -c Release
dotnet run --project Tests/UI/UiTests.csproj -c Release -- artifacts/ui
```

The UI harness uses synthetic hotbars and placeholder action icons, and does not connect to the game. See VERIFICATION.md for measured limits.

### References and support

- [FFXIVClientStructs](https://github.com/aers/FFXIVClientStructs), aers and contributors, MIT: SDK structure layouts, local hotbar slots and user-file save API. No upstream plugin clone.
- [Official hotbar command syntax](https://jp.finalfantasyxiv.com/lodestone/playguide/db/text_command/0992946f68f/).
- [Dalamud](https://github.com/goatcorp/Dalamud) and host-provided Lumina libraries; see NOTICE.
- Optional support for Roxyz0501: [Ko-fi](https://ko-fi.com/roxyz0501).
