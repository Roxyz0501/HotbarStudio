namespace HotbarStudio;
internal sealed class Configuration { public int Version { get; set; } = 1; public string? Language { get; set; } }
internal static class Loc
{
    public static readonly string[] Codes = ["ja", "en", "de", "fr", "ko", "zh-Hans", "zh-Hant"];
    private static readonly Dictionary<string, string[]> Text = new()
    {
        ["ConfigRecovered"] = ["設定を正常なバックアップから復元しました。", "Settings recovered from a valid backup.", "Einstellungen aus Sicherung wiederhergestellt.", "Paramètres restaurés depuis une sauvegarde valide.", "정상 백업에서 설정을 복원했습니다.", "已从有效备份恢复设置。", "已從有效備份還原設定。"],
        ["ConfigDamaged"] = ["設定を読めませんでした。初期設定を使用します。", "Settings unreadable. Using defaults.", "Einstellungen unlesbar. Standardwerte aktiv.", "Paramètres illisibles. Valeurs par défaut.", "설정을 읽을 수 없어 기본값을 사용합니다.", "无法读取设置，使用默认值。", "無法讀取設定，使用預設值。"],
        ["ConfigSaveFailed"] = ["設定保存に失敗しました。再試行は最大3回です。", "Settings save failed. At most three attempts.", "Speichern fehlgeschlagen. Höchstens drei Versuche.", "Échec de sauvegarde. Trois tentatives maximum.", "설정 저장 실패. 최대 3회 시도합니다.", "设置保存失败，最多尝试三次。", "設定儲存失敗，最多嘗試三次。"],
        ["ConfigSaveStopped"] = ["設定保存を停止しました。設定タブから再試行できます。", "Settings saving stopped. Retry in Settings.", "Speichern gestoppt. In Einstellungen wiederholen.", "Sauvegarde arrêtée. Réessayez dans Paramètres.", "설정 저장 중단. 설정 탭에서 재시도하세요.", "已停止保存设置，可在设置页重试。", "已停止儲存設定，可在設定頁重試。"],
        ["History"] = ["変更履歴", "History", "Verlauf", "Historique", "변경 기록", "更改历史", "變更紀錄"],
        ["HistoryHelp"] = ["UTC日付で操作前の記録を表示します。復元はプレビューに読み込み、適用時に反映します。", "Browse pre-edit records by UTC date. Restore loads a preview; Apply writes it.", "Vorherige Daten nach UTC-Datum. Wiederherstellen lädt eine Vorschau; Übernehmen schreibt sie.", "Consultez par date UTC. La restauration charge un aperçu à appliquer.", "UTC 날짜별 편집 전 기록입니다. 복원 미리보기를 적용해야 반영됩니다.", "按UTC日期浏览编辑前记录。恢复加载预览，应用后生效。", "依UTC日期瀏覽編輯前紀錄。還原載入預覽，套用後生效。"],
        ["Restore"] = ["変更前をプレビュー", "Preview restore", "Wiederherstellung ansehen", "Aperçu de restauration", "복원 미리보기", "预览恢复", "預覽還原"],
        ["Refresh"] = ["更新", "Refresh", "Aktualisieren", "Actualiser", "새로고침", "刷新", "重新整理"],
        ["RestoredPreview"] = ["復元内容を編集タブで確認し、適用してください。", "Review the restored preview in Editor, then Apply.", "Vorschau im Editor prüfen und übernehmen.", "Vérifiez l’aperçu dans l’éditeur, puis appliquez.", "편집 탭에서 복원 내용을 확인하고 적용하세요.", "请在编辑页检查恢复预览，然后应用。", "請在編輯頁檢查還原預覽，然後套用。"],
        ["MoveBar"] = ["このバーを移動", "Move this bar", "Leiste verschieben", "Déplacer cette barre", "이 단축바 이동", "移动此栏", "移動此列"],
        ["Editor"] = ["編集", "Editor", "Editor", "Éditeur", "편집", "编辑", "編輯"],
        ["Source"] = ["コピー元", "Source", "Quelle", "Source", "원본", "来源", "來源"],
        ["Target"] = ["コピー先", "Destination", "Ziel", "Destination", "대상", "目标", "目標"],
        ["Shared"] = ["共通", "Shared", "Gemeinsam", "Partagée", "공유", "共享", "共用"],
        ["Normal"] = ["通常ホットバー", "Hotbar", "Kommandoleiste", "Barre", "단축바", "快捷栏", "快捷列"],
        ["Cross"] = ["クロスホットバー", "Cross hotbar", "Kreuz-Kommandomenü", "Barre en croix", "십자 단축바", "十字快捷栏", "十字快捷列"],
        ["Bar"] = ["バー", "Bar", "Leiste", "Barre", "단축바", "栏", "列"],
        ["Copy"] = ["コピー", "Copy", "Kopieren", "Copier", "복사", "复制", "複製"],
        ["Move"] = ["移動", "Move", "Verschieben", "Déplacer", "이동", "移动", "移動"],
        ["CopyBar"] = ["このバーをコピー", "Copy this bar", "Leiste kopieren", "Copier cette barre", "이 단축바 복사", "复制此栏", "複製此列"],
        ["Batch"] = ["選択先にまとめてコピー", "Copy to selected bars", "In Auswahl kopieren", "Copier vers la sélection", "선택한 단축바에 복사", "复制到所选栏", "複製到所選列"],
        ["Apply"] = ["変更を適用", "Apply changes", "Übernehmen", "Appliquer", "변경 적용", "应用更改", "套用變更"],
        ["Undo"] = ["操作を戻す", "Undo edit", "Rückgängig", "Annuler la modification", "편집 취소", "撤销编辑", "復原編輯"],
        ["Reset"] = ["破棄して再読込", "Discard and reload", "Verwerfen und neu laden", "Abandonner et recharger", "취소 후 새로고침", "放弃并重新加载", "捨棄並重新載入"],
        ["Settings"] = ["設定", "Settings", "Einstellungen", "Paramètres", "설정", "设置", "設定"],
        ["Language"] = ["言語設定", "Language settings", "Sprache", "Langue", "언어 설정", "语言设置", "語言設定"],
        ["Support"] = ["支援", "Support", "Unterstützung", "Soutien", "후원", "支持", "支持"],
        ["Help"] = ["アイコンまたはバーの見出しをドラッグ。変更は適用までゲームに反映されません。コピー先の内容は上書きされます。", "Drag an icon or bar header. Changes stay in the preview until applied. Destination contents are overwritten.", "Symbol oder Leistentitel ziehen. Erst Übernehmen ändert das Spiel. Zielinhalte werden überschrieben.", "Glissez une icône ou un titre de barre. Appliquez pour modifier le jeu. La destination sera écrasée.", "아이콘이나 단축바 제목을 드래그하세요. 적용 전에는 게임이 변경되지 않습니다. 대상 내용은 덮어씁니다.", "拖动图标或栏标题。应用前仅更改预览。目标内容会被覆盖。", "拖曳圖示或列標題。套用前僅變更預覽。目標內容會被覆寫。"],
        ["Policy"] = ["バーコピーは可能な場合にゲーム内コマンドを使用。それ以外はローカルのホットバーデータを編集します。独自パケット送信なし。", "Whole-bar copies use game commands when possible. Other edits update local hotbar data. No custom packet sending.", "Leistenkopien nutzen nach Möglichkeit Spielbefehle. Andere Änderungen bearbeiten lokale Daten. Keine eigenen Netzwerkpakete.", "Les copies utilisent les commandes si possible. Les autres modifications restent locales. Aucun paquet personnalisé.", "가능한 단축바 복사는 게임 명령을 사용합니다. 나머지는 로컬 데이터를 편집합니다. 별도 패킷 전송 없음.", "整栏复制尽可能使用游戏命令，其他编辑修改本地快捷栏数据。不发送自定义数据包。", "整列複製盡可能使用遊戲指令，其他編輯修改本機快捷列資料。不傳送自訂封包。"],
        ["Donate"] = ["開発を任意で支援できます。", "You can optionally support development.", "Die Entwicklung kann freiwillig unterstützt werden.", "Vous pouvez soutenir le développement librement.", "개발을 자발적으로 후원할 수 있습니다.", "您可以自愿支持开发。", "您可以自願支持開發。"],
        ["NotReady"] = ["ログイン後、ホットバー読込完了を待ってください。", "Wait until login and hotbar loading finish.", "Auf Anmeldung und Laden warten.", "Attendez la connexion et le chargement.", "로그인과 단축바 로딩을 기다리세요.", "请等待登录和快捷栏加载完成。", "請等待登入和快捷列載入完成。"],
        ["Conflict"] = ["ゲーム側で内容が変わりました。破棄して再読込してください。", "Game data changed. Discard and reload.", "Spieldaten geändert. Verwerfen und neu laden.", "Données modifiées. Abandonnez et rechargez.", "게임 데이터가 변경되었습니다. 다시 불러오세요.", "游戏数据已更改，请放弃并重新加载。", "遊戲資料已變更，請捨棄並重新載入。"],
        ["Busy"] = ["戦闘中・エリア移動中などは適用できません。", "Cannot apply during combat, zoning or unavailable commands.", "Im Kampf oder beim Gebietswechsel nicht möglich.", "Impossible en combat ou pendant un changement de zone.", "전투 또는 지역 이동 중에는 적용할 수 없습니다.", "战斗、切换区域或命令不可用时无法应用。", "戰鬥、切換區域或指令不可用時無法套用。"],
        ["CharacterChanged"] = ["キャラクターが変わったため操作を破棄しました。", "Character changed; edits discarded.", "Charakter gewechselt; Änderungen verworfen.", "Personnage changé ; modifications abandonnées.", "캐릭터 변경으로 편집을 취소했습니다.", "角色已更改，编辑已放弃。", "角色已變更，編輯已捨棄。"],
        ["InvalidJob"] = ["このジョブには対象の保存領域がありません。", "No saved hotbars for this job and mode.", "Keine gespeicherten Leisten für diesen Job und Modus.", "Aucune barre sauvegardée pour ce job et ce mode.", "해당 직업과 모드의 저장된 단축바가 없습니다.", "此职业和模式没有已保存快捷栏。", "此職業和模式沒有已儲存快捷列。"],
        ["CrossModeHelp"] = ["PvE⇔PvPはローカル転送します。アクションを相手モード用に変換しないため、配置後も使用できないアクションがあります。", "PvE/PvP transfers are local. Actions are not converted to the other mode and may remain unusable there.", "PvE/PvP-Transfers erfolgen lokal. Aktionen werden nicht für den anderen Modus umgewandelt und sind dort eventuell nicht nutzbar.", "Les transferts PvE/PvP sont locaux. Les actions ne sont pas converties et peuvent rester inutilisables dans l’autre mode.", "PvE/PvP 전송은 로컬에서 처리합니다. 다른 모드의 기술로 변환하지 않으므로 사용할 수 없는 기술이 있을 수 있습니다.", "PvE/PvP传输在本地处理。技能不会转换为另一模式的技能，放置后可能仍无法使用。", "PvE/PvP傳輸在本機處理。技能不會轉換為另一模式的技能，放置後可能仍無法使用。"],
        ["SizeMismatch"] = ["バー全体は同じ種類同士で転送してください。", "Whole bars require the same bar type.", "Ganze Leisten benötigen denselben Typ.", "Les barres entières doivent être du même type.", "전체 단축바는 같은 종류여야 합니다.", "整栏传输需要相同类型。", "整列傳輸需要相同類型。"],
        ["Invalid"] = ["操作対象が無効です。", "Invalid selection.", "Ungültige Auswahl.", "Sélection invalide.", "잘못된 선택입니다.", "选择无效。", "選擇無效。"],
        ["Unsupported"] = ["安全に保存できない一時スロットが含まれています。", "Contains unsupported transient slots.", "Nicht unterstützte temporäre Slots enthalten.", "Emplacements temporaires non pris en charge.", "지원하지 않는 임시 슬롯이 포함되어 있습니다.", "包含不支持的临时槽位。", "包含不支援的暫存欄位。"],
        ["Limit"] = ["編集上限です。適用または破棄してください。", "Edit limit reached. Apply or discard.", "Limit erreicht. Übernehmen oder verwerfen.", "Limite atteinte. Appliquez ou abandonnez.", "편집 한도입니다. 적용하거나 취소하세요.", "已达编辑上限，请应用或放弃。", "已達編輯上限，請套用或捨棄。"],
        ["VerifyFailed"] = ["反映を確認できませんでした。自動再実行はしません。再読込して確認してください。", "Could not verify changes. No automatic retry. Reload and inspect.", "Änderung nicht bestätigt. Keine Wiederholung. Neu laden und prüfen.", "Modification non vérifiée. Aucun nouvel essai automatique. Rechargez et vérifiez.", "변경 확인 실패. 자동 재시도하지 않습니다. 다시 불러와 확인하세요.", "无法确认更改，不会自动重试。请重新加载并检查。", "無法確認變更，不會自動重試。請重新載入並檢查。"],
        ["Done"] = ["反映を確認しました。", "Changes verified.", "Änderungen bestätigt.", "Modifications vérifiées.", "변경을 확인했습니다.", "已确认更改。", "已確認變更。"],
        ["Applying"] = ["反映を確認中…", "Verifying changes…", "Änderungen werden geprüft…", "Vérification…", "변경 확인 중…", "正在确认更改…", "正在確認變更…"],
        ["Error"] = ["処理に失敗しました。ログを確認してください。", "Operation failed. Check the log.", "Fehler. Protokoll prüfen.", "Échec. Consultez le journal.", "작업 실패. 로그를 확인하세요.", "操作失败，请检查日志。", "操作失敗，請檢查紀錄。"],
        ["Pending"] = ["変更するバー数", "Changed bars", "Geänderte Leisten", "Barres modifiées", "변경된 단축바", "更改的栏数", "變更的列數"],
        ["Empty"] = ["空き", "Empty", "Leer", "Vide", "빈 슬롯", "空", "空"],
        ["Retry"] = ["設定保存を再試行", "Retry settings save", "Speichern wiederholen", "Réessayer la sauvegarde", "설정 저장 재시도", "重试保存设置", "重試儲存設定"],
    };
    public static string Get(string? code, string key)
    {
        if (!Text.TryGetValue(key, out var row)) return key;
        int index = Array.IndexOf(Codes, code); return row[index < 0 ? 1 : index];
    }
    public static string FontText => string.Concat(Text.Values.SelectMany(x => x)) + "日本語EnglishDeutschFrançais한국어简体中文繁體中文";
    public static string? Normalize(string? value)
    {
        string code = (value ?? "").Replace('_', '-').ToLowerInvariant();
        if (code.StartsWith("zh-hans") || code is "zh-cn" or "zh-sg") return "zh-Hans";
        if (code.StartsWith("zh-hant") || code is "zh-tw" or "zh-hk" or "zh-mo") return "zh-Hant";
        string first = code.Split('-')[0]; return first is "ja" or "en" or "de" or "fr" or "ko" ? first : null;
    }
}
