using UnityEngine;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 常识管理页面（FR-16 / D49）常量（<see cref="FactionKnowledgeConfig"/> 的 partial 分片）：
    /// 设置页入口按钮文案、自建窗口的标题 / 导航 / 操作文案与尺寸。
    /// 界面文案一律集中于此，便于统一修改；尺寸避免散落魔数。
    /// </summary>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 设置页入口 ----------

        /// <summary>设置项标题：打开常识管理页面。</summary>
        public const string MOD_SETTINGS_OPEN_MANAGER = "打开常识管理页面";

        /// <summary>设置项说明：打开常识管理页面。</summary>
        public const string MOD_SETTINGS_OPEN_MANAGER_TIP =
            "按包管理本模组注入常识";

        // ---------- 窗口文案 ----------

        /// <summary>窗口标题。</summary>
        public const string MANAGER_TITLE = "常识管理";

        /// <summary>导航项：全部条目。</summary>
        public const string MANAGER_NAV_ALL = "全部条目";

        /// <summary>导航项：本 mod 注入。</summary>
        public const string MANAGER_NAV_OWN = "本 mod 注入";

        /// <summary>导航项：玩家自建。</summary>
        public const string MANAGER_NAV_USER = "玩家自建";

        /// <summary>导航分组标题：预设库。</summary>
        public const string MANAGER_NAV_PRESET = "预设库（块 → 模组）";

        /// <summary>导航分组标题：自定义子页。</summary>
        public const string MANAGER_NAV_CUSTOM = "自定义子页";

        /// <summary>导航项：新建子页。</summary>
        public const string MANAGER_NEW_PAGE = "＋ 新建子页";

        /// <summary>操作按钮：把勾选项加入当前子页。</summary>
        public const string MANAGER_ADD_TO_PAGE = "勾选项加入本页";

        /// <summary>操作按钮：把勾选项移出当前子页。</summary>
        public const string MANAGER_REMOVE_FROM_PAGE = "勾选项移出本页";

        /// <summary>操作按钮：重命名当前子页。</summary>
        public const string MANAGER_RENAME_PAGE = "重命名";

        /// <summary>操作按钮：删除当前子页。</summary>
        public const string MANAGER_DELETE_PAGE = "删除";

        /// <summary>操作按钮：确定（重命名 / 删除确认）。</summary>
        public const string MANAGER_CONFIRM = "确定";

        /// <summary>操作按钮：取消（重命名 / 删除确认）。</summary>
        public const string MANAGER_CANCEL = "取消";

        /// <summary>操作按钮：全选当前列表。</summary>
        public const string MANAGER_SELECT_ALL = "全选";

        /// <summary>操作按钮：清空勾选。</summary>
        public const string MANAGER_CLEAR_SELECTION = "清空勾选";

        /// <summary>操作按钮：刷新（重新读取常识库与来源索引）。</summary>
        public const string MANAGER_REFRESH = "刷新";

        /// <summary>搜索框占位文案（输入框为空时以灰字绘制）。</summary>
        public const string MANAGER_SEARCH_PLACEHOLDER = "搜索标签或内容…";

        /// <summary>提示：无存档时窗口正文。</summary>
        public const string MANAGER_NO_GAME = "当前没有正在进行的存档，无法读取常识库。";

        /// <summary>提示：当前未选中任何自定义子页时，加入 / 移出按钮不可用的原因。</summary>
        public const string MANAGER_NO_PAGE_TIP = "请先在左侧「自定义子页」中新建或选中一个子页。";

        /// <summary>删除确认文案。</summary>
        public const string MANAGER_CONFIRM_DELETE = "确认删除该子页？";

        /// <summary>计数格式：{0}=当前列表条数，{1}=库内总条数，{2}=勾选条数。</summary>
        public const string MANAGER_COUNT_FORMAT = "当前 {0} 条 / 共 {1} 条，勾选 {2} 条";

        /// <summary>新建子页的默认名格式。{0}=序号。</summary>
        public const string MANAGER_DEFAULT_PAGE_NAME = "新子页 {0}";

        /// <summary>列表行：来源分隔符（标签 —— 来源）。</summary>
        public const string MANAGER_ROW_SOURCE_SEPARATOR = "  ——  ";

        /// <summary>来源里「块 / 模组」的连接符。</summary>
        public const string MANAGER_SOURCE_JOINER = "/";

        /// <summary>导航块行格式：{0}=块名，{1}=块内模组文件数。</summary>
        public const string MANAGER_BLOCK_ROW_FORMAT = "{0}（{1} 个模组）";

        /// <summary>导航自定义子页行格式：{0}=子页名，{1}=成员条数。</summary>
        public const string MANAGER_PAGE_ROW_FORMAT = "{0}（{1}）";

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

        /// <summary>导航行高。</summary>
        public const float MANAGER_NAV_ROW_HEIGHT = 26f;

        /// <summary>导航分组标题行高。</summary>
        public const float MANAGER_NAV_HEADER_HEIGHT = 22f;

        /// <summary>导航模组行相对块行的缩进。</summary>
        public const float MANAGER_NAV_INDENT = 16f;

        /// <summary>条目行高。</summary>
        public const float MANAGER_ROW_HEIGHT = 46f;

        /// <summary>搜索行高。</summary>
        public const float MANAGER_SEARCH_HEIGHT = 28f;

        /// <summary>底部操作栏高度（两行按钮：勾选操作 + 子页操作）。</summary>
        public const float MANAGER_ACTION_BAR_HEIGHT = 66f;

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

        /// <summary>行内正文预览最多显示的字符数。</summary>
        public const int MANAGER_CONTENT_MAX_CHARS = 78;

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

        /// <summary>提示文本（如搜索框占位符）的颜色。</summary>
        public static readonly Color MANAGER_HINT_COLOR = new Color(0.62f, 0.62f, 0.62f);
    }
}
