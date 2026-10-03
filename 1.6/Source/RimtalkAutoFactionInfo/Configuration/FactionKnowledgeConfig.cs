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

        // ---------- 前缀与标签 ----------

        /// <summary>日志统一前缀，所有日志行以此开头，便于按本 mod 过滤。</summary>
        public const string LOG_PREFIX = "[派系常识]";

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

        // ---------- 数值默认值 ----------

        /// <summary>注入条目的默认重要度（世界观级最高档，0~1）。</summary>
        public const float DEFAULT_IMPORTANCE = 1.0f;

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

        // ---------- 启动包信息与注入合计日志 ----------

        /// <summary>启动日志：本 mod 的包标识与加载来源。{0}=包标识，{1}=来源文案。</summary>
        public const string LOG_MOD_ORIGIN = "本 mod 已加载：包标识 {0}，来源 {1}。";

        /// <summary>来源文案：Steam 创意工坊。</summary>
        public const string MOD_SOURCE_STEAM_WORKSHOP = "创意工坊";

        /// <summary>来源文案：本地 Mods 文件夹。</summary>
        public const string MOD_SOURCE_MODS_FOLDER = "本地 Mods 文件夹";

        /// <summary>来源文案：来源未知（元数据缺失）。</summary>
        public const string MOD_SOURCE_UNDEFINED = "未知来源";

        /// <summary>初始化完成日志：本次注入的合计条数。{0}=派系条数，{1}=异种人条数。</summary>
        public const string LOG_INJECTION_TOTAL = "常识注入完成：派系 {0} 条，异种人 {1} 条。";

        /// <summary>告警：读取主基地财富失败（触发全图重算时越界），本次省略财富段。{0}=异常摘要。</summary>
        public const string WARN_PLAYER_WEALTH_UNAVAILABLE = "读取主基地财富失败，本次省略我方派系的财富段：{0}";

        // ---------- 日志 key（供 KnowledgeLog.WarnOnce 去重）----------

        /// <summary>日志 key：上游常识库不可用。</summary>
        public const int LOG_KEY_LIBRARY_UNAVAILABLE = 1;

        /// <summary>日志 key：读取主基地财富失败。</summary>
        public const int LOG_KEY_PLAYER_WEALTH_UNAVAILABLE = 2;

        /// <summary>日志 key：派系界面「实际成员」构建失败。</summary>
        public const int LOG_KEY_UI_COMPOSITION_FAILED = 3;
    }
}
