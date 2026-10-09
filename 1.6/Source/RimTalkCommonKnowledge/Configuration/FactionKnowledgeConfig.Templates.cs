namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 常识内容模板（<see cref="FactionKnowledgeConfig"/> 的 partial 分片）：
    /// 普通派系五段、我方派系八段、异种人三段，以及派系界面「实际成员」块。
    /// 文案类常量均为**单行**：上游 <c>ImportFromText</c> 按 <c>\n</c> 切行，内容含换行会破坏导入导出格式。
    /// </summary>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 派系常识内容模板 ----------

        /// <summary>① 基础身份：{0}=派系名，{1}=类型标签，{2}=科技水平。</summary>
        public const string FACTION_SEG_IDENTITY = "{0}是一支{1}派系，科技水平为{2}。";

        /// <summary>
        /// ① 基础身份（无科技水平时）：{0}=派系名，{1}=类型标签。
        /// 用于 <c>FactionDef.techLevel</c> 为 <c>TechLevel.Undefined</c> 的派系
        /// （Def 未声明科技水平，照原模板会写出「科技水平为Undefined」这种假数据）。
        /// </summary>
        public const string FACTION_SEG_IDENTITY_NO_TECH = "{0}是一支{1}派系。";

        /// <summary>
        /// ① 基础身份（名字与类型标签重复时）：{0}=名字，{1}=科技水平。
        /// 隐藏派系（如原版机械族、虫族）<c>HasName</c> 为假、<c>Name</c> 回退为 <c>def.LabelCap</c>，
        /// 与类型标签同字，故省去「是一支 X 派系」以免同义重复。
        /// </summary>
        public const string FACTION_SEG_IDENTITY_SAME = "{0}，科技水平为{1}。";

        /// <summary>① 基础身份（名字与类型标签重复、且无科技水平时）：{0}=名字。</summary>
        public const string FACTION_SEG_IDENTITY_SAME_NO_TECH = "{0}。";

        /// <summary>① 追加定义原文：{0}=介绍文本。文本为空时整句省略。</summary>
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

        /// <summary>
        /// ⑤ 成员种族（外星种族，如 HAR / alien race 派系）：{0}=种族名列表（顿号连接）。
        /// 与 <see cref="FACTION_SEG_MEMBERS"/> **互斥**：这类派系的成员在 Biotech 异种人体系里
        /// 一律登记为智人种（<c>Baseliner</c>），照异种人口径写会得到「智人种 100%」这类误导内容，
        /// 故成员面貌改由本句表达。
        /// </summary>
        public const string FACTION_SEG_MEMBER_RACES = "他们的外星种族成员包括{0}。";

        /// <summary>⑤ 成员异种人构成：{0}=「异种人名（约p%）」列表。</summary>
        public const string FACTION_SEG_MEMBERS = "他们的成员主要是{0}。";

        /// <summary>⑤ 单个异种人占比项：{0}=异种人名，{1}=百分比文本。</summary>
        public const string FACTION_SEG_MEMBERS_ITEM = "{0}（约{1}）";

        // ---------- 派系界面「实际成员」块 ----------

        /// <summary>
        /// 派系界面「实际成员」（派系级分布不可信、只能按兵种列举时）：{0}=异种人名列表（顿号连接）。
        /// 与常识 ⑤ 段的 <see cref="FACTION_SEG_MEMBER_RACES"/> 口径一致，只是**不写占比**——
        /// 兵种级集合是无条件并集，同一异种人可能只出现在个别特殊兵种上，写占比会误导。
        /// </summary>
        public const string UI_COMPOSITION_KIND_XENOTYPES = "他们的成员涉及以下异种人：{0}。";

        /// <summary>
        /// 派系界面「实际成员」中**外星种族**块的标题（对应原版 <c>MemberXenotypeChances</c> 段的标题位置）。
        /// 种族在 Def 层没有概率声明（<c>PawnKindDef.race</c> 是单一确定值），故只列名称、不写占比。
        /// </summary>
        public const string UI_COMPOSITION_MEMBER_RACES_TITLE = "外星种族";

        /// <summary>
        /// 派系界面名称列表的每行前缀，对齐原版成员段 <c>ToLineList("  - ", false)</c> 的排版。
        /// </summary>
        public const string UI_LIST_ITEM_PREFIX = "  - ";

        // ---------- 我方派系介绍内容模板 ----------

        /// <summary>我方派系 ① 身份前半：{0}=派系名。</summary>
        public const string PLAYER_SEG_IDENTITY_PREFIX = "「{0}」";

        /// <summary>
        /// 我方派系 ① 身份后半（固定句），与前半拼成「「{派系名}」是我们自己所属的派系。」。
        /// ⚠ 本句同时充当「我方派系条目」的**无名字识别标记**：派系改名后 tag 已变，
        ///   旧条目只能靠这句与名字无关的固定文案被找回并删除（见 <c>RimTalkMemoryBridge.FindManagedIdsByContent</c>）。
        ///   **不要为了改文案而改动本句**，否则存量存档里的旧条目将失去识别依据。
        /// </summary>
        public const string PLAYER_SEG_IDENTITY_SUFFIX = "是我们自己所属的派系。";

        /// <summary>
        /// 我方派系 ① 身份段之后的开局剧本句：{0}=剧本名（如「迫降」「失落的部落」「赤裸的暴行」）。
        /// 读不到剧本时整句省略。
        /// </summary>
        public const string PLAYER_SEG_SCENARIO = "我们的开局剧本是「{0}」。";

        /// <summary>我方派系 ② 人口：{0}=殖民者数（不含奴隶 / 囚犯 / 临时成员），{1}=附加类别子句（无则空串）。</summary>
        public const string PLAYER_SEG_POPULATION = "我们有{0}名殖民者{1}。";

        /// <summary>我方派系 ② 人口附加类别的引导语。</summary>
        public const string PLAYER_SEG_POPULATION_EXTRA_LEAD = "，另有";

        /// <summary>我方派系 ② 人口附加类别项：{0}=人数，{1}=类别名。</summary>
        public const string PLAYER_SEG_POPULATION_EXTRA_ITEM = "{0}名{1}";

        /// <summary>人口类别名：临时成员（任务寄居者，如难民 / 来做客的）。</summary>
        public const string PLAYER_POP_LABEL_TEMPORARY = "临时成员";

        /// <summary>人口类别名：囚犯。</summary>
        public const string PLAYER_POP_LABEL_PRISONER = "囚犯";

        /// <summary>人口类别名：奴隶。</summary>
        public const string PLAYER_POP_LABEL_SLAVE = "奴隶";

        /// <summary>
        /// 我方派系 ③ 友方机械族（需 Biotech）：{0}=型号构成列表（「N 台某型」，以顿号连接）。
        /// 仅在启用 Biotech 时输出；未启用时整段跳过（此时不存在机械族，写了只是噪声）。
        /// </summary>
        public const string PLAYER_SEG_MECHS = "我们还有{0}。";

        /// <summary>我方派系 ③ 未启用 Biotech / 尚未拥有机械族时的替代句（仅 Biotech 启用时输出）。</summary>
        public const string PLAYER_SEG_MECHS_NONE = "我们目前没有友方机械族。";

        /// <summary>我方派系 ③ 机械族型号项：{0}=台数，{1}=型号名（如「清扫机」）。</summary>
        public const string PLAYER_SEG_MECHS_ITEM = "{0}台{1}";

        /// <summary>我方派系 ④：{0}=据点名列表。</summary>
        public const string PLAYER_SEG_SETTLEMENTS = "我们的据点是{0}。";

        /// <summary>我方派系 ④ 无据点时的替代句。</summary>
        public const string PLAYER_SEG_SETTLEMENTS_NONE = "我们目前还没有固定的据点。";

        /// <summary>我方派系 ⑤ 气候：{0}=主基地所属生物群系名（如「温带森林」）。</summary>
        public const string PLAYER_SEG_CLIMATE = "殖民地所在位置的气候是{0}。";

        /// <summary>我方派系 ⑥ 可读停留时长：{0}=飞船名，{1}=停留时长。</summary>
        public const string PLAYER_SEG_GRAVSHIP = "逆重飞船「{0}」已在此停留{1}。";

        /// <summary>我方派系 ⑥ 读不到着陆时刻：{0}=飞船名。</summary>
        public const string PLAYER_SEG_GRAVSHIP_BARE = "我们有一艘逆重飞船「{0}」。";

        /// <summary>我方派系 ⑦：{0}=时长文本。</summary>
        public const string PLAYER_SEG_AGE = "这个派系已经建立了{0}。";

        /// <summary>我方派系 ⑧：{0}=财富值。</summary>
        public const string PLAYER_SEG_WEALTH = "主基地的财富约为{0}。";

        // ---------- 异种人常识内容模板 ----------

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
    }
}
