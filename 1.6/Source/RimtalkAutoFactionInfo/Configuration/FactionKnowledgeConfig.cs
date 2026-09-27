namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 全局常量与默认值。
    /// 约定：业务代码中不得出现裸字面量（前缀、模板、默认值、日志 key 等），一律从此类取值。
    /// 文案类常量均为**单行**：上游 <c>ImportFromText</c> 按 <c>\n</c> 切行，内容含换行会破坏导入导出格式（证据 ⑧）。
    /// </summary>
    public static class FactionKnowledgeConfig
    {
        // ---------- 前缀与标签 ----------

        /// <summary>日志统一前缀，所有日志行以此开头，便于按本 mod 过滤。</summary>
        public const string LOG_PREFIX = "[派系常识]";

        /// <summary>派系常识的内容前缀，同时作为「本 mod 注入」的幂等判定标记。</summary>
        public const string FACTION_CONTENT_PREFIX = "【派系】";

        /// <summary>异种人常识的内容前缀，同时作为「本 mod 注入」的幂等判定标记。</summary>
        public const string XENOTYPE_CONTENT_PREFIX = "【异种人】";

        /// <summary>
        /// 触发标签内多关键词的分隔符（半角逗号）。
        /// **必须是上游 <c>GetTags()</c> 认的 5 个字符之一**（`, ， 、 ; ；`，证据 ㉗），否则整串会被当成一个词。
        /// </summary>
        public const string TAG_SEPARATOR = ",";

        /// <summary>内容内部的列表连接符（与标签分隔符无关，内容不参与标签切分）。</summary>
        public const string LIST_SEPARATOR = "、";

        // ---------- 数值默认值 ----------

        /// <summary>注入条目的默认重要度（世界观级最高档，0~1）。</summary>
        public const float DEFAULT_IMPORTANCE = 1.0f;

        /// <summary>
        /// 常识条目「定向殖民者」的取值：<c>-1</c> 表示不限定，任何殖民者都可匹配（证据 ㉘）。
        /// </summary>
        public const int TARGET_PAWN_ALL = -1;

        /// <summary>异种人常识默认列出的「标志性基因」条数（0 表示不写该段）。</summary>
        public const int DEFAULT_XENOTYPE_GENE_COUNT = 4;

        // ---------- 定时刷新与覆写冷却（FR-11 / FR-12）----------

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
        /// 用于在游戏内快速观察覆写行为，不必真等 3 天（FR-12）。
        /// </summary>
        public const int DEBUG_REWRITE_COOLDOWN_TICKS = TICKS_PER_HOUR;

        /// <summary>
        /// 好感度重写阈值：与上次写入值相差**未达**此值时，不为纯好感度波动重写条目
        /// （避免 -75 → -74 这类无意义写入）。此判定独立于覆写冷却：小幅波动直接不写。
        /// </summary>
        public const int GOODWILL_REFRESH_THRESHOLD = 10;

        // ---------- 派系常识内容模板（FR-3）----------

        /// <summary>① 基础身份：{0}=派系名，{1}=类型标签，{2}=科技水平。</summary>
        public const string FACTION_SEG_IDENTITY = "{0}是一支{1}派系，科技水平为{2}。";

        /// <summary>① 追加定义原文：{0}=介绍文本。文本为空时整句省略（FR-3 ① 缺省处理）。</summary>
        public const string FACTION_SEG_DESCRIPTION = "{0}。";

        /// <summary>② 意识形态（有信条）：{0}=理念名，{1}=信条列表。</summary>
        public const string FACTION_SEG_IDEO = "他们信奉「{0}」，核心信条是{1}。";

        /// <summary>② 意识形态（理念无信条）：{0}=理念名。</summary>
        public const string FACTION_SEG_IDEO_NO_MEMES = "他们信奉「{0}」。";

        /// <summary>③ 与我方关系：{0}=派系名，{1}=关系标签。</summary>
        public const string FACTION_SEG_RELATION = "我们与{0}的关系是{1}。";

        /// <summary>③ 好感度子句：{0}=数值。仅在 <c>Faction.HasGoodwill</c> 为真时追加。</summary>
        public const string FACTION_SEG_GOODWILL = "其对我们好感度为{0}。";

        /// <summary>④ 领袖：{0}=派系名，{1}=领袖名。</summary>
        public const string FACTION_SEG_LEADER = "{0}的领袖是{1}。";

        /// <summary>④ 无领袖：{0}=派系名。</summary>
        public const string FACTION_SEG_LEADER_NONE = "{0}目前没有已知的领袖。";

        /// <summary>④ 据点数量：{0}=数量。</summary>
        public const string FACTION_SEG_SETTLEMENT_COUNT = "他们在世界地图上共有{0}处定居点。";

        /// <summary>④ 最近据点距离：{0}=格数。</summary>
        public const string FACTION_SEG_SETTLEMENT_NEAREST = "最近的一处距我们约{0}格。";

        /// <summary>⑤ 成员异种人构成：{0}=「异种人名（约p%）」列表。</summary>
        public const string FACTION_SEG_MEMBERS = "他们的成员主要是{0}。";

        /// <summary>⑤ 单个异种人占比项：{0}=异种人名，{1}=百分比文本。</summary>
        public const string FACTION_SEG_MEMBERS_ITEM = "{0}（约{1}）";

        // ---------- 异种人常识内容模板（FR-9.2）----------

        /// <summary>① 是什么：{0}=异种人名，{1}=描述。</summary>
        public const string XENOTYPE_SEG_INTRO = XENOTYPE_CONTENT_PREFIX + "{0}：{1}";

        /// <summary>① 是什么（无描述文本时）：{0}=异种人名。</summary>
        public const string XENOTYPE_SEG_INTRO_BARE = XENOTYPE_CONTENT_PREFIX + "{0}";

        /// <summary>② 标志性基因：{0}=基因列表。</summary>
        public const string XENOTYPE_SEG_GENES = "标志性基因：{0}。";

        /// <summary>② 无基因时的固定文案（基础异种人即此情况）。</summary>
        public const string XENOTYPE_NO_GENES = "没有任何特殊基因，是自然演化的人类。";

        /// <summary>③ 出没派系：{0}=派系列表。</summary>
        public const string XENOTYPE_SEG_FACTIONS = "他们主要在{0}出没。";

        // ---------- 日志 key（供 KnowledgeLog.WarnOnce 去重）----------

        /// <summary>日志 key：上游常识库不可用。</summary>
        public const int LOG_KEY_LIBRARY_UNAVAILABLE = 1;
    }
}
