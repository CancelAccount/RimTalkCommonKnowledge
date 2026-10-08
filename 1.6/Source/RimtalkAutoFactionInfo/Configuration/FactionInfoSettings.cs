using System.Collections.Generic;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 本 mod 的可持久化设置（游戏「选项 → Mod 设置」页读写）。
    /// 字段一律 <c>public</c> 并由 <see cref="ExposeData"/> 落盘；改字段/键名会丢失存量设置。
    /// 实例由 <see cref="FactionInfoMod"/> 在构造时载入并存入其静态字段，供业务代码只读访问。
    /// </summary>
    /// <remarks>
    /// 业务代码请用本类的**静态生效值属性**（<see cref="EnableInjection"/> 等），
    /// 不要直接读 <see cref="FactionInfoMod.Settings"/> 的字段：静态属性统一处理了
    /// 「设置尚未载入（<c>null</c>）时按默认值处理」与取值钳制两件事，避免调用方各写一份判空。
    /// </remarks>
    public class FactionInfoSettings : ModSettings
    {
        // ==================== 注入时机 ====================

        /// <summary>常识注入总开关。关闭后新开档 / 读档 / 定时校正都不写入。</summary>
        public bool enableInjection = true;

        /// <summary>读档校正开关（D4 / FR-11）：读档时逐条比对，缺失则补、陈旧则重写、一致则不动。</summary>
        public bool enableBackfillOnLoad = true;

        /// <summary>运行中定时校正开关（FR-11）：关闭后仅在新开档 / 读档时校正。</summary>
        public bool enablePeriodicRefresh = true;

        /// <summary>定时校正间隔（游戏小时）。</summary>
        public int refreshIntervalHours = FactionKnowledgeConfig.DEFAULT_REFRESH_INTERVAL_HOURS;

        /// <summary>好感度重写阈值：差异仅来自好感度且未达此值时不为纯数值波动重写（FR-11）。</summary>
        public int goodwillRefreshThreshold = FactionKnowledgeConfig.GOODWILL_REFRESH_THRESHOLD;

        /// <summary>
        /// 预设库导入开关（FR-6 / D44）：在新档 / 读档把**已启用 mod** 对应的随包社区常识块导入上游常识库。
        /// 关闭后本路径不写入，但不影响本 mod 自建条目的注入。
        /// </summary>
        public bool enableKnowledgeBaseImport = true;

        /// <summary>
        /// 本体块导入开关（D45）：是否一并导入包内**无 <c>packageId</c>** 的 17 块
        /// （16 个「游戏本体」块 + 自定义常识）。体量较大，默认关；与 mod 块开关各自独立。
        /// </summary>
        public bool enableBuiltinKnowledgeImport;

        // ==================== 派系常识内容段 ====================

        /// <summary>是否写入「基础身份」段。</summary>
        public bool includeIdentity = true;

        /// <summary>是否写入「意识形态」段（无 Ideology DLC 时自动失效）。</summary>
        public bool includeIdeology = true;

        /// <summary>是否写入「与我方关系」段。</summary>
        public bool includeRelation = true;

        /// <summary>是否写入「领袖与据点」段。</summary>
        public bool includeSettlements = true;

        // ==================== 重要度与分类 ====================

        /// <summary>我方派系条目的重要度（0~1，默认 0.95 —— 对齐社区常识库顶级档）。</summary>
        public float knowledgeImportancePlayer = FactionKnowledgeConfig.DEFAULT_IMPORTANCE_PLAYER;

        /// <summary>其它派系与异种人条目的重要度（0~1，默认 0.80 —— 对齐社区常识库「派系本体」档）。</summary>
        public float knowledgeImportanceOther = FactionKnowledgeConfig.DEFAULT_IMPORTANCE_OTHER;

        /// <summary>是否强制 <c>category = KnowledgeEntryCategory.Lore</c>（世界观）。</summary>
        public bool categoryAlwaysLore = true;

        // ==================== 异种人常识 ====================

        /// <summary>是否写入独立的异种人常识条目（FR-9，无 Biotech DLC 时自动失效）。</summary>
        public bool includeXenotypes = true;

        /// <summary>是否写入派系与异种人的双向引用（派系 ⑤ 段 + 异种人「出没派系」段）。</summary>
        public bool includeFactionXenotypeRefs = true;

        /// <summary>异种人常识里列出的「标志性基因」条数（0 = 不写基因段）。</summary>
        public int xenotypeGeneCount = FactionKnowledgeConfig.DEFAULT_XENOTYPE_GENE_COUNT;

        // ==================== 日志与界面 ====================

        /// <summary>输出逐派系 / 逐异种人的明细日志（开发者模式下始终输出）。</summary>
        public bool enableVerboseLog;

        /// <summary>
        /// 是否在派系界面（派系列表悬停提示、派系信息卡）追加「实际成员」构成。
        /// 默认开启：游戏自带的「成员异种人概率」在 HAR 种族 / 兵种级异种人派系上会显示
        /// 「智人种 100%」这类误导内容，本项用于把那层修正显示出来；
        /// 不喜欢多余文本的玩家可关闭，关闭后常识注入不受影响。
        /// </summary>
        public bool showCompositionInFactionUi = true;

        /// <summary>
        /// 是否优化上游常识库界面的滚动性能（D48）。
        /// 上游 <c>Dialog_CommonKnowledge</c> 的中心列表把虚拟列表控件建好却从未调用其绘制方法，
        /// 改成手写全量遍历绘制，条目一多就每帧卡顿。开启后本 mod 只绘制滚动视口内的行，
        /// 不改上游布局与功能；出现异常可关闭以恢复上游原始行为。
        /// </summary>
        public bool enableKnowledgeUiOptimization = true;

        /// <summary>
        /// 玩家自建的自定义子页（FR-16 / D49）：每个子页是一个有名字的条目集合，
        /// 由常识管理页面手工挑选条目组成。随设置文件持久化，不写入上游常识库。
        /// </summary>
        public List<KnowledgeSubPage> customKnowledgePages = new List<KnowledgeSubPage>();

        /// <summary>读写设置文件（引擎在载入与关闭设置窗时调用）。</summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref enableInjection, FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_INJECTION, true);
            Scribe_Values.Look(ref enableBackfillOnLoad, FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_BACKFILL_ON_LOAD, true);
            Scribe_Values.Look(ref enablePeriodicRefresh, FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_PERIODIC_REFRESH, true);
            Scribe_Values.Look(ref refreshIntervalHours, FactionKnowledgeConfig.SETTINGS_KEY_REFRESH_INTERVAL_HOURS,
                FactionKnowledgeConfig.DEFAULT_REFRESH_INTERVAL_HOURS);
            Scribe_Values.Look(ref goodwillRefreshThreshold, FactionKnowledgeConfig.SETTINGS_KEY_GOODWILL_REFRESH_THRESHOLD,
                FactionKnowledgeConfig.GOODWILL_REFRESH_THRESHOLD);
            Scribe_Values.Look(ref enableKnowledgeBaseImport,
                FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_KNOWLEDGE_BASE_IMPORT, true);
            Scribe_Values.Look(ref enableBuiltinKnowledgeImport,
                FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_BUILTIN_KNOWLEDGE_IMPORT, false);

            Scribe_Values.Look(ref includeIdentity, FactionKnowledgeConfig.SETTINGS_KEY_INCLUDE_IDENTITY, true);
            Scribe_Values.Look(ref includeIdeology, FactionKnowledgeConfig.SETTINGS_KEY_INCLUDE_IDEOLOGY, true);
            Scribe_Values.Look(ref includeRelation, FactionKnowledgeConfig.SETTINGS_KEY_INCLUDE_RELATION, true);
            Scribe_Values.Look(ref includeSettlements, FactionKnowledgeConfig.SETTINGS_KEY_INCLUDE_SETTLEMENTS, true);

            Scribe_Values.Look(ref knowledgeImportancePlayer,
                FactionKnowledgeConfig.SETTINGS_KEY_KNOWLEDGE_IMPORTANCE_PLAYER,
                FactionKnowledgeConfig.DEFAULT_IMPORTANCE_PLAYER);
            Scribe_Values.Look(ref knowledgeImportanceOther,
                FactionKnowledgeConfig.SETTINGS_KEY_KNOWLEDGE_IMPORTANCE_OTHER,
                FactionKnowledgeConfig.DEFAULT_IMPORTANCE_OTHER);
            Scribe_Values.Look(ref categoryAlwaysLore, FactionKnowledgeConfig.SETTINGS_KEY_CATEGORY_ALWAYS_LORE, true);

            Scribe_Values.Look(ref includeXenotypes, FactionKnowledgeConfig.SETTINGS_KEY_INCLUDE_XENOTYPES, true);
            Scribe_Values.Look(ref includeFactionXenotypeRefs,
                FactionKnowledgeConfig.SETTINGS_KEY_INCLUDE_FACTION_XENOTYPE_REFS, true);
            Scribe_Values.Look(ref xenotypeGeneCount, FactionKnowledgeConfig.SETTINGS_KEY_XENOTYPE_GENE_COUNT,
                FactionKnowledgeConfig.DEFAULT_XENOTYPE_GENE_COUNT);

            Scribe_Values.Look(ref enableVerboseLog, FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_VERBOSE_LOG, false);
            Scribe_Values.Look(ref showCompositionInFactionUi, FactionKnowledgeConfig.SETTINGS_KEY_SHOW_UI_COMPOSITION, true);
            Scribe_Values.Look(ref enableKnowledgeUiOptimization,
                FactionKnowledgeConfig.SETTINGS_KEY_ENABLE_KNOWLEDGE_UI_OPTIMIZATION, true);

            Scribe_Collections.Look(ref customKnowledgePages,
                FactionKnowledgeConfig.SETTINGS_KEY_CUSTOM_KNOWLEDGE_PAGES, LookMode.Deep);
            if (customKnowledgePages == null)
            {
                customKnowledgePages = new List<KnowledgeSubPage>();
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // 清理脏数据：空项、成员表为空的项、名称为空的项（老存档 / 手改文件都可能出现）。
                customKnowledgePages.RemoveAll(page =>
                    page == null || string.IsNullOrEmpty(page.name) || page.entryIds == null);
            }
        }

        // ==================== 静态生效值 ====================
        // 设置尚未载入（FactionInfoMod.Settings 为 null）时一律按默认值处理，与 FR-14 补丁的兜底口径一致。

        /// <summary>当前设置实例；未载入时为 <c>null</c>。</summary>
        private static FactionInfoSettings Instance
        {
            get { return FactionInfoMod.Settings; }
        }

        /// <summary>生效的总开关（未载入时按默认「开」处理）。</summary>
        public static bool EnableInjection
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.enableInjection;
            }
        }

        /// <summary>生效的读档校正开关（未载入时按默认「开」处理）。</summary>
        public static bool EnableBackfillOnLoad
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.enableBackfillOnLoad;
            }
        }

        /// <summary>生效的定时校正开关（未载入时按默认「开」处理）。</summary>
        public static bool EnablePeriodicRefresh
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.enablePeriodicRefresh;
            }
        }

        /// <summary>生效的定时校正间隔（游戏小时，已钳制到合法区间）。</summary>
        public static int RefreshIntervalHours
        {
            get
            {
                FactionInfoSettings s = Instance;
                int hours = s == null
                    ? FactionKnowledgeConfig.DEFAULT_REFRESH_INTERVAL_HOURS
                    : s.refreshIntervalHours;
                return Clamp(
                    hours,
                    FactionKnowledgeConfig.REFRESH_INTERVAL_HOURS_MIN,
                    FactionKnowledgeConfig.REFRESH_INTERVAL_HOURS_MAX);
            }
        }

        /// <summary>生效的定时校正间隔（tick）。</summary>
        public static int RefreshIntervalTicks
        {
            get { return RefreshIntervalHours * FactionKnowledgeConfig.TICKS_PER_HOUR; }
        }

        /// <summary>生效的好感度重写阈值（已钳制到合法区间）。</summary>
        public static int GoodwillRefreshThreshold
        {
            get
            {
                FactionInfoSettings s = Instance;
                int value = s == null
                    ? FactionKnowledgeConfig.GOODWILL_REFRESH_THRESHOLD
                    : s.goodwillRefreshThreshold;
                return Clamp(
                    value,
                    FactionKnowledgeConfig.GOODWILL_THRESHOLD_MIN,
                    FactionKnowledgeConfig.GOODWILL_THRESHOLD_MAX);
            }
        }

        /// <summary>生效的预设库（mod 块）导入开关（未载入时按默认「开」处理）。</summary>
        public static bool EnableKnowledgeBaseImport
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.enableKnowledgeBaseImport;
            }
        }

        /// <summary>生效的本体块导入开关（未载入时按默认「关」处理）。</summary>
        public static bool EnableBuiltinKnowledgeImport
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s != null && s.enableBuiltinKnowledgeImport;
            }
        }

        /// <summary>生效的「基础身份」段开关。</summary>
        public static bool IncludeIdentity
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.includeIdentity;
            }
        }

        /// <summary>生效的「意识形态」段开关。</summary>
        public static bool IncludeIdeology
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.includeIdeology;
            }
        }

        /// <summary>生效的「与我方关系」段开关。</summary>
        public static bool IncludeRelation
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.includeRelation;
            }
        }

        /// <summary>生效的「领袖与据点」段开关。</summary>
        public static bool IncludeSettlements
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.includeSettlements;
            }
        }

        /// <summary>生效的**我方派系**条目重要度（已钳制到 0~1）。</summary>
        public static float KnowledgeImportancePlayer
        {
            get
            {
                FactionInfoSettings s = Instance;
                return ClampImportance(s == null
                    ? FactionKnowledgeConfig.DEFAULT_IMPORTANCE_PLAYER
                    : s.knowledgeImportancePlayer);
            }
        }

        /// <summary>生效的**其它派系与异种人**条目重要度（已钳制到 0~1）。</summary>
        public static float KnowledgeImportanceOther
        {
            get
            {
                FactionInfoSettings s = Instance;
                return ClampImportance(s == null
                    ? FactionKnowledgeConfig.DEFAULT_IMPORTANCE_OTHER
                    : s.knowledgeImportanceOther);
            }
        }

        /// <summary>生效的「分类固定为世界观」开关。</summary>
        public static bool CategoryAlwaysLore
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.categoryAlwaysLore;
            }
        }

        /// <summary>生效的「写入独立异种人条目」开关。</summary>
        public static bool IncludeXenotypes
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.includeXenotypes;
            }
        }

        /// <summary>生效的「写入双向引用」开关。</summary>
        public static bool IncludeFactionXenotypeRefs
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.includeFactionXenotypeRefs;
            }
        }

        /// <summary>生效的异种人标志性基因条数（已钳制到合法区间）。</summary>
        public static int XenotypeGeneCount
        {
            get
            {
                FactionInfoSettings s = Instance;
                int value = s == null
                    ? FactionKnowledgeConfig.DEFAULT_XENOTYPE_GENE_COUNT
                    : s.xenotypeGeneCount;
                return Clamp(
                    value,
                    FactionKnowledgeConfig.XENOTYPE_GENE_COUNT_MIN,
                    FactionKnowledgeConfig.XENOTYPE_GENE_COUNT_MAX);
            }
        }

        /// <summary>生效的明细日志开关（默认关）。</summary>
        public static bool EnableVerboseLog
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s != null && s.enableVerboseLog;
            }
        }

        /// <summary>生效的常识库界面滚动优化开关（未载入时按默认「开」处理）。</summary>
        public static bool EnableKnowledgeUiOptimization
        {
            get
            {
                FactionInfoSettings s = Instance;
                return s == null || s.enableKnowledgeUiOptimization;
            }
        }

        /// <summary>
        /// 生效的自定义子页列表（FR-16 / D49）。
        /// 设置尚未载入时返回 <c>null</c>，调用方须判空；返回的是设置内的活列表，可直接增删。
        /// </summary>
        public static List<KnowledgeSubPage> CustomKnowledgePages
        {
            get
            {
                FactionInfoSettings s = Instance;
                if (s == null)
                {
                    return null;
                }
                if (s.customKnowledgePages == null)
                {
                    s.customKnowledgePages = new List<KnowledgeSubPage>();
                }
                return s.customKnowledgePages;
            }
        }

        /// <summary>把整数钳制到 <paramref name="min"/>~<paramref name="max"/>（含端点）。</summary>
        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            return value > max ? max : value;
        }

        /// <summary>把条目重要度钳制到 <see cref="FactionKnowledgeConfig.IMPORTANCE_MIN"/>~<see cref="FactionKnowledgeConfig.IMPORTANCE_MAX"/>。</summary>
        private static float ClampImportance(float value)
        {
            if (value < FactionKnowledgeConfig.IMPORTANCE_MIN)
            {
                return FactionKnowledgeConfig.IMPORTANCE_MIN;
            }
            return value > FactionKnowledgeConfig.IMPORTANCE_MAX
                ? FactionKnowledgeConfig.IMPORTANCE_MAX
                : value;
        }
    }
}
