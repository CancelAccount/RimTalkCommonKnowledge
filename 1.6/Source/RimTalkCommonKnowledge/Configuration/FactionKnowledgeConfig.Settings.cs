using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// Mod 设置页常量（<see cref="FactionKnowledgeConfig"/> 的 partial 分片）：
    /// 设置分类名、持久化键、分组标题、控件标题与悬停说明、尺寸与单位。
    /// 持久化键一律与 <see cref="FactionInfoSettings"/> 的字段同名（RimWorld 惯例），改键名会丢失存量设置。
    /// 界面文案为随游戏语言翻译的只读属性（Keyed 键 RTCK.Set.*）；
    /// 分类名（品牌名）、持久化键、尺寸与空单位后缀保持常量不译。
    /// </summary>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 设置分类与界面增强开关 ----------

        /// <summary>
        /// Mod 设置页的分类名（引擎按 <c>Mod.SettingsCategory()</c> 非空把本 mod 列入设置列表，
        /// 同时作为列表项与设置窗标题）。留空会使本 mod 从设置列表消失，故给固定名。
        /// 取值与 About.xml 的显示名一致：RimTalk 常识+。品牌名不随语言翻译。
        /// </summary>
        public const string MOD_SETTINGS_CATEGORY = "RimTalk 常识+";

        /// <summary>设置项标题：在派系界面追加「实际成员」构成。</summary>
        public static string MOD_SETTINGS_SHOW_UI_COMPOSITION => "RTCK.Set.ShowUiComposition".Translate();

        /// <summary>设置项说明（悬停提示）。</summary>
        public static string MOD_SETTINGS_SHOW_UI_COMPOSITION_TIP => "RTCK.Set.ShowUiCompositionTip".Translate();

        /// <summary>设置项标题：常识库界面滚动优化。</summary>
        public static string MOD_SETTINGS_ENABLE_KNOWLEDGE_UI_OPTIMIZATION => "RTCK.Set.EnableKnowledgeUiOptimization".Translate();

        /// <summary>设置项说明（悬停提示）。</summary>
        public static string MOD_SETTINGS_ENABLE_KNOWLEDGE_UI_OPTIMIZATION_TIP => "RTCK.Set.EnableKnowledgeUiOptimizationTip".Translate();

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

        /// <summary>设置持久化键：常识库界面滚动优化开关。</summary>
        public const string SETTINGS_KEY_ENABLE_KNOWLEDGE_UI_OPTIMIZATION = "enableKnowledgeUiOptimization";

        // ---------- 设置项：设置页分组标题 ----------

        /// <summary>设置页分组：注入时机。</summary>
        public static string MOD_SETTINGS_SECTION_INJECTION => "RTCK.Set.SectionInjection".Translate();

        /// <summary>设置页分组：派系常识内容段。</summary>
        public static string MOD_SETTINGS_SECTION_FACTION_SECTIONS => "RTCK.Set.SectionFactionSections".Translate();

        /// <summary>设置页分组：重要度与分类。</summary>
        public static string MOD_SETTINGS_SECTION_IMPORTANCE => "RTCK.Set.SectionImportance".Translate();

        /// <summary>设置页分组：异种人常识（需 Biotech）。</summary>
        public static string MOD_SETTINGS_SECTION_XENOTYPE => "RTCK.Set.SectionXenotype".Translate();

        /// <summary>设置页分组：日志。</summary>
        public static string MOD_SETTINGS_SECTION_LOG => "RTCK.Set.SectionLog".Translate();

        /// <summary>设置页分组：界面增强。</summary>
        public static string MOD_SETTINGS_SECTION_UI => "RTCK.Set.SectionUi".Translate();

        // ---------- 设置项：标题与悬停说明 ----------

        /// <summary>设置项标题：注入总开关。</summary>
        public static string MOD_SETTINGS_ENABLE_INJECTION => "RTCK.Set.EnableInjection".Translate();

        /// <summary>设置项说明：注入总开关。</summary>
        public static string MOD_SETTINGS_ENABLE_INJECTION_TIP => "RTCK.Set.EnableInjectionTip".Translate();

        /// <summary>设置项标题：读档校正。</summary>
        public static string MOD_SETTINGS_ENABLE_BACKFILL_ON_LOAD => "RTCK.Set.EnableBackfillOnLoad".Translate();

        /// <summary>设置项说明：读档校正。</summary>
        public static string MOD_SETTINGS_ENABLE_BACKFILL_ON_LOAD_TIP => "RTCK.Set.EnableBackfillOnLoadTip".Translate();

        /// <summary>设置项标题：运行中定时校正。</summary>
        public static string MOD_SETTINGS_ENABLE_PERIODIC_REFRESH => "RTCK.Set.EnablePeriodicRefresh".Translate();

        /// <summary>设置项说明：运行中定时校正。</summary>
        public static string MOD_SETTINGS_ENABLE_PERIODIC_REFRESH_TIP => "RTCK.Set.EnablePeriodicRefreshTip".Translate();

        /// <summary>设置项标题：定时校正间隔。</summary>
        public static string MOD_SETTINGS_REFRESH_INTERVAL_HOURS => "RTCK.Set.RefreshIntervalHours".Translate();

        /// <summary>设置项说明：定时校正间隔。</summary>
        public static string MOD_SETTINGS_REFRESH_INTERVAL_HOURS_TIP => "RTCK.Set.RefreshIntervalHoursTip".Translate();

        /// <summary>设置项标题：好感度重写阈值。</summary>
        public static string MOD_SETTINGS_GOODWILL_REFRESH_THRESHOLD => "RTCK.Set.GoodwillRefreshThreshold".Translate();

        /// <summary>设置项说明：好感度重写阈值。</summary>
        public static string MOD_SETTINGS_GOODWILL_REFRESH_THRESHOLD_TIP => "RTCK.Set.GoodwillRefreshThresholdTip".Translate();

        /// <summary>设置项标题：预设库（mod 块）导入开关。</summary>
        public static string MOD_SETTINGS_ENABLE_KNOWLEDGE_BASE_IMPORT => "RTCK.Set.EnableKnowledgeBaseImport".Translate();

        /// <summary>设置项说明：预设库（mod 块）导入开关。</summary>
        public static string MOD_SETTINGS_ENABLE_KNOWLEDGE_BASE_IMPORT_TIP => "RTCK.Set.EnableKnowledgeBaseImportTip".Translate();

        /// <summary>设置项标题：本体块导入开关。</summary>
        public static string MOD_SETTINGS_ENABLE_BUILTIN_KNOWLEDGE_IMPORT => "RTCK.Set.EnableBuiltinKnowledgeImport".Translate();

        /// <summary>设置项说明：本体块导入开关。</summary>
        public static string MOD_SETTINGS_ENABLE_BUILTIN_KNOWLEDGE_IMPORT_TIP => "RTCK.Set.EnableBuiltinKnowledgeImportTip".Translate();

        /// <summary>设置项标题：写入「基础身份」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_IDENTITY => "RTCK.Set.IncludeIdentity".Translate();

        /// <summary>设置项说明：写入「基础身份」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_IDENTITY_TIP => "RTCK.Set.IncludeIdentityTip".Translate();

        /// <summary>设置项标题：写入「意识形态」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_IDEOLOGY => "RTCK.Set.IncludeIdeology".Translate();

        /// <summary>设置项说明：写入「意识形态」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_IDEOLOGY_TIP => "RTCK.Set.IncludeIdeologyTip".Translate();

        /// <summary>设置项标题：写入「与我方关系」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_RELATION => "RTCK.Set.IncludeRelation".Translate();

        /// <summary>设置项说明：写入「与我方关系」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_RELATION_TIP => "RTCK.Set.IncludeRelationTip".Translate();

        /// <summary>设置项标题：写入「领袖与据点」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_SETTLEMENTS => "RTCK.Set.IncludeSettlements".Translate();

        /// <summary>设置项说明：写入「领袖与据点」段。</summary>
        public static string MOD_SETTINGS_INCLUDE_SETTLEMENTS_TIP => "RTCK.Set.IncludeSettlementsTip".Translate();

        /// <summary>设置项标题：我方派系条目重要度。</summary>
        public static string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_PLAYER => "RTCK.Set.KnowledgeImportancePlayer".Translate();

        /// <summary>设置项说明：我方派系条目重要度。</summary>
        public static string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_PLAYER_TIP => "RTCK.Set.KnowledgeImportancePlayerTip".Translate();

        /// <summary>设置项标题：其它派系与异种人条目重要度。</summary>
        public static string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_OTHER => "RTCK.Set.KnowledgeImportanceOther".Translate();

        /// <summary>设置项说明：其它派系与异种人条目重要度。</summary>
        public static string MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_OTHER_TIP => "RTCK.Set.KnowledgeImportanceOtherTip".Translate();

        /// <summary>设置项标题：分类固定为「世界观」。</summary>
        public static string MOD_SETTINGS_CATEGORY_ALWAYS_LORE => "RTCK.Set.CategoryAlwaysLore".Translate();

        /// <summary>设置项说明：分类固定为「世界观」。</summary>
        public static string MOD_SETTINGS_CATEGORY_ALWAYS_LORE_TIP => "RTCK.Set.CategoryAlwaysLoreTip".Translate();

        /// <summary>设置项标题：写入独立异种人常识条目。</summary>
        public static string MOD_SETTINGS_INCLUDE_XENOTYPES => "RTCK.Set.IncludeXenotypes".Translate();

        /// <summary>设置项说明：写入独立异种人常识条目。</summary>
        public static string MOD_SETTINGS_INCLUDE_XENOTYPES_TIP => "RTCK.Set.IncludeXenotypesTip".Translate();

        /// <summary>设置项标题：写入双向引用。</summary>
        public static string MOD_SETTINGS_INCLUDE_FACTION_XENOTYPE_REFS => "RTCK.Set.IncludeFactionXenotypeRefs".Translate();

        /// <summary>设置项说明：写入双向引用。</summary>
        public static string MOD_SETTINGS_INCLUDE_FACTION_XENOTYPE_REFS_TIP => "RTCK.Set.IncludeFactionXenotypeRefsTip".Translate();

        /// <summary>设置项标题：异种人标志性基因条数。</summary>
        public static string MOD_SETTINGS_XENOTYPE_GENE_COUNT => "RTCK.Set.XenotypeGeneCount".Translate();

        /// <summary>设置项说明：异种人标志性基因条数。</summary>
        public static string MOD_SETTINGS_XENOTYPE_GENE_COUNT_TIP => "RTCK.Set.XenotypeGeneCountTip".Translate();

        /// <summary>设置项标题：明细日志开关。</summary>
        public static string MOD_SETTINGS_ENABLE_VERBOSE_LOG => "RTCK.Set.EnableVerboseLog".Translate();

        /// <summary>设置项说明：明细日志开关。</summary>
        public static string MOD_SETTINGS_ENABLE_VERBOSE_LOG_TIP => "RTCK.Set.EnableVerboseLogTip".Translate();

        /// <summary>设置项标题：立即重新注入按钮。</summary>
        public static string MOD_SETTINGS_REINJECT => "RTCK.Set.Reinject".Translate();

        /// <summary>设置项说明：立即重新注入按钮（可用时）。</summary>
        public static string MOD_SETTINGS_REINJECT_TIP => "RTCK.Set.ReinjectTip".Translate();

        /// <summary>设置项说明：立即重新注入按钮（无存档时不可用的原因）。</summary>
        public static string MOD_SETTINGS_REINJECT_NO_GAME_TIP => "RTCK.Set.ReinjectNoGameTip".Translate();

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
        public const float MOD_SETTINGS_CONTENT_HEIGHT = 1200f;

        /// <summary>设置页滚动视图预留的滚动条宽度。</summary>
        public const float MOD_SETTINGS_SCROLLBAR_WIDTH = 20f;

        /// <summary>设置页滑块行的标签宽度占比（中文标签较长，需多于默认的一半）。</summary>
        public const float MOD_SETTINGS_SLIDER_LABEL_PCT = 0.6f;

        /// <summary>设置页滑块标题格式：{0}=标题，{1}=当前值，{2}=单位后缀。</summary>
        public static string MOD_SETTINGS_SLIDER_LABEL_FORMAT => "RTCK.Set.SliderLabelFormat".Translate();

        /// <summary>滑块单位后缀：游戏小时。</summary>
        public static string MOD_SETTINGS_UNIT_HOURS => "RTCK.Set.UnitHours".Translate();

        /// <summary>滑块单位后缀：条（基因条数）。</summary>
        public static string MOD_SETTINGS_UNIT_ENTRIES => "RTCK.Set.UnitEntries".Translate();

        /// <summary>滑块单位后缀：无（好感度阈值、重要度）。空串各语言一致，无需翻译。</summary>
        public const string MOD_SETTINGS_UNIT_NONE = "";
    }
}
