# Bảng dịch chuỗi cho Giai đoạn 4 (A1 Regex Tester · A2 kéo-thả quy tắc · A3 màn phụ · A4 command palette).
# Dùng: .\packaging\add-strings.ps1
# LƯU Ý: file .ps1 phải lưu UTF-8 CÓ BOM để PowerShell 5.1 đọc đúng tiếng Việt.
$ErrorActionPreference = 'Stop'

# Định dạng mỗi dòng: Key|vi|en|fr|zh-Hans
$table = @'
# ---------- A5: nhãn tham số quy tắc ----------
ParamFindReplacePattern|Mẫu (pattern)|Pattern|Motif (pattern)|模式（pattern）
ParamFindReplaceReplacement|Thay bằng|Replace with|Remplacer par|替换为
ParamFindReplaceUseRegex|Dùng Regex|Use regex|Utiliser regex|使用正则表达式
ParamFindReplaceIgnoreCase|Không phân biệt hoa/thường|Ignore case|Ignorer la casse|忽略大小写
ParamFindReplaceReplaceAll|Thay tất cả|Replace all|Tout remplacer|全部替换
ParamFindReplaceScope|Phạm vi|Scope|Portée|范围
ParamNumberingStart|Bắt đầu|Start|Début|起始值
ParamNumberingStep|Bước nhảy|Step|Pas|步长
ParamNumberingWidth|Số chữ số|Zero-padding width|Largeur de remplissage|补零宽度
ParamNumberingPosition|Vị trí|Position|Position|位置
ParamNumberingSeparator|Dấu ngăn|Separator|Séparateur|分隔符
ParamNumberingStyle|Kiểu số|Number style|Style de numérotation|编号样式
ParamNumberingResetBy|Đặt lại theo|Reset by|Réinitialiser par|重置依据
ParamTrimChars|Xóa ký tự|Characters to remove|Caractères à supprimer|要删除的字符
ParamTrimCollapseSpaces|Gộp khoảng trắng|Collapse spaces|Fusionner les espaces|合并空格
ParamTrimTrimEnds|Cắt khoảng trắng đầu/cuối|Trim leading/trailing spaces|Supprimer les espaces en bord|去除首尾空格
ParamTrimRemoveTrailingDot|Xóa dấu chấm/khoảng trắng cuối|Remove trailing dot/space|Supprimer point/espace final|删除末尾的点或空格
ParamTrimRemoveFirstN|Xóa N ký tự đầu|Remove first N chars|Supprimer les N premiers caractères|删除开头 N 个字符
ParamTrimRemoveLastN|Xóa N ký tự cuối|Remove last N chars|Supprimer les N derniers caractères|删除结尾 N 个字符
ParamInsertText|Văn bản/token|Text/token|Texte/token|文本/token
ParamInsertPosition|Vị trí|Position|Position|位置
ParamInsertIndex|Vị trí N|Index N|Index N|位置 N
ParamInsertCountFromEnd|Đếm từ cuối|Count from end|Compter depuis la fin|从末尾计数
ParamCaseStyle|Kiểu|Case style|Style de casse|大小写样式
ParamCaseScope|Phạm vi|Scope|Portée|范围
ParamCaseExceptions|Từ ngoại lệ|Exceptions|Exceptions|例外词
ParamSanitizeDiacritics|Bỏ dấu tiếng Việt|Remove Vietnamese diacritics|Retirer les diacritiques vietnamiens|去除越南语变音符号
ParamSanitizeNfc|Chuẩn hóa Unicode NFC|Normalize Unicode NFC|Normaliser Unicode NFC|规范化 Unicode NFC
ParamSanitizeInvalidChars|Thay ký tự không hợp lệ|Replace invalid characters|Remplacer les caractères invalides|替换非法字符
ParamSanitizeInvalidCharReplacement|Ký tự thay thế|Replacement character|Caractère de remplacement|替换字符
ParamSanitizeSpaces|Khoảng trắng|Spaces|Espaces|空格
ParamSanitizeSlug|Slug (a-z0-9-)|Slug (a-z0-9-)|Slug (a-z0-9-)|Slug (a-z0-9-)
ParamDateSource|Nguồn|Source|Source|来源
ParamDateFormat|Định dạng|Format|Format|格式
ParamDatePosition|Vị trí|Position|Position|位置
ParamDateSeparator|Dấu ngăn|Separator|Séparateur|分隔符
ParamDateUtc|Dùng UTC|Use UTC|Utiliser UTC|使用 UTC
ParamExtensionMode|Chế độ|Mode|Mode|模式
ParamExtensionNew|Đuôi mới|New extension|Nouvelle extension|新扩展名
ParamExtensionAllowChange|Cho phép đổi đuôi|Allow extension change|Autoriser le changement d extension|允许更改扩展名
ParamTemplateText|Mẫu tên|Name template|Modèle de nom|名称模板
# ---------- A1: Trình thử Regex ----------
RegexTesterTitle|Trình thử Regex|Regex tester|Testeur de regex|正则表达式测试器
RegexTesterPattern|Mẫu Regex|Regex pattern|Motif regex|正则模式
RegexTesterReplacement|Thay bằng|Replace with|Remplacer par|替换为
RegexTesterInput|Chuỗi thử|Test string|Chaîne de test|测试字符串
RegexTesterIgnoreCase|Bỏ qua hoa/thường|Ignore case|Ignorer la casse|忽略大小写
RegexTesterResult|Kết quả|Result|Résultat|结果
RegexTesterMatch|Khớp|Match|Correspondance|匹配
RegexTesterNoMatch|Không khớp|No match|Aucune correspondance|无匹配
RegexTesterGroup|Nhóm {0}|Group {0}|Groupe {0}|组 {0}
RegexTesterError|Lỗi Regex: {0}|Regex error: {0}|Erreur regex : {0}|正则错误：{0}
RegexTesterApplyAll|Dùng cho tất cả tệp|Apply to all files|Appliquer à tous les fichiers|应用到所有文件
RegexTesterLibrary|Thư viện mẫu|Sample library|Bibliothèque d exemples|示例库
RegexTesterUseSelected|Dùng tên đang chọn|Use selected name|Utiliser le nom sélectionné|使用当前选中项
RegexTesterNeedFindReplace|Cần một quy tắc Tìm và thay đang bật Regex để áp dụng kết quả.|A Find and replace rule with regex enabled is required to apply the result.|Une règle Rechercher-remplacer avec regex est requise pour appliquer le résultat.|需要启用正则表达式的"查找和替换"规则才能应用结果。
RegexTesterApplied|Đã cập nhật quy tắc Tìm và thay.|Find and replace rule updated.|Règle Rechercher-remplacer mise à jour.|已更新"查找和替换"规则。
# ---------- A2: kéo-thả đổi thứ tự quy tắc ----------
RuleMoveUp|Đưa quy tắc lên|Move rule up|Monter la règle|上移规则
RuleMoveDown|Đưa quy tắc xuống|Move rule down|Descendre la règle|下移规则
RuleDragHint|Kéo để đổi thứ tự|Drag to reorder|Glisser pour réordonner|拖动以排序
# ---------- A3: màn phụ & cài đặt ----------
SettingsCheckUpdateNow|Kiểm tra ngay|Check now|Vérifier maintenant|立即检查
SettingsUpdateChecking|Đang kiểm tra cập nhật...|Checking for updates...|Recherche de mise à jour...|正在检查更新…
SettingsUpdateFound|Có bản mới: {0}|New version available: {0}|Nouvelle version disponible : {0}|有新版本：{0}
SettingsUpdateNone|Bạn đang dùng bản mới nhất ({0}).|You are on the latest version ({0}).|Vous utilisez la dernière version ({0}).|您使用的是最新版本（{0}）。
SettingsUpdateFailed|Không kiểm tra được cập nhật: {0}|Could not check for updates: {0}|Impossible de vérifier les mises à jour : {0}|无法检查更新：{0}
PresetLibrarySearch|Tìm preset...|Search presets...|Rechercher un préréglage...|搜索预设…
PresetLibraryEmpty|Chưa có preset nào trong thư viện.|The library is empty.|La bibliothèque est vide.|预设库为空。
WatchFolderEmpty|Chưa theo dõi thư mục nào.|No folder is being watched.|Aucun dossier surveillé.|尚未监视任何文件夹。
WatchFolderRecent|Sự kiện gần đây|Recent events|Événements récents|最近事件
HistoryEmpty|Chưa có lần áp dụng nào.|No rename runs yet.|Aucune exécution de renommage.|尚无重命名记录。
HistoryHint|Xem các lần đổi tên trước và hoàn tác khi cần.|View previous rename runs and undo them when needed.|Consulter les exécutions de renommage précédentes et les annuler.|查看之前的重命名记录并可撤销。
HistoryDetail|Chi tiết bước|Step details|Détails de l étape|步骤详情
HistoryUndo|Hoan tác lần này|Undo this run|Annuler cette exécution|撤销此次操作
# ---------- A4: command palette ----------
CommandPaletteTitle|Lệnh nhanh|Command palette|Palette de commandes|命令面板
CommandPaletteSearch|Gõ để tìm lệnh...|Type to search commands...|Tapez pour rechercher...|输入以搜索命令…
CommandPaletteEmpty|Không tìm thấy lệnh nào.|No matching command.|Aucune commande trouvée.|没有匹配的命令。
CommandPaletteHint|Esc đóng · Enter chạy · ↑↓ chọn|Esc closes · Enter runs · ↑↓ selects|Échap ferme · Entrée exécute · ↑↓ sélectionne|Esc 关闭 · Enter 执行 · ↑↓ 选择
# ---------- A5 bổ sung: tên quy tắc, tóm tắt, nhãn lựa chọn ----------
RuleTypeFindReplace|Tìm và thay|Find and replace|Rechercher et remplacer|查找和替换
RuleTypeNumbering|Số thứ tự|Numbering|Numérotation|编号
RuleTypeTrim|Cắt tỉa|Trim|Rogner|修剪
RuleTypeInsert|Chèn|Insert|Insérer|插入
RuleTypeCase|Hoa/thường|Case|Casse|大小写
RuleTypeSanitize|Bỏ dấu và chuẩn hóa|Sanitize|Assainir|清理
RuleTypeDate|Ngày tháng|Date|Date|日期
RuleTypeExtension|Phần mở rộng|Extension|Extension|扩展名
RuleTypeTemplate|Mẫu tên|Name template|Modèle de nom|名称模板
RuleSummaryFindReplace|{0} → {1} {2}|{0} → {1} {2}|{0} → {1} {2}|{0} → {1} {2}
RuleSummaryNumbering|bắt đầu {0}, bước {1}, {2} chữ số|start {0}, step {1}, {2} digits|début {0}, pas {1}, {2} chiffres|起始 {0}，步长 {1}，{2} 位数字
RuleSummaryTrim|bỏ ký tự, gộp và cắt khoảng trắng|strip chars, collapse and trim spaces|supprimer des caractères, fusionner et rogner les espaces|删除字符，合并并去除空格
RuleSummaryInsert|{0} ← {1}|{0} ← {1}|{0} ← {1}|{0} ← {1}
RuleSummaryCase|{0} · {1}|{0} · {1}|{0} · {1}|{0} · {1}
RuleSummarySanitize|bỏ dấu tiếng Việt, chuẩn hóa NFC|strip Vietnamese diacritics, NFC|supprimer les diacritiques vietnamiens, NFC|去除越南语变音符号，NFC
RuleSummaryDate|{0} · {1}|{0} · {1}|{0} · {1}|{0} · {1}
RuleSummaryExtension|{0}|{0}|{0}|{0}
RuleSummaryTemplate|{0}|{0}|{0}|{0}
ValueOn|có|on|oui|开
ValueOff|không|off|non|关
OptionName|Tên|Name|Nom|名称
OptionExtension|Phần mở rộng|Extension|Extension|扩展名
OptionBoth|Cả hai|Both|Les deux|两者
OptionStart|Đầu|Start|Début|开头
OptionEnd|Cuối|End|Fin|结尾
OptionReplaceAll|Thay tất cả|Replace all|Tout remplacer|全部替换
OptionIndex|Vị trí|Index|Index|位置
OptionDecimal|Thập phân|Decimal|Décimal|十进制
OptionLetters|Chữ cái|Letters|Lettres|字母
OptionRoman|La Mã|Roman|Romain|罗马数字
OptionNone|Không|None|Aucun|无
OptionByFolder|Theo thư mục|By folder|Par dossier|按文件夹
OptionByTokenChange|Khi token đổi|When token changes|Quand le jeton change|当标记变化时
OptionAtIndex|Tại vị trí N|At index N|À l index N|在位置 N
OptionLower|Chữ thường|Lowercase|Minuscules|小写
OptionUpper|CHỮ HOA|Uppercase|MAJUSCULES|大写
OptionTitle|Viết Hoa Chữ Đầu|Title Case|Titre|标题格式
OptionSentence|Viết Hoa Đầu Câu|Sentence case|Phrase|句首大写
OptionKeep|Giữ nguyên|Keep|Conserver|保留
OptionDash|Dấu gạch ngang|Dash|Tiret|短横线
OptionUnderscore|Dấu gạch dưới|Underscore|Tiret bas|下划线
OptionRemove|Xóa|Remove|Supprimer|删除
OptionCreated|Ngày tạo|Created|Créé le|创建时间
OptionModified|Ngày sửa|Modified|Modifié le|修改时间
OptionTaken|Ngày chụp|Taken|Pris le|拍摄时间
OptionMedia|Ngày media|Media|Média|媒体时间
OptionNow|Hiện tại|Now|Maintenant|当前时间
OptionSet|Đặt|Set|Définir|设置
# ---------- A5 bổ sung 2: trạng thái xem trước & tiêu đề cột ----------
StatusWillChange|Sẽ đổi|Will change|Sera modifié|将重命名
StatusUnchanged|Không đổi|Unchanged|Inchangé|无变化
StatusConflict|Xung đột|Conflict|Conflit|冲突
StatusInvalidName|Tên không hợp lệ|Invalid name|Nom invalide|名称无效
StatusPathTooLong|Đường dẫn quá dài|Path too long|Chemin trop long|路径过长
StatusLocked|Tệp đang bị khóa|File locked|Fichier verrouillé|文件被锁定
StatusMissingData|Thiếu dữ liệu|Missing data|Données manquantes|缺少数据
ColumnOriginalName|Tên gốc|Original name|Nom d origine|原文件名
ColumnRule|Quy tắc|Rule|Règle|规则
ColumnResult|Kết quả|Result|Résultat|结果
# ---------- A1 bổ sung: thư viện mẫu Regex ----------
RegexSampleDatePrefix|Tách tiền tố ngày|Split date prefix|Extraire le préfixe de date|拆分日期前缀
RegexSampleSpaces|Khoảng trắng thành gạch dưới|Spaces to underscores|Espaces en tirets bas|空格转下划线
RegexSampleExtension|Thêm đuôi .bak|Add .bak extension|Ajouter l extension .bak|添加 .bak 扩展名
RegexSampleDigits|Thay chữ số bằng #|Replace digits with #|Remplacer les chiffres par #|数字替换为 #
RegexSampleParentheses|Bỏ ngoặc đầu tiên|Remove leading parentheses|Supprimer les parenthèses en tête|去掉前导括号
RegexTesterHint|Esc đóng|Esc closes|Échap ferme|Esc 关闭
RegexTesterOpen|Mở trình thử Regex|Open regex tester|Ouvrir le testeur de regex|打开正则测试器
RuleDragHandleA11y|Kéo để đổi thứ tự quy tắc|Drag to reorder rules|Glisser pour réordonner les règles|拖动以调整规则顺序
# ---------- A4 bổ sung: nhãn lệnh trong Command Palette ----------
CommandCategoryFile|Tệp|Files|Fichiers|文件
CommandCategoryRules|Quy tắc|Rules|Règles|规则
CommandCategoryTools|Công cụ|Tools|Outils|工具
CommandCategoryApp|Ứng dụng|Application|Application|应用
CommandAddFiles|Thêm tệp|Add files|Ajouter des fichiers|添加文件
CommandAddFolder|Thêm thư mục|Add folder|Ajouter un dossier|添加文件夹
CommandClearList|Xóa danh sách tệp|Clear file list|Vider la liste des fichiers|清空文件列表
CommandApply|Áp dụng đổi tên|Apply renaming|Appliquer le renommage|执行重命名
CommandUndo|Hoàn tác lần đổi tên cuối|Undo last rename|Annuler le dernier renommage|撤销上次重命名
CommandSavePreset|Lưu chuỗi quy tắc thành preset|Save rules as preset|Enregistrer les règles comme préréglage|保存规则为预设
CommandImportExportPreset|Nhập hoặc xuất preset|Import or export preset|Importer ou exporter un préréglage|导入或导出预设
CommandPresetLibrary|Mở thư viện preset|Open preset library|Ouvrir la bibliothèque de préréglages|打开预设库
CommandHistory|Mở lịch sử và hoàn tác|Open history and undo|Ouvrir l historique et annuler|打开历史记录并撤销
CommandWatchFolder|Theo dõi thư mục|Watch folder|Surveiller un dossier|监视文件夹
CommandContextMenu|Menu chuột phải trong Explorer|Explorer context menu|Menu contextuel de l Explorateur|资源管理器右键菜单
CommandExportReport|Xuất báo cáo|Export report|Exporter le rapport|导出报告
CommandRegexTester|Mở trình thử Regex|Open regex tester|Ouvrir le testeur de regex|打开正则测试器
CommandSettings|Mở cài đặt|Open settings|Ouvrir les paramètres|打开设置
CommandUpgrade|Nâng cấp lên Pro|Upgrade to Pro|Mettre à niveau vers Pro|升级到专业版
CommandCheckUpdate|Kiểm tra cập nhật|Check for updates|Vérifier les mises à jour|检查更新
CommandToggleTheme|Đổi giao diện sáng hoặc tối|Toggle light or dark theme|Basculer le thème clair ou sombre|切换浅色或深色主题
# ---------- A3 bổ sung: Watch Folder ----------
WatchRecursive|Theo dõi cả thư mục con|Watch subfolders too|Surveiller aussi les sous-dossiers|同时监视子文件夹
WatchDryRun|Chỉ tính toán, không đổi tên|Dry run, do not rename|Simulation, sans renommer|仅试算，不重命名
WatchStart|Bắt đầu theo dõi|Start watching|Démarrer la surveillance|开始监视
WatchStop|Dừng theo dõi|Stop watching|Arrêter la surveillance|停止监视
WatchBrowseButton|Chọn thư mục|Browse folder|Parcourir le dossier|浏览文件夹
WatchOptionsTitle|Thiết lập theo dõi|Watch settings|Réglages de surveillance|监视设置
WatchLogTitle|Sự kiện gần đây|Recent events|Événements récents|最近事件
UpdateDownload|Tải bản mới|Download the new version|Télécharger la nouvelle version|下载新版本
'@

$files = [ordered]@{
  'vi'       = 'src\BFR.App\Resources\Strings.vi.xaml'
  'en'       = 'src\BFR.App\Resources\Strings.en.xaml'
  'fr'       = 'src\BFR.App\Resources\Strings.fr.xaml'
  'zh-Hans'  = 'src\BFR.App\Resources\Strings.zh-Hans.xaml'
}
$cols = @{ 'vi' = 1; 'en' = 2; 'fr' = 3; 'zh-Hans' = 4 }

$enc = New-Object System.Text.UTF8Encoding($false)
$rows = New-Object System.Collections.ArrayList

foreach ($line in ($table -split "`r?`n")) {
    $trim = $line.Trim()
    if ($trim.Length -eq 0 -or $trim.StartsWith('#')) { continue }
    $parts = $trim.Split('|')
    if ($parts.Count -lt 5) { throw "Dòng không hợp lệ: $trim" }
    [void]$rows.Add($parts)
}

foreach ($lang in $files.Keys) {
    $path = (Resolve-Path $files[$lang]).Path
    $content = [System.IO.File]::ReadAllText($path)
    $col = $cols[$lang]
    $added = 0

    foreach ($parts in $rows) {
        $key = $parts[0]
        if ($content.Contains('x:Key="' + $key + '"')) { continue }
        $value = $parts[$col].Replace('&', '&amp;').Replace('"', '&quot;')
        $line = '    <sys:String x:Key="' + $key + '">' + $value + '</sys:String>'
        $content = $content.Replace('</ResourceDictionary>', $line + "`r`n</ResourceDictionary>")
        $added++
    }

    [System.IO.File]::WriteAllText($path, $content, $enc)
    Write-Output ("{0}: thêm {1} khoá" -f $lang, $added)
}

foreach ($lang in $files.Keys) {
    $keys = Select-String -Path $files[$lang] -Pattern 'x:Key="([^"]+)"' -AllMatches |
        ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value }
    $dupes = @($keys | Group-Object | Where-Object { $_.Count -gt 1 })
    Write-Output ("{0}: tổng {1} khoá, trùng {2}" -f $lang, $keys.Count, $dupes.Count)
}
