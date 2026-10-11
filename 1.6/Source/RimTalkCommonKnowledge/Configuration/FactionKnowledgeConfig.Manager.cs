using UnityEngine;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 常识管理页面（FR-16 / D49）常量（<see cref="FactionKnowledgeConfig"/> 的 partial 分片）：
    /// 设置页入口按钮文案、自建窗口的标题 / 导航 / 操作文案与尺寸。
    /// 界面文案一律集中于此，便于统一修改；尺寸避免散落魔数。
    /// 文案为随游戏语言翻译的只读属性（Keyed 键 RTCK.Mgr / Imp / Edt / Set），
    /// 尺寸 / 颜色 / 字符上限与纯排版符号（分隔符、缩进前缀）保持常量不译。
    /// </summary>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 设置页入口 ----------

        /// <summary>设置项标题：打开常识管理页面。</summary>
        public static string MOD_SETTINGS_OPEN_MANAGER => "RTCK.Set.OpenManager".Translate();

        /// <summary>设置项说明：打开常识管理页面。</summary>
        public static string MOD_SETTINGS_OPEN_MANAGER_TIP => "RTCK.Set.OpenManagerTip".Translate();

        // ---------- 窗口文案 ----------

        /// <summary>窗口标题。</summary>
        public static string MANAGER_TITLE => "RTCK.Mgr.Title".Translate();

        /// <summary>导航项：全部条目。</summary>
        public static string MANAGER_NAV_ALL => "RTCK.Mgr.NavAll".Translate();

        /// <summary>导航项：自动注入。</summary>
        public static string MANAGER_NAV_OWN => "RTCK.Mgr.NavOwn".Translate();

        /// <summary>导航子项：本 mod 注入的派系信息。</summary>
        public static string MANAGER_NAV_OWN_FACTION => "RTCK.Mgr.NavOwnFaction".Translate();

        /// <summary>导航子项：本 mod 注入的异种人信息。</summary>
        public static string MANAGER_NAV_OWN_XENOTYPE => "RTCK.Mgr.NavOwnXenotype".Translate();

        /// <summary>导航项：自建库。</summary>
        public static string MANAGER_NAV_USER => "RTCK.Mgr.NavUser".Translate();

        /// <summary>导航分组标题：预设库。</summary>
        public static string MANAGER_NAV_PRESET => "RTCK.Mgr.NavPreset".Translate();

        /// <summary>导航分组标题：自定义子页。</summary>
        public static string MANAGER_NAV_CUSTOM => "RTCK.Mgr.NavCustom".Translate();

        /// <summary>导航项：新建子页。</summary>
        public static string MANAGER_NEW_PAGE => "RTCK.Mgr.NewPage".Translate();

        /// <summary>操作按钮：把勾选项加入当前子页。</summary>
        public static string MANAGER_ADD_TO_PAGE => "RTCK.Mgr.AddToPage".Translate();

        /// <summary>操作按钮：把勾选项移出当前子页。</summary>
        public static string MANAGER_REMOVE_FROM_PAGE => "RTCK.Mgr.RemoveFromPage".Translate();

        /// <summary>操作按钮：重命名当前子页。</summary>
        public static string MANAGER_RENAME_PAGE => "RTCK.Mgr.RenamePage".Translate();

        /// <summary>操作按钮：删除当前子页。</summary>
        public static string MANAGER_DELETE_PAGE => "RTCK.Mgr.DeletePage".Translate();

        /// <summary>操作按钮：确定（重命名 / 删除确认）。</summary>
        public static string MANAGER_CONFIRM => "RTCK.Mgr.Confirm".Translate();

        /// <summary>操作按钮：取消（重命名 / 删除确认）。</summary>
        public static string MANAGER_CANCEL => "RTCK.Mgr.Cancel".Translate();

        /// <summary>操作按钮：全选当前列表。</summary>
        public static string MANAGER_SELECT_ALL => "RTCK.Mgr.SelectAll".Translate();

        /// <summary>操作按钮：清空勾选。</summary>
        public static string MANAGER_CLEAR_SELECTION => "RTCK.Mgr.ClearSelection".Translate();

        /// <summary>操作按钮：刷新（重新读取常识库与来源索引）。</summary>
        public static string MANAGER_REFRESH => "RTCK.Mgr.Refresh".Translate();

        /// <summary>搜索框占位文案（输入框为空时以灰字绘制）。</summary>
        public static string MANAGER_SEARCH_PLACEHOLDER => "RTCK.Mgr.SearchPlaceholder".Translate();

        /// <summary>提示：无存档时窗口正文。</summary>
        public static string MANAGER_NO_GAME => "RTCK.Mgr.NoGame".Translate();

        /// <summary>提示：当前未选中任何自定义子页时，加入 / 移出按钮不可用的原因。</summary>
        public static string MANAGER_NO_PAGE_TIP => "RTCK.Mgr.NoPageTip".Translate();

        /// <summary>删除确认文案。</summary>
        public static string MANAGER_CONFIRM_DELETE => "RTCK.Mgr.ConfirmDelete".Translate();

        // ---------- 条目写操作（FR-18 ~ FR-21） ----------

        /// <summary>条目操作按钮：新增条目。</summary>
        public static string MANAGER_NEW_ENTRY => "RTCK.Mgr.NewEntry".Translate();

        /// <summary>条目操作按钮：编辑选中条目。</summary>
        public static string MANAGER_EDIT_ENTRY => "RTCK.Mgr.EditEntry".Translate();

        /// <summary>条目操作按钮：删除选中条目（批量）。</summary>
        public static string MANAGER_DELETE_ENTRIES => "RTCK.Mgr.DeleteEntries".Translate();

        /// <summary>提示：编辑按钮在勾选数不为 1 时不可用的原因。</summary>
        public static string MANAGER_EDIT_SINGLE_TIP => "RTCK.Mgr.EditSingleTip".Translate();

        /// <summary>提示：删除按钮在无勾选时不可用的原因。</summary>
        public static string MANAGER_NO_SELECTION_TIP => "RTCK.Mgr.NoSelectionTip".Translate();

        /// <summary>列表行：已锁定条目的后缀标识。</summary>
        public static string MANAGER_LOCKED_SUFFIX => "RTCK.Mgr.LockedSuffix".Translate();

        /// <summary>底部第一行：把全部勾选条目设为启用。</summary>
        public static string MANAGER_BATCH_ENABLE => "RTCK.Mgr.BatchEnable".Translate();

        /// <summary>底部第一行：把全部勾选条目设为停用。</summary>
        public static string MANAGER_BATCH_DISABLE => "RTCK.Mgr.BatchDisable".Translate();

        /// <summary>批量启用结果消息。{0}=本次实际改变条数。</summary>
        public static string MANAGER_BATCH_ENABLE_RESULT_FORMAT => "RTCK.Mgr.BatchEnableResultFormat".Translate();

        /// <summary>批量停用结果消息。{0}=本次实际改变条数。</summary>
        public static string MANAGER_BATCH_DISABLE_RESULT_FORMAT => "RTCK.Mgr.BatchDisableResultFormat".Translate();

        /// <summary>删除条目确认文案格式。{0}=本次删除条数。</summary>
        public static string MANAGER_CONFIRM_DELETE_ENTRIES_FORMAT => "RTCK.Mgr.ConfirmDeleteEntriesFormat".Translate();

        /// <summary>删除确认中的警告：本 mod 注入条目删除后会被定时刷新重新注入。</summary>
        public static string MANAGER_DELETE_OWN_WARNING => "RTCK.Mgr.DeleteOwnWarning".Translate();

        // ---------- 导入 / 导出 ----------

        /// <summary>操作按钮：导入条目（打开导入对话框）。</summary>
        public static string MANAGER_IMPORT_ENTRIES => "RTCK.Mgr.ImportEntries".Translate();

        /// <summary>操作按钮：导出条目（复制到剪贴板）。</summary>
        public static string MANAGER_EXPORT_ENTRIES => "RTCK.Mgr.ExportEntries".Translate();

        /// <summary>导出按钮悬停说明：范围口径（勾选优先，否则当前列表）。</summary>
        public static string MANAGER_EXPORT_TIP => "RTCK.Mgr.ExportTip".Translate();

        /// <summary>导入按钮悬停说明。</summary>
        public static string MANAGER_IMPORT_TIP => "RTCK.Mgr.ImportTip".Translate();

        /// <summary>导出结果提示。{0}=条数。</summary>
        public static string MANAGER_EXPORT_RESULT_FORMAT => "RTCK.Mgr.ExportResultFormat".Translate();

        /// <summary>导出为空时的提示。</summary>
        public static string MANAGER_EXPORT_EMPTY => "RTCK.Mgr.ExportEmpty".Translate();

        // ---------- 导入对话框（Dialog_ImportKnowledge） ----------

        /// <summary>导入对话框标题。</summary>
        public static string IMPORTER_TITLE => "RTCK.Imp.Title".Translate();

        /// <summary>按钮：读取剪贴板内容。</summary>
        public static string IMPORTER_PASTE => "RTCK.Imp.Paste".Translate();

        /// <summary>按钮：清空文本区。</summary>
        public static string IMPORTER_CLEAR => "RTCK.Imp.Clear".Translate();

        /// <summary>复选框：跳过重复条目。</summary>
        public static string IMPORTER_SKIP_DUPLICATES => "RTCK.Imp.SkipDuplicates".Translate();

        /// <summary>未去重时的警告：会产生重复条目。</summary>
        public static string IMPORTER_DUPLICATE_WARNING => "RTCK.Imp.DuplicateWarning".Translate();

        /// <summary>统计格式。{0}=有效行数，{1}=实际将导入条数。</summary>
        public static string IMPORTER_STATS_FORMAT => "RTCK.Imp.StatsFormat".Translate();

        /// <summary>没有可导入内容时的错误。</summary>
        public static string IMPORTER_ERROR_EMPTY => "RTCK.Imp.ErrorEmpty".Translate();

        /// <summary>全部为重复条目时的错误。</summary>
        public static string IMPORTER_ERROR_ALL_DUPLICATE => "RTCK.Imp.ErrorAllDuplicate".Translate();

        /// <summary>导入结果提示。{0}=条数。</summary>
        public static string IMPORTER_RESULT_FORMAT => "RTCK.Imp.ResultFormat".Translate();

        /// <summary>导入条目录入当前子页的追加提示。{0}=条数，{1}=子页名。</summary>
        public static string IMPORTER_ADDED_TO_PAGE_FORMAT => "RTCK.Imp.AddedToPageFormat".Translate();

        /// <summary>导入对话框宽。</summary>
        public const float IMPORTER_WINDOW_WIDTH = 560f;

        /// <summary>导入文本区显示行数。</summary>
        public const int IMPORTER_CONTENT_LINES = 14;

        /// <summary>导入文本区高度：行数 × 字体行高 + 文本域上下内边距。</summary>
        public static readonly float IMPORTER_CONTENT_AREA_HEIGHT =
            IMPORTER_CONTENT_LINES * Text.LineHeight + Text.CurTextAreaStyle.padding.vertical;

        /// <summary>导入对话框初始高：边距 + 标题 + 文本区 + 工具行 + 去重行 + 统计行 + 按钮行。</summary>
        public static readonly float IMPORTER_WINDOW_HEIGHT =
            EDITOR_WINDOW_MARGIN * 2f
            + MANAGER_HEADER_HEIGHT + EDITOR_GAP
            + IMPORTER_CONTENT_AREA_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT
            + MANAGER_NAV_ROW_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT;

        // ---------- 编辑对话框（Dialog_EditKnowledgeEntry） ----------

        /// <summary>编辑对话框标题（编辑现有条目）。</summary>
        public static string EDITOR_TITLE_EDIT => "RTCK.Edt.TitleEdit".Translate();

        /// <summary>编辑对话框标题（新增条目）。</summary>
        public static string EDITOR_TITLE_NEW => "RTCK.Edt.TitleNew".Translate();

        /// <summary>字段标签：标签。</summary>
        public static string EDITOR_TAG_LABEL => "RTCK.Edt.TagLabel".Translate();

        /// <summary>字段标签：正文。</summary>
        public static string EDITOR_CONTENT_LABEL => "RTCK.Edt.ContentLabel".Translate();

        /// <summary>字段标签：重要度。</summary>
        public static string EDITOR_IMPORTANCE_LABEL => "RTCK.Edt.ImportanceLabel".Translate();

        /// <summary>复选框：启用此条目。</summary>
        public static string EDITOR_ENABLED => "RTCK.Edt.Enabled".Translate();

        /// <summary>字段标签：扩展开关组。</summary>
        public static string EDITOR_EXTENSION_LABEL => "RTCK.Edt.ExtensionLabel".Translate();

        /// <summary>复选框：允许提取。</summary>
        public static string EDITOR_EXTRACT_LABEL => "RTCK.Edt.ExtractLabel".Translate();

        /// <summary>复选框：允许匹配。</summary>
        public static string EDITOR_MATCH_LABEL => "RTCK.Edt.MatchLabel".Translate();

        /// <summary>悬停说明：允许提取的含义。</summary>
        public static string EDITOR_EXTRACT_TIP => "RTCK.Edt.ExtractTip".Translate();

        /// <summary>悬停说明：允许匹配的含义。</summary>
        public static string EDITOR_MATCH_TIP => "RTCK.Edt.MatchTip".Translate();

        /// <summary>复选框：锁定此条目（不被自动刷新覆盖）。</summary>
        public static string EDITOR_LOCK => "RTCK.Edt.Lock".Translate();

        /// <summary>操作按钮：保存。</summary>
        public static string EDITOR_SAVE => "RTCK.Edt.Save".Translate();

        /// <summary>错误提示：标签为空。</summary>
        public static string EDITOR_ERROR_TAG_EMPTY => "RTCK.Edt.ErrorTagEmpty".Translate();

        /// <summary>错误提示：正文为空。</summary>
        public static string EDITOR_ERROR_CONTENT_EMPTY => "RTCK.Edt.ErrorContentEmpty".Translate();

        /// <summary>错误提示：标签或正文含换行。</summary>
        public static string EDITOR_ERROR_SINGLE_LINE => "RTCK.Edt.ErrorSingleLine".Translate();

        /// <summary>错误提示：重要度不是 0~1 的数字。</summary>
        public static string EDITOR_ERROR_IMPORTANCE => "RTCK.Edt.ErrorImportance".Translate();

        /// <summary>错误提示：更新保存失败。</summary>
        public static string EDITOR_SAVE_FAILED => "RTCK.Edt.SaveFailed".Translate();

        /// <summary>错误提示：新增失败。</summary>
        public static string EDITOR_ADD_FAILED => "RTCK.Edt.AddFailed".Translate();

        /// <summary>锁定复选框旁的说明：保存后自动锁定。</summary>
        public static string EDITOR_LOCK_TIP => "RTCK.Edt.LockTip".Translate();

        /// <summary>锁定说明的显示行数（文案较长，一行会被截断）。</summary>
        public const int EDITOR_LOCK_TIP_LINES = 2;

        /// <summary>锁定说明区高：两行小字，顶对齐自动换行。</summary>
        public static readonly float EDITOR_LOCK_TIP_HEIGHT =
            EDITOR_LOCK_TIP_LINES * Text.LineHeight;

        /// <summary>计数格式：{0}=当前列表条数，{1}=库内总条数，{2}=勾选条数。</summary>
        public static string MANAGER_COUNT_FORMAT => "RTCK.Mgr.CountFormat".Translate();

        /// <summary>新建子页的默认名格式。{0}=序号。</summary>
        public static string MANAGER_DEFAULT_PAGE_NAME => "RTCK.Mgr.DefaultPageName".Translate();

        /// <summary>列表行：来源分隔符（标签 —— 来源）。随语言翻译（中文双破折号 / 英文单破折号）。</summary>
        public static string MANAGER_ROW_SOURCE_SEPARATOR => "RTCK.Mgr.RowSourceSeparator".Translate();

        /// <summary>来源里「块 / 模组」的连接符。</summary>
        public const string MANAGER_SOURCE_JOINER = "/";

        /// <summary>导航块行格式：{0}=块名，{1}=块内模组文件数。</summary>
        public static string MANAGER_BLOCK_ROW_FORMAT => "RTCK.Mgr.BlockRowFormat".Translate();

        /// <summary>导航自定义子页行格式：{0}=子页名，{1}=成员条数。</summary>
        public static string MANAGER_PAGE_ROW_FORMAT => "RTCK.Mgr.PageRowFormat".Translate();

        /// <summary>导航前缀：块已展开。</summary>
        public const string MANAGER_EXPANDED_PREFIX = "- ";

        /// <summary>导航前缀：块未展开。</summary>
        public const string MANAGER_COLLAPSED_PREFIX = "+ ";

        /// <summary>行内省略号。</summary>
        public const string MANAGER_ELLIPSIS = "…";

        /// <summary>提示文本（tooltip）内的换段分隔：用于拼接「标签 + 正文」或「说明 + 原因」。</summary>
        public const string MANAGER_TIP_BLOCK_SEPARATOR = "\n\n";

        // ---------- 布局尺寸（像素）----------

        /// <summary>窗口初始宽。</summary>
        public const float MANAGER_WINDOW_WIDTH = 1040f;

        /// <summary>窗口初始高。</summary>
        public const float MANAGER_WINDOW_HEIGHT = 720f;

        /// <summary>窗口内容边距。</summary>
        public const float MANAGER_MARGIN = 8f;

        /// <summary>标题行高。</summary>
        public const float MANAGER_HEADER_HEIGHT = 32f;

        /// <summary>左侧导航列宽。</summary>
        public const float MANAGER_NAV_WIDTH = 300f;

        /// <summary>导航行高（单行）。</summary>
        public const float MANAGER_NAV_ROW_HEIGHT = 26f;

        /// <summary>导航子页标题最多显示的行数（长 mod 名可折到第二行）。</summary>
        public const int MANAGER_NAV_MAX_TITLE_LINES = 2;

        /// <summary>导航行高上限：标题占满两行时的高度。</summary>
        public const float MANAGER_NAV_ROW_MAX_HEIGHT =
            MANAGER_NAV_ROW_HEIGHT * MANAGER_NAV_MAX_TITLE_LINES;

        /// <summary>导航分组标题行高。</summary>
        public const float MANAGER_NAV_HEADER_HEIGHT = 22f;

        /// <summary>导航模组行相对块行的缩进。</summary>
        public const float MANAGER_NAV_INDENT = 16f;

        /// <summary>条目行正文预览的显示行数。</summary>
        public const int MANAGER_ROW_CONTENT_LINES = 2;

        /// <summary>条目行高：顶留白 + 标签行 + 两行正文 + 底留白。</summary>
        public const float MANAGER_ROW_HEIGHT =
            MANAGER_ROW_LINE_Y
            + MANAGER_ROW_LINE_HEIGHT
            + MANAGER_ROW_CONTENT_LINES * MANAGER_ROW_LINE_HEIGHT
            + MANAGER_ROW_LINE_Y;

        /// <summary>条目行右侧停用标记竖色条的宽度。</summary>
        public const float MANAGER_ROW_BADGE_WIDTH = 6f;

        /// <summary>搜索行高。</summary>
        public const float MANAGER_SEARCH_HEIGHT = 28f;

        /// <summary>底部操作栏高度（三行按钮：勾选操作 + 条目操作（新增 / 编辑 / 删除 / 导入 / 导出）+ 子页操作）。</summary>
        public const float MANAGER_ACTION_BAR_HEIGHT = 102f;

        /// <summary>控件之间的常规间距。</summary>
        public const float MANAGER_GAP = 6f;

        /// <summary>滚动视图预留的滚动条宽度。</summary>
        public const float MANAGER_SCROLLBAR_WIDTH = 16f;

        /// <summary>操作按钮的默认宽度。</summary>
        public const float MANAGER_BUTTON_WIDTH = 120f;

        /// <summary>重命名输入框宽度。</summary>
        public const float MANAGER_RENAME_FIELD_WIDTH = 220f;

        /// <summary>行内标签最多显示的字符数（超出截断加省略号）。</summary>
        public const int MANAGER_TAG_MAX_CHARS = 40;

        /// <summary>行内正文预览最多显示的字符数（按两行宽度取保守值，超长全文靠行 Tooltip 查看）。</summary>
        public const int MANAGER_CONTENT_MAX_CHARS = 96;

        /// <summary>子页名称的最大字符数（输入框上限，防止写出超长名字）。</summary>
        public const int MANAGER_PAGE_NAME_MAX_CHARS = 24;

        /// <summary>条目行左侧复选框的边长。</summary>
        public const float MANAGER_CHECKBOX_SIZE = 22f;

        /// <summary>条目行内文本（标签 / 预览）单行的高度。</summary>
        public const float MANAGER_ROW_LINE_HEIGHT = 19f;

        /// <summary>条目行内第一行文本距行顶的距离。</summary>
        public const float MANAGER_ROW_LINE_Y = 4f;

        /// <summary>行内元素与行边界的水平留白。</summary>
        public const float MANAGER_ROW_INDENT = 4f;

        /// <summary>底部操作栏单行按钮的高度。</summary>
        public const float MANAGER_ACTION_ROW_HEIGHT = 30f;

        /// <summary>次级文本（正文预览）的颜色。</summary>
        public static readonly Color MANAGER_SECONDARY_COLOR = new Color(0.78f, 0.78f, 0.78f);

        /// <summary>停用条目的文本颜色（标签与正文统一压灰，作为整行停用标记）。</summary>
        public static readonly Color MANAGER_DISABLED_COLOR = new Color(0.55f, 0.55f, 0.55f);

        /// <summary>停用条目右侧竖色条的颜色（警示红；仅停用行绘制）。</summary>
        public static readonly Color MANAGER_DISABLED_BADGE_COLOR = new Color(0.9f, 0.35f, 0.3f);

        /// <summary>提示文本（如搜索框占位符）的颜色。</summary>
        public static readonly Color MANAGER_HINT_COLOR = new Color(0.62f, 0.62f, 0.62f);

        // ---------- 编辑对话框布局尺寸（像素） ----------

        /// <summary>编辑对话框初始宽。</summary>
        public const float EDITOR_WINDOW_WIDTH = 500f;

        /// <summary>窗口边距（与 <see cref="Window"/> 默认标准边距对齐，作为窗口高公式的组成项）。</summary>
        public const float EDITOR_WINDOW_MARGIN = 18f;

        /// <summary>锁定块（开关行 + 两行提示 + 间距）所占高度。</summary>
        public static readonly float EDITOR_LOCK_BLOCK_HEIGHT =
            EDITOR_FIELD_HEIGHT + EDITOR_LOCK_TIP_HEIGHT + EDITOR_GAP;

        /// <summary>正文文本域的显示行数。</summary>
        public const int EDITOR_CONTENT_LINES = 10;

        /// <summary>
        /// 正文文本域高度：行数 × 当前字体行高，再补文本域样式上下内边距，保证各行完整可见。
        /// 运行时取值（字体行高与样式内边距均由游戏 GUI 提供），故为 static readonly 而非 const。
        /// </summary>
        public static readonly float EDITOR_CONTENT_AREA_HEIGHT =
            EDITOR_CONTENT_LINES * Text.LineHeight + Text.CurTextAreaStyle.padding.vertical;

        /// <summary>
        /// 编辑对话框初始高：按最完整布局（锁定块 + 错误行预留）一次算足，
        /// 新增 / 无错时底部自然留白，按钮与错误行锚底不与表单重叠。
        /// </summary>
        public static readonly float EDITOR_WINDOW_HEIGHT =
            EDITOR_WINDOW_MARGIN * 2f
            + MANAGER_HEADER_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT + EDITOR_GAP
            + EDITOR_CONTENT_AREA_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT + EDITOR_GAP
            + EDITOR_LOCK_BLOCK_HEIGHT
            + EDITOR_FIELD_HEIGHT + EDITOR_GAP
            + EDITOR_FIELD_HEIGHT;

        /// <summary>编辑对话框左侧字段标签列宽。</summary>
        public const float EDITOR_LABEL_WIDTH = 56f;

        /// <summary>编辑对话框输入控件高。</summary>
        public const float EDITOR_FIELD_HEIGHT = 28f;

        /// <summary>编辑对话框控件之间的垂直间距。</summary>
        public const float EDITOR_GAP = 8f;

        /// <summary>编辑对话框底部按钮宽。</summary>
        public const float EDITOR_BUTTON_WIDTH = 100f;

        /// <summary>标签输入框字符上限。</summary>
        public const int EDITOR_TAG_MAX_CHARS = 200;

        /// <summary>正文输入框字符上限。</summary>
        public const int EDITOR_CONTENT_MAX_CHARS = 600;

        /// <summary>重要度输入框宽。</summary>
        public const float EDITOR_IMPORTANCE_FIELD_WIDTH = 80f;

        /// <summary>扩展开关复选框（含标签文字）的占位宽，两个并排时各取此宽。</summary>
        public const float EDITOR_CHECKBOX_WIDTH = 120f;

        /// <summary>编辑对话框错误信息文本颜色。</summary>
        public static readonly Color EDITOR_ERROR_COLOR = new Color(1f, 0.42f, 0.42f);
    }
}
