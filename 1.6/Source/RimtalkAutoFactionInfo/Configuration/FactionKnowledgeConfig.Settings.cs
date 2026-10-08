namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// Mod 设置页常量（<see cref="FactionKnowledgeConfig"/> 的 partial 分片）：
    /// 设置分类名、持久化键、分组标题、控件标题与悬停说明、尺寸与单位。
    /// 持久化键一律与 <see cref="FactionInfoSettings"/> 的字段同名（RimWorld 惯例），改键名会丢失存量设置。
    /// </summary>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 设置分类与界面增强开关 ----------

        /// <summary>
        /// Mod 设置页的分类名（引擎按 <c>Mod.SettingsCategory()</c> 非空把本 mod 列入设置列表，
        /// 同时作为列表项与设置窗标题）。留空会使本 mod 从设置列表消失，故给固定名。
        /// </summary>
        public const string MOD_SETTINGS_CATEGORY = "Rimtalk Auto Faction Info";

        /// <summary>设置项标题：在派系界面追加「实际成员」构成。</summary>
        public const string MOD_SETTINGS_SHOW_UI_COMPOSITION = "在派系界面显示实际成员构成";

        /// <summary>设置项说明（悬停提示）。</summary>
        public const string MOD_SETTINGS_SHOW_UI_COMPOSITION_TIP =
            "在派系列表的悬停提示与派系信息卡中追加一行「实际成员」。" +
            "用于修正游戏自带的成员统计在 HAR 种族 / 兵种级异种人派系上误报「智人种 100%」的情况。";

        /// <summary>设置持久化键：派系界面实际成员开关。改键名会丢失存量设置。</summary>
        public const string SETTINGS_KEY_SHOW_UI_COMPOSITION = "showCompositionInFactionUi";

        // ---------- 设置项：持久化键 ----------
        // 键名一律与字段同名（RimWorld 惯例），改键名会丢失存量设置。

        /// <summary>设置持久化键：常识注入总开关。</summary>
        public const string SETTINGS_KEY_ENABLE_INJECTION = "enableInjection";

        /// <summary>设置持久化键：读档校正开关。</summary>
        public const string SETTINGS_KEY_ENABLE_BACKFILL_ON_LOAD = "enableBackfillOnLoad";

        /// <summary>设置持久化键：运行中定时校正开关。</summary>
        public const string SETTINGS_KEY_ENABLE_PERIODIC_REFRESH = "enablePeriodicRefresh";

        /// <summary>设置持久化键：定时校正间隔（游戏小时）。</summary>
        public const string SETTINGS_KEY_REFRESH_INTERVAL_HOURS = "refreshIntervalHours";

        /// <summary>设置持久化键：好感度重写阈值。</summary>
        public const string SETTINGS_KEY_GOODWILL_REFRESH_THRESHOLD = "goodwillRefreshThreshold";

        /// <summary>设置持久化键：预设库（mod 块）导入开关。</summary>
        public const string SETTINGS_KEY_ENABLE_KNOWLEDGE_BASE_IMPORT = "enableKnowledgeBaseImport";

        /// <summary>设置持久化键：本体块导入开关。</summary>
        public const string SETTINGS_KEY_ENABLE_BUILTIN_KNOWLEDGE_IMPORT = "enableBuiltinKnowledgeImport";

        /// <summary>设置持久化键：写入「基础身份」段。</summary>
        public const string SETTINGS_KEY_INCLUDE_IDENTITY = "includeIdentity";

        /// <summary>设置持久化键：写入「意识形态」段。</summary>
        public const string SETTINGS_KEY_INCLUDE_IDEOLOGY = "includeIdeology";

        /// <summary>设置持久化键：写入「与我方关系」段。</summary>
        public const string SETTINGS_KEY_INCLUDE_RELATION = "includeRelation";

        /// <summary>设置持久化键：写入「领袖与据点」段。</summary>
        public const string SETTINGS_KEY_INCLUDE_SETTLEMENTS = "includeSettlements";

        /// <summary>设置持久化键：我方派系条目重要度。</summary>
        public const string SETTINGS_KEY_KNOWLEDGE_IMPORTANCE_PLAYER = "knowledgeImportancePlayer";

        /// <summary>设置持久化键：其它派系与异种人条目重要度。</summary>
        public const string SETTINGS_KEY_KNOWLEDGE_IMPORTANCE_OTHER = "knowledgeImportanceOther";

        /// <summary>设置持久化键：分类固定为「世界观」。</summary>
        public const string SETTINGS_KEY_CATEGORY_ALWAYS_LORE = "categoryAlwaysLore";

        /// <summary>设置持久化键：写入独立异种人常识条目。</summary>
        public const string SETTINGS_KEY_INCLUDE_XENOTYPES = "includeXenotypes";

        /// <summary>设置持久化键：写入派系与异种人的双向引用。</summary>
        public const string SETTINGS_KEY_INCLUDE_FACTION_XENOTYPE_REFS = "includeFactionXenotypeRefs";

        /// <summary>设置持久化键：异种人条目列出的标志性基因条数。</summary>
        public const string SETTINGS_KEY_XENOTYPE_GENE_COUNT = "xenotypeGeneCount";

        /// <summary>设置持久化键：明细日志开关。</summary>
        public const string SETTINGS_KEY_ENABLE_VERBOSE_LOG = "enableVerboseLog";

        // ---------- 设置项：设置页分组标题 ----------

        /// <summary>设置页分组：注入时机。</summary>
        public const string MOD_SETTINGS_SECTION_INJECTION = "注入时机";

        /// <summary>设置页分组：派系常识内容段。</summary>
        public const string MOD_SETTINGS_SECTION_FACTION_SECTIONS = "派系常识内容段";

        /// <summary>设置页分组：重要度与分类。</summary>
        public const string MOD_SETTINGS_SECTION_IMPORTANCE = "重要度与分类";

        /// <summary>设置页分组：异种人常识（需 Biotech）。</summary>
        public const string MOD_SETTINGS_SECTION_XENOTYPE = "异种人常识（需 Biotech）";

        /// <summary>设置页分组：日志。</summary>
        public const string MOD_SETTINGS_SECTION_LOG = "日志";

        /// <summary>设置页分组：界面增强。</summary>
        public const string MOD_SETTINGS_SECTION_UI = "界面增强";

        // ---------- 设置项：标题与悬停说明 ----------

        /// <summary>设置项标题：注入总开关。</summary>
        public const string MOD_SETTINGS_ENABLE_INJECTION = "启用常识注入（总开关）";

        /// <summary>设置项说明：注入总开关。</summary>
        public const string MOD_SETTINGS_ENABLE_INJECTION_TIP =
            "关闭后新开档、读档与定时校正都不再写入常识；已写入的条目不受影响。";

        /// <summary>设置项标题：读档校正。</summary>
        public const string MOD_SETTINGS_ENABLE_BACKFILL_ON_LOAD = "读档时校正（补齐与刷新）";

        /// <summary>设置项说明：读档校正。</summary>
        public const string MOD_SETTINGS_ENABLE_BACKFILL_ON_LOAD_TIP =
            "读档时逐条比对：缺失则补、陈旧则重写、一致则不动。关闭后仅在新开档与定时校正时处理。";

        /// <summary>设置项标题：运行中定时校正。</summary>
        public const string MOD_SETTINGS_ENABLE_PERIODIC_REFRESH = "运行中定时校正";

        /// <summary>设置项说明：运行中定时校正。</summary>
        public const string MOD_SETTINGS_ENABLE_PERIODIC_REFRESH_TIP =
            "按下方间隔轮询，让关系、好感度、领袖、据点等变化及时跟上。关闭后仅在新开档 / 读档时校正。";

        /// <summary>设置项标题：定时校正间隔。</summary>
        public const string MOD_SETTINGS_REFRESH_INTERVAL_HOURS = "定时校正间隔（游戏小时）";

        /// <summary>设置项说明：定时校正间隔。</summary>
        public const string MOD_SETTINGS_REFRESH_INTERVAL_HOURS_TIP =
            "默认 1 小时。轮询只查库并拼串比对、不写库，开销很低；越短越及时。";

        /// <summary>设置项标题：好感度重写阈值。</summary>
        public const string MOD_SETTINGS_GOODWILL_REFRESH_THRESHOLD = "好感度重写阈值";

        /// <summary>设置项说明：好感度重写阈值。</summary>
        public const string MOD_SETTINGS_GOODWILL_REFRESH_THRESHOLD_TIP =
            "好感度变化未达此值、且其它内容都没变时，不为纯数值波动重写条目。默认 10。";

        /// <summary>设置项标题：预设库（mod 块）导入开关。</summary>
        public const string MOD_SETTINGS_ENABLE_KNOWLEDGE_BASE_IMPORT = "导入随包预设库（已启用 mod）";

        /// <summary>设置项说明：预设库（mod 块）导入开关。</summary>
        public const string MOD_SETTINGS_ENABLE_KNOWLEDGE_BASE_IMPORT_TIP =
            "新开档 / 读档时，把随包社区常识库中与「当前已启用 mod」对应的条目文件（连同该块的公共面）导入常识库：" +
            "按「标签 + 内容」判重、缺失即补、沿用社区原始重要度。只订阅了本体、未订阅扩展的玩家不会被塞入扩展内容。" +
            "玩家手动删除的条目会在下次新档 / 读档被补回。关闭后本路径不写入。";

        /// <summary>设置项标题：本体块导入开关。</summary>
        public const string MOD_SETTINGS_ENABLE_BUILTIN_KNOWLEDGE_IMPORT = "同时导入游戏本体与通用常识";

        /// <summary>设置项说明：本体块导入开关。</summary>
        public const string MOD_SETTINGS_ENABLE_BUILTIN_KNOWLEDGE_IMPORT_TIP =
            "额外导入预设库中的游戏本体条目（只取你已持有 DLC 对应的部分）与不依赖任何 mod 的公共块（如「自定义常识」）。" +
            "体量较大。默认关闭。";

        /// <summary>设置项标题：写入「基础身份」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_IDENTITY = "写入「基础身份」段";

        /// <summary>设置项说明：写入「基础身份」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_IDENTITY_TIP =
            "派系名、类型标签与科技水平。关闭后该段缺席（我方派系条目不受影响）。";

        /// <summary>设置项标题：写入「意识形态」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_IDEOLOGY = "写入「意识形态」段";

        /// <summary>设置项说明：写入「意识形态」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_IDEOLOGY_TIP =
            "主理念与其信条。无 Ideology DLC 时该段本就自动失效。";

        /// <summary>设置项标题：写入「与我方关系」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_RELATION = "写入「与我方关系」段";

        /// <summary>设置项说明：写入「与我方关系」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_RELATION_TIP =
            "关系种类与好感度。关闭后该段缺席。";

        /// <summary>设置项标题：写入「领袖与据点」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_SETTLEMENTS = "写入「领袖与据点」段";

        /// <summary>设置项说明：写入「领袖与据点」段。</summary>
        public const string MOD_SETTINGS_INCLUDE_SETTLEMENTS_TIP =
            "领袖姓名、定居点数量与最近距离。关闭后该段缺席。";

        /// <summary>设置项标题：我方派系条目重要度。</summary>
        public const string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_PLAYER = "我方派系条目重要度";

        /// <summary>设置项说明：我方派系条目重要度。</summary>
        public const string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_PLAYER_TIP =
            "0~1，默认 0.95。对齐社区常识库顶级档（社区把 0.96~1.0 留给系统级）。" +
            "调低会让我方派系条目在触发排序中被其它常识挤到后面。";

        /// <summary>设置项标题：其它派系与异种人条目重要度。</summary>
        public const string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_OTHER = "其它派系 / 异种人条目重要度";

        /// <summary>设置项说明：其它派系与异种人条目重要度。</summary>
        public const string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_OTHER_TIP =
            "0~1，默认 0.80。对齐社区常识库的「派系本体」档，避免压过同派系的社区条目。" +
            "调低会让条目在触发排序中被其它常识挤到后面。";

        /// <summary>设置项标题：分类固定为「世界观」。</summary>
        public const string MOD_SETTINGS_CATEGORY_ALWAYS_LORE = "条目分类固定为「世界观」";

        /// <summary>设置项说明：分类固定为「世界观」。</summary>
        public const string MOD_SETTINGS_CATEGORY_ALWAYS_LORE_TIP =
            "关闭则改由上游按标签自动猜分类，实测会落到「其它」，仅影响常识库 UI 分组。";

        /// <summary>设置项标题：写入独立异种人常识条目。</summary>
        public const string MOD_SETTINGS_INCLUDE_XENOTYPES = "写入独立的异种人常识条目";

        /// <summary>设置项说明：写入独立异种人常识条目。</summary>
        public const string MOD_SETTINGS_INCLUDE_XENOTYPES_TIP =
            "需 Biotech DLC，无 DLC 时本就整体跳过。关闭后派系条目的成员构成段不受影响。";

        /// <summary>设置项标题：写入双向引用。</summary>
        public const string MOD_SETTINGS_INCLUDE_FACTION_XENOTYPE_REFS = "写入派系与异种人的双向引用";

        /// <summary>设置项说明：写入双向引用。</summary>
        public const string MOD_SETTINGS_INCLUDE_FACTION_XENOTYPE_REFS_TIP =
            "派系条目的「成员构成」段 + 异种人条目的「出没派系」段；同时决定派系标签是否并列特征异种人名，" +
            "以免出现「提到某异种人却带出不含该信息的条目」。";

        /// <summary>设置项标题：异种人标志性基因条数。</summary>
        public const string MOD_SETTINGS_XENOTYPE_GENE_COUNT = "异种人条目标志性基因条数";

        /// <summary>设置项说明：异种人标志性基因条数。</summary>
        public const string MOD_SETTINGS_XENOTYPE_GENE_COUNT_TIP =
            "0 表示不写基因段。默认 4。";

        /// <summary>设置项标题：明细日志开关。</summary>
        public const string MOD_SETTINGS_ENABLE_VERBOSE_LOG = "输出逐条明细日志";

        /// <summary>设置项说明：明细日志开关。</summary>
        public const string MOD_SETTINGS_ENABLE_VERBOSE_LOG_TIP =
            "默认只输出汇总与计数。开启后输出每个派系 / 异种人的完整内容。Debug 包默认开启，与本项无关。";

        /// <summary>设置项标题：立即重新注入按钮。</summary>
        public const string MOD_SETTINGS_REINJECT = "立即对当前存档重新注入";

        /// <summary>设置项说明：立即重新注入按钮（可用时）。</summary>
        public const string MOD_SETTINGS_REINJECT_TIP =
            "对当前存档重跑一次注入与校正，便于改完设置立刻看效果，无需重开档。";

        /// <summary>设置项说明：立即重新注入按钮（无存档时不可用的原因）。</summary>
        public const string MOD_SETTINGS_REINJECT_NO_GAME_TIP = "当前没有正在进行的存档。";

        // ---------- 设置页尺寸与单位 ----------

        /// <summary>设置页滑块行高。</summary>
        public const float MOD_SETTINGS_SLIDER_ROW_HEIGHT = 30f;

        /// <summary>设置页按钮行高。</summary>
        public const float MOD_SETTINGS_BUTTON_HEIGHT = 30f;

        /// <summary>设置页分组标题上方的间隔。</summary>
        public const float MOD_SETTINGS_SECTION_GAP = 12f;

        /// <summary>
        /// 设置页滚动内容的总高度：本 mod 设置项较多，一屏放不下，故整体放入滚动视图。
        /// 数值按「控件行数 × 行高 + 分组标题」估算并留有余量；新增设置项时须同步调大。
        /// </summary>
        public const float MOD_SETTINGS_CONTENT_HEIGHT = 1100f;

        /// <summary>设置页滚动视图预留的滚动条宽度。</summary>
        public const float MOD_SETTINGS_SCROLLBAR_WIDTH = 20f;

        /// <summary>设置页滑块行的标签宽度占比（中文标签较长，需多于默认的一半）。</summary>
        public const float MOD_SETTINGS_SLIDER_LABEL_PCT = 0.6f;

        /// <summary>设置页滑块标题格式：{0}=标题，{1}=当前值，{2}=单位后缀。</summary>
        public const string MOD_SETTINGS_SLIDER_LABEL_FORMAT = "{0}：{1}{2}";

        /// <summary>滑块单位后缀：游戏小时。</summary>
        public const string MOD_SETTINGS_UNIT_HOURS = " 小时";

        /// <summary>滑块单位后缀：条（基因条数）。</summary>
        public const string MOD_SETTINGS_UNIT_ENTRIES = " 条";

        /// <summary>滑块单位后缀：无（好感度阈值、重要度）。</summary>
        public const string MOD_SETTINGS_UNIT_NONE = "";
    }
}
