namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 全局常量与默认值（核心区）。
    /// 约定：业务代码中不得出现裸字面量（前缀、模板、默认值、日志 key 等），一律从此类取值。
    /// 文案类常量均为**单行**：上游 <c>ImportFromText</c> 按 <c>\n</c> 切行，内容含换行会破坏导入导出格式。
    /// </summary>
    /// <remarks>
    /// 本类按 partial 拆为三个文件，引用名不变，便于按用途查阅：
    /// ① 本文件——前缀与标签、数值默认值与边界、刷新与冷却、日志与来源；
    /// ② <c>FactionKnowledgeConfig.Templates.cs</c>——派系 / 我方派系 / 异种人的内容模板；
    /// ③ <c>FactionKnowledgeConfig.Settings.cs</c>——设置分类名、持久化键、分组标题、控件文案与尺寸。
    /// </remarks>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 本 mod 元信息 ----------

        /// <summary>本 mod 的 Harmony 实例标识，用于区分补丁集、便于排查冲突。</summary>
        public const string HARMONY_ID = "Cancelation.RimtalkAutoFactionInfo";

        /// <summary>
        /// 本次编译是否为 Debug 包：由 csproj 的 <c>DEBUG</c> 编译符号判定（Release 配置不定义它）。
        /// 用途：① 启动日志输出包类型；② 明细日志的默认门控（与开发者模式无关）。
        /// </summary>
#if DEBUG
        public const bool IS_DEBUG_BUILD = true;
#else
        public const bool IS_DEBUG_BUILD = false;
#endif

        // ---------- 前缀与标签 ----------

        /// <summary>日志统一前缀（纯文本），所有日志行以此开头，便于按本 mod 过滤。</summary>
        public const string LOG_PREFIX = "[派系常识]";

        /// <summary>日志前缀的染色值（RGB 255,255,198，淡黄），供日志窗口按富文本解析。</summary>
        public const string LOG_PREFIX_COLOR = "#FFFFC6";

        /// <summary>
        /// 实际输出的日志前缀：给 <see cref="LOG_PREFIX"/> 套一层淡黄染色。
        /// 日志窗口按富文本渲染即显示为淡黄；写入 Player.log 等纯文本文件时会保留标签本身，
        /// 过滤时搜 <see cref="LOG_PREFIX"/> 仍可命中（标签不隔断其中的文本）。
        /// </summary>
        public const string LOG_PREFIX_COLORED =
            "<color=" + LOG_PREFIX_COLOR + ">" + LOG_PREFIX + "</color>";

        /// <summary>派系常识的内容前缀，同时作为「本 mod 注入」的幂等判定标记。</summary>
        public const string FACTION_CONTENT_PREFIX = "【派系】";

        /// <summary>异种人常识的内容前缀，同时作为「本 mod 注入」的幂等判定标记。</summary>
        public const string XENOTYPE_CONTENT_PREFIX = "【异种人】";

        /// <summary>触发标签内多关键词的分隔符（半角逗号）。</summary>
        public const string TAG_SEPARATOR = ",";

        /// <summary>
        /// 上游 <c>GetTags()</c> 认的全部分隔符字符。
        /// 用途：派系名 / 异种人名里若含这些字符，拼出的标签会被切成多段、主键比对随即失效，
        /// 条目将反复堆积；故构建标签前必须把名字里的这些字符净化掉。
        /// 首项须与 <see cref="TAG_SEPARATOR"/> 保持一致。
        /// </summary>
        public static readonly char[] TAG_SEPARATOR_CHARS = { ',', '，', '、', ';', '；' };

        /// <summary>内容内部的列表连接符（与标签分隔符无关，内容不参与标签切分）。</summary>
        public const string LIST_SEPARATOR = "、";

        // ---------- 随包预设库（KnowledgeBase）----------

        /// <summary>随包社区常识库的目录名（位于本 mod 根目录下）。</summary>
        public const string KNOWLEDGE_BASE_FOLDER = "KnowledgeBase";

        /// <summary>导入包目录名前缀：真实目录名带版本戳（如「mod层导入包-1008-0543」），故只能按前缀扫描。</summary>
        public const string KNOWLEDGE_BLOCK_PACK_PREFIX = "mod层导入包-";

        /// <summary>条目文件扩展名。</summary>
        public const string KNOWLEDGE_BLOCK_FILE_EXTENSION = ".txt";

        /// <summary>
        /// 块级通用面文件名（每个块目录一份）。
        /// 内容为该块的中枢行 / 派生行 / 未命中行，门槛与块内模组文件联动（见 <c>KnowledgeBaseImporter</c>）。
        /// </summary>
        public const string KNOWLEDGE_COMMON_FILE_NAME = "_公共.txt";

        /// <summary>游戏本体文件前缀：文件名形如 <c>本体-Core.txt</c> / <c>本体-Royalty.txt</c>，按 DLC 取用。</summary>
        public const string KNOWLEDGE_BUILTIN_FILE_PREFIX = "本体-";

        /// <summary>本体文件 DLC 名：Core（基础游戏，恒为持有）。</summary>
        public const string KNOWLEDGE_DLC_CORE = "Core";

        /// <summary>本体文件 DLC 名：Royalty（皇权）。</summary>
        public const string KNOWLEDGE_DLC_ROYALTY = "Royalty";

        /// <summary>本体文件 DLC 名：Ideology（文化）。</summary>
        public const string KNOWLEDGE_DLC_IDEOLOGY = "Ideology";

        /// <summary>本体文件 DLC 名：Biotech（生物科技）。</summary>
        public const string KNOWLEDGE_DLC_BIOTECH = "Biotech";

        /// <summary>本体文件 DLC 名：Anomaly（异象）。</summary>
        public const string KNOWLEDGE_DLC_ANOMALY = "Anomaly";

        /// <summary>本体文件 DLC 名：Odyssey（奥德赛）。</summary>
        public const string KNOWLEDGE_DLC_ODYSSEY = "Odyssey";

        /// <summary>「块名 / 模组名 / packageId」文件级索引文件名。</summary>
        public const string KNOWLEDGE_SOURCE_MAP_FILE = "模组溯源映射.tsv";

        /// <summary>索引文件的列分隔符（制表符）。</summary>
        public const char KNOWLEDGE_SOURCE_MAP_SEPARATOR = '\t';

        /// <summary>
        /// 判重键（<c>tag</c> + 内容）的内部分隔符。
        /// 取控制字符，避免与标签 / 正文里可能出现的可见字符冲突而误判为同一条。
        /// </summary>
        public const char KNOWLEDGE_KEY_SEPARATOR = '\u0001';

        /// <summary>标签段整体缺失时的兜底标签，与上游 <c>ParseLine</c> 口径一致。</summary>
        public const string KNOWLEDGE_DEFAULT_TAG = "通用";

        /// <summary>标签框内的子字段分隔符（上游格式：标签|重要度|匹配模式|可提取|可匹配）。</summary>
        public const char KNOWLEDGE_TAG_FIELD_SEPARATOR = '|';

        /// <summary>Steam 版 <c>packageId</c> 后缀：比较前须去除，否则会漏块（见证据 ㊼）。</summary>
        public const string PACKAGE_ID_STEAM_POSTFIX = "_steam";

        // ---------- 数值默认值 ----------

        /// <summary>
        /// **我方派系**条目的默认重要度（0~1）。
        /// 取 0.95：社区常识库把 0.96~1.0 整段留空（自我约束留给系统级），社区顶级条目（如种族）用 0.95；
        /// 本 mod 单方占 1.0 既越过该保留带，又会在同派系上把社区条目挤出注入名额，故取 0.95 与之齐平。
        /// </summary>
        public const float DEFAULT_IMPORTANCE_PLAYER = 0.95f;

        /// <summary>
        /// **其它派系与异种人**条目的默认重要度（0~1）。
        /// 取 0.80：与社区常识库的「派系本体」档一致，避免本 mod 条目压过同派系的社区条目。
        /// </summary>
        public const float DEFAULT_IMPORTANCE_OTHER = 0.80f;

        /// <summary>
        /// 常识条目「定向殖民者」的取值：<c>-1</c> 表示不限定，任何殖民者都可匹配。
        /// </summary>
        public const int TARGET_PAWN_ALL = -1;

        /// <summary>异种人常识默认列出的「标志性基因」条数（0 表示不写该段）。</summary>
        public const int DEFAULT_XENOTYPE_GENE_COUNT = 4;

        // ---------- 设置项默认值与取值边界 ----------

        /// <summary>定时校正的默认间隔（游戏小时）。</summary>
        public const int DEFAULT_REFRESH_INTERVAL_HOURS = 1;

        /// <summary>定时校正间隔的最小取值（游戏小时）：低于 1 小时没有意义。</summary>
        public const int REFRESH_INTERVAL_HOURS_MIN = 1;

        /// <summary>定时校正间隔的最大取值（游戏小时）：一天，再长不如关掉本开关。</summary>
        public const int REFRESH_INTERVAL_HOURS_MAX = 24;

        /// <summary>好感度重写阈值的最小取值。</summary>
        public const int GOODWILL_THRESHOLD_MIN = 0;

        /// <summary>好感度重写阈值的最大取值（好感度区间为 -100~100，取 100 即只在关系翻转时重写）。</summary>
        public const int GOODWILL_THRESHOLD_MAX = 100;

        /// <summary>异种人条目标志性基因条数的最小取值（0 = 不写基因段）。</summary>
        public const int XENOTYPE_GENE_COUNT_MIN = 0;

        /// <summary>异种人条目标志性基因条数的最大取值。</summary>
        public const int XENOTYPE_GENE_COUNT_MAX = 10;

        /// <summary>常识条目重要度的最小取值。</summary>
        public const float IMPORTANCE_MIN = 0f;

        /// <summary>常识条目重要度的最大取值（世界观级最高档）。</summary>
        public const float IMPORTANCE_MAX = 1f;

        // ---------- 定时刷新与覆写冷却 ----------

        /// <summary>1 游戏小时对应的 tick 数（1 天 = 24 小时 = 60000 tick）。</summary>
        public const int TICKS_PER_HOUR = 2500;

        /// <summary>1 游戏天对应的 tick 数。</summary>
        public const int TICKS_PER_DAY = 60000;

        /// <summary>
        /// 校正**检测**间隔（tick）：每 1 游戏小时查一次库并与新建内容比对。
        /// 这一步只做「查库 + 拼串」，不写库、不产生日志，开销可忽略；
        /// 真正决定写入频率的是下面的覆写冷却，两者刻意分开。
        /// </summary>
        public const int REFRESH_INTERVAL_TICKS = TICKS_PER_HOUR;

        /// <summary>
        /// 非质变覆写的冷却（tick）：3 天。
        /// 「质变」= <c>PlayerRelationKind</c>（敌对 / 中立 / 盟友）相对上次写入发生翻转——
        /// 质变**不受**冷却限制、立即重写；好感度累积、领袖更替、据点增减、成员变化等
        /// 非质变改动则要等到冷却结束才重写，避免长局里频繁改库。
        /// </summary>
        public const int REWRITE_COOLDOWN_TICKS = 3 * TICKS_PER_DAY;

        /// <summary>
        /// 调试模式（开发者菜单「切换快速刷新」）下的覆写冷却：1 小时。
        /// 用于在游戏内快速观察覆写行为，不必真等 3 天。
        /// </summary>
        public const int DEBUG_REWRITE_COOLDOWN_TICKS = TICKS_PER_HOUR;

        /// <summary>
        /// 好感度重写阈值：与上次写入值相差**未达**此值时，不为纯好感度波动重写条目
        /// （避免 -75 → -74 这类无意义写入）。此判定独立于覆写冷却：小幅波动直接不写。
        /// </summary>
        public const int GOODWILL_REFRESH_THRESHOLD = 10;

        /// <summary>
        /// 普通派系质变指纹在「与玩家无可谈关系」时的取值。
        /// 隐藏 / 临时派系的 <c>Faction.HasGoodwill</c> 为假（= <c>!Hidden &amp;&amp; !temporary</c>），
        /// 引擎里没有它们与玩家的关系条目；硬取 <c>PlayerRelationKind</c> 会落入
        /// <c>RelationWith(other, allowNull: false)</c> 的「缺失关系」分支——先打一条引擎
        /// <c>Log.Error</c>、再返回 dummy 关系（默认中立），写出去就是假数据。故改用本固定键，
        /// 语义为「无关系可质变」：这类派系的条目只在内容变化且冷却到期时重写。
        /// </summary>
        public const string QUALITATIVE_KEY_NO_RELATION = "NoRelation";

        // ---------- 注入开关相关日志 ----------

        /// <summary>汇总结论：注入总开关关闭，本次不写入。</summary>
        public const string LOG_INJECTION_DISABLED = "常识注入已关闭（总开关），本次未写入任何条目。";

        /// <summary>汇总结论：读档校正开关关闭，本次读档不校正。</summary>
        public const string LOG_BACKFILL_ON_LOAD_DISABLED = "读档校正已关闭，本次读档未做任何写入。";

        /// <summary>汇总结论：异种人条目开关关闭，本次跳过异种人一遍。</summary>
        public const string LOG_XENOTYPE_INJECTION_DISABLED = "异种人常识：已在设置中关闭，本次跳过。";

        // ---------- 预设库导入日志与告警 ----------

        /// <summary>
        /// 汇总：预设库导入结果。{0}=实际导入的文件数，{1}=新增条数，{2}=跳过条数（已存在），{3}=跳过文件数（未启用 / 开关关闭）。
        /// </summary>
        public const string LOG_KNOWLEDGE_IMPORT_SUMMARY =
            "预设库导入：导入 {0} 个文件，新增 {1} 条，跳过 {2} 条（已存在），跳过 {3} 个文件（对应 mod 未启用或开关关闭）。";

        /// <summary>汇总：两个导入开关都关闭，本次跳过预设库导入。</summary>
        public const string LOG_KNOWLEDGE_IMPORT_DISABLED = "预设库导入：两个导入开关均为关闭，本次跳过。";

        /// <summary>告警：未找到随包预设库目录（不影响本 mod 自建条目注入）。{0}=预期路径。</summary>
        public const string WARN_KNOWLEDGE_BASE_MISSING = "未找到随包预设库目录，本次跳过预设库导入：{0}";

        /// <summary>告警：预设库下未找到导入包目录。{0}=目录名前缀。</summary>
        public const string WARN_KNOWLEDGE_BLOCK_PACK_MISSING =
            "预设库下未找到导入包目录（目录名前缀「{0}」），本次跳过预设库导入。";

        /// <summary>告警：索引文件列出、但包内缺失的条目文件（可能因打包缺漏而未导入）。{0}=「块名/模组名」（「、」连接）。</summary>
        public const string WARN_KNOWLEDGE_MAP_FILE_MISSING = "索引文件列出但包内缺失的条目文件：{0}";

        /// <summary>告警：包内存在、但索引文件缺行的模组文件（会导致该文件静默不导入，是包更新时的主要风险点）。{0}=「块名/模组名」（「、」连接）。</summary>
        public const string WARN_KNOWLEDGE_PACK_FILE_UNMAPPED = "包内存在但索引文件缺行的模组文件（将静默不导入）：{0}";

        /// <summary>告警：一个 <c>packageId</c> 同时对应多个块（正常现象，仅提示以便排查）。{0}=packageId，{1}=块名（「、」连接）。</summary>
        public const string WARN_KNOWLEDGE_PACKAGE_MULTI_BLOCK = "模组 {0} 对应的常识块有多个：{1}";

        /// <summary>告警：同名派系（tag 相同）撞车（D21）——两条都写入、不消歧。{0}=先遇到的 defName，{1}=后遇到的 defName。</summary>
        public const string WARN_DUPLICATE_FACTION_TAG =
            "检测到同名派系（tag 相同），两条常识都会注入且不做消歧：{0} / {1}";

        /// <summary>原版「古代人」派系（中立）的 defName。</summary>
        public const string FACTION_DEFNAME_ANCIENTS = "Ancients";

        /// <summary>原版「敌对古代人」派系的 defName（与 <see cref="FACTION_DEFNAME_ANCIENTS"/> 派系名相同，原版固定如此）。</summary>
        public const string FACTION_DEFNAME_ANCIENTS_HOSTILE = "AncientsHostile";

        /// <summary>
        /// 原版这对同名古代人撞车时的吐槽（D21 特例）：两个 defName 不同、派系名却相同，
        /// 属原版预期行为、无需处理，单独补一句让玩家知道这条重复不用管。
        /// </summary>
        public const string WARN_ANCIENTS_NAMESAKE_BANTER = "泰南你做古代人给我做好了口牙！";

        /// <summary>告警：单个条目文件读取失败。{0}=文件名，{1}=异常摘要。</summary>
        public const string WARN_KNOWLEDGE_BLOCK_READ_FAILED = "读取常识条目文件失败：{0} —— {1}";

        /// <summary>告警：常识库界面优化补丁未能应用（上游界面结构可能已变）。{0}=异常摘要。</summary>
        public const string WARN_COMMON_KNOWLEDGE_UI_PATCH_FAILED =
            "常识库界面优化补丁未生效（上游界面结构可能已变），本次回退为上游默认行为：{0}";

        // ---------- 启动包信息与注入合计日志 ----------

        /// <summary>启动日志：本次运行加载的是 Debug 包还是 Release 包。{0}=包类型文案。</summary>
        public const string LOG_MOD_BUILD = "炒饭智能正在为边缘世界写入常识，当前版本：{0}。";

        /// <summary>包类型文案：Debug 包（本地开发 / 调试用）。</summary>
        public const string MOD_BUILD_DEBUG = "Debug（调试版）";

        /// <summary>包类型文案：Release 包（发行版）。</summary>
        public const string MOD_BUILD_RELEASE = "Release（发行版）";

        /// <summary>初始化完成日志：本次注入的合计条数。{0}=派系条数，{1}=异种人条数。</summary>
        public const string LOG_INJECTION_TOTAL = "常识注入完成：派系 {0} 条，异种人 {1} 条。";

        /// <summary>告警：读取主基地财富失败（触发全图重算时越界），本次省略财富段。{0}=异常摘要。</summary>
        public const string WARN_PLAYER_WEALTH_UNAVAILABLE = "读取主基地财富失败，本次省略我方派系的财富段：{0}";

        // ---------- 上游常识库界面补丁（D48）----------
        // 上游 Dialog_CommonKnowledge 的中心列表把 VirtualListView 建好却未调用其 Draw，
        // 改成手写 foreach 全量绘制，条目一多就每帧拖垮界面。本 mod 只补一层「跳过不可见行」，
        // 不改上游逻辑、不动其控件顺序。方法名与字段名是上游私有实现，改名即补丁自动跳过（见 WARN 常量）。

        /// <summary>上游「中心列表」方法名（补丁在此记录滚动视口）。</summary>
        public const string UPSTREAM_UI_METHOD_DRAW_CENTER_LIST = "DrawCenterList";

        /// <summary>上游「单行绘制」方法名（补丁在此按可见性裁剪）。</summary>
        public const string UPSTREAM_UI_METHOD_DRAW_ENTRY_ROW = "DrawEntryRow";

        /// <summary>上游中心列表的滚动位置私有字段名。</summary>
        public const string UPSTREAM_UI_FIELD_LIST_SCROLL_POSITION = "listScrollPosition";

        /// <summary>告警占位：上游目标方法 / 字段未找到（拼进 WARN_COMMON_KNOWLEDGE_UI_PATCH_FAILED 的 {0}）。</summary>
        public const string UPSTREAM_UI_PATCH_TARGET_MISSING = "上游方法或字段未找到";

        /// <summary>
        /// 上游中心列表外框与滚动视口之间的边距合计：上游用 <c>GenUI.ContractedBy(rect, 5f)</c>，上下各 5。
        /// 视口高度 = 外框高度 − 本值。
        /// </summary>
        public const float UPSTREAM_UI_VIEWPORT_PADDING = 10f;

        /// <summary>
        /// 可见性裁剪的额外余量（内容坐标）：视口外再多绘一行的厚度，避免行在视口边缘反复进出时闪烁。
        /// 取值等于上游行高（70）。
        /// </summary>
        public const float UPSTREAM_UI_CULL_MARGIN = 70f;

        // ---------- 上游常识库界面入口按钮（FR-17）----------
        // 在 Dialog_CommonKnowledge 的工具栏里追加一个「常识管理」按钮，点击打开本 mod 的管理窗口。
        // 横向位置取上游搜索框（宽 300、左内边距 5）右侧的空白处，避开其右对齐的按钮组。

        /// <summary>上游「工具栏」方法名（补丁在其绘制完成后追加入口按钮）。</summary>
        public const string UPSTREAM_UI_METHOD_DRAW_TOOLBAR = "DrawToolbar";

        /// <summary>入口按钮文案。</summary>
        public const string UPSTREAM_UI_MANAGER_BUTTON_LABEL = "自动注入常识管理";

        /// <summary>入口按钮的悬停说明。</summary>
        public const string UPSTREAM_UI_MANAGER_BUTTON_TIP =
            "打开自动注入常识mod的管理页面，可查看常识条目来源";

        /// <summary>入口按钮在工具栏内的横向偏移（左内边距 5 + 上游搜索框 300 + 间距 10）。</summary>
        public const float UPSTREAM_UI_MANAGER_BUTTON_OFFSET_X = 315f;

        /// <summary>入口按钮在工具栏内的纵向偏移（上游工具栏内边距 5 + 控件内边距 5）。</summary>
        public const float UPSTREAM_UI_MANAGER_BUTTON_OFFSET_Y = 10f;

        /// <summary>入口按钮宽度。</summary>
        public const float UPSTREAM_UI_MANAGER_BUTTON_WIDTH = 200f;

        /// <summary>入口按钮高度（与上游工具栏按钮一致）。</summary>
        public const float UPSTREAM_UI_MANAGER_BUTTON_HEIGHT = 32f;

        /// <summary>告警：上游常识库界面入口按钮补丁未生效。{0}=异常摘要或目标缺失说明。</summary>
        public const string WARN_COMMON_KNOWLEDGE_MANAGER_BUTTON_PATCH_FAILED =
            "常识库界面「常识管理」入口按钮补丁未生效，界面不显示该按钮：{0}";

        // ---------- 日志 key（供 KnowledgeLog.WarnOnce 去重）----------

        /// <summary>日志 key：上游常识库不可用。</summary>
        public const int LOG_KEY_LIBRARY_UNAVAILABLE = 1;

        /// <summary>日志 key：读取主基地财富失败。</summary>
        public const int LOG_KEY_PLAYER_WEALTH_UNAVAILABLE = 2;

        /// <summary>日志 key：派系界面「实际成员」构建失败。</summary>
        public const int LOG_KEY_UI_COMPOSITION_FAILED = 3;

        /// <summary>日志 key：未找到随包预设库目录。</summary>
        public const int LOG_KEY_KNOWLEDGE_BASE_MISSING = 4;

        /// <summary>日志 key：未找到导入包目录。</summary>
        public const int LOG_KEY_KNOWLEDGE_BLOCK_PACK_MISSING = 5;

        /// <summary>日志 key：索引文件列出但包内缺失的条目文件。</summary>
        public const int LOG_KEY_KNOWLEDGE_MAP_FILE_MISSING = 6;

        /// <summary>日志 key：一个 <c>packageId</c> 对应多个块。</summary>
        public const int LOG_KEY_KNOWLEDGE_PACKAGE_MULTI_BLOCK = 7;

        /// <summary>日志 key：同名派系 tag 撞车（D21）。</summary>
        public const int LOG_KEY_DUPLICATE_FACTION_TAG = 8;

        /// <summary>日志 key：包内存在但索引文件缺行的模组文件。</summary>
        public const int LOG_KEY_KNOWLEDGE_PACK_FILE_UNMAPPED = 9;

        /// <summary>日志 key：常识库界面优化补丁未生效。</summary>
        public const int LOG_KEY_COMMON_KNOWLEDGE_UI_PATCH_FAILED = 10;

        /// <summary>日志 key：常识库界面入口按钮补丁未生效。</summary>
        public const int LOG_KEY_COMMON_KNOWLEDGE_MANAGER_BUTTON_PATCH_FAILED = 11;

        /// <summary>日志 key：原版 Ancients / AncientsHostile 同名派系撞车（D21 特例吐槽）。</summary>
        public const int LOG_KEY_ANCIENTS_NAMESAKE_BANTER = 12;
    }
}
