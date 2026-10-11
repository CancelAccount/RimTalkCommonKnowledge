using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 常识内容模板（<see cref="FactionKnowledgeConfig"/> 的 partial 分片）：
    /// 普通派系五段、我方派系八段、异种人三段，以及派系界面「实际成员」块。
    /// 文案均为**单行**（上游 <c>ImportFromText</c> 按 <c>\n</c> 切行，内容含换行会破坏导入导出格式），
    /// 且全部走 Keyed 翻译随游戏语言切换（译文维护在 <c>Languages/&lt;语言&gt;/Keyed/RimTalkCommonKnowledge.xml</c>）。
    /// 原为 const 的模板改为静态属性 + <c>Translate()</c>，既有 string.Format 调用点无需改动；
    /// 译文中的 {0}/{1} 占位符顺序与语义不得变动。
    /// </summary>
    public static partial class FactionKnowledgeConfig
    {
        // ---------- 派系常识内容模板 ----------

        /// <summary>① 基础身份：{0}=派系名，{1}=类型标签，{2}=科技水平。</summary>
        public static string FACTION_SEG_IDENTITY => "RTCK.Tpl.FactionIdentity".Translate();

        /// <summary>
        /// ① 基础身份（无科技水平时）：{0}=派系名，{1}=类型标签。
        /// 用于 <c>FactionDef.techLevel</c> 为 <c>TechLevel.Undefined</c> 的派系
        /// （Def 未声明科技水平，照原模板会写出「科技水平为Undefined」这种假数据）。
        /// </summary>
        public static string FACTION_SEG_IDENTITY_NO_TECH => "RTCK.Tpl.FactionIdentityNoTech".Translate();

        /// <summary>
        /// ① 基础身份（名字与类型标签重复时）：{0}=名字，{1}=科技水平。
        /// 隐藏派系（如原版机械族、虫族）<c>HasName</c> 为假、<c>Name</c> 回退为 <c>def.LabelCap</c>，
        /// 与类型标签同字，故省去「是一支 X 派系」以免同义重复。
        /// </summary>
        public static string FACTION_SEG_IDENTITY_SAME => "RTCK.Tpl.FactionIdentitySame".Translate();

        /// <summary>① 基础身份（名字与类型标签重复、且无科技水平时）：{0}=名字。</summary>
        public static string FACTION_SEG_IDENTITY_SAME_NO_TECH => "RTCK.Tpl.FactionIdentitySameNoTech".Translate();

        /// <summary>① 追加定义原文：{0}=介绍文本。文本为空时整句省略。</summary>
        public static string FACTION_SEG_DESCRIPTION => "RTCK.Tpl.FactionDescription".Translate();

        /// <summary>② 意识形态（有信条）：{0}=理念名，{1}=信条列表。</summary>
        public static string FACTION_SEG_IDEO => "RTCK.Tpl.FactionIdeo".Translate();

        /// <summary>② 意识形态（理念无信条）：{0}=理念名。</summary>
        public static string FACTION_SEG_IDEO_NO_MEMES => "RTCK.Tpl.FactionIdeoNoMemes".Translate();

        /// <summary>③ 与我方关系：{0}=派系名，{1}=关系标签。</summary>
        public static string FACTION_SEG_RELATION => "RTCK.Tpl.FactionRelation".Translate();

        /// <summary>③ 好感度子句：{0}=数值。仅在 <c>Faction.HasGoodwill</c> 为真时追加。</summary>
        public static string FACTION_SEG_GOODWILL => "RTCK.Tpl.FactionGoodwill".Translate();

        /// <summary>④ 领袖：{0}=派系名，{1}=领袖名。</summary>
        public static string FACTION_SEG_LEADER => "RTCK.Tpl.FactionLeader".Translate();

        /// <summary>④ 无领袖：{0}=派系名。</summary>
        public static string FACTION_SEG_LEADER_NONE => "RTCK.Tpl.FactionLeaderNone".Translate();

        /// <summary>④ 据点数量：{0}=数量。</summary>
        public static string FACTION_SEG_SETTLEMENT_COUNT => "RTCK.Tpl.FactionSettlementCount".Translate();

        /// <summary>④ 最近据点距离：{0}=格数。</summary>
        public static string FACTION_SEG_SETTLEMENT_NEAREST => "RTCK.Tpl.FactionSettlementNearest".Translate();

        /// <summary>
        /// ⑤ 成员种族（外星种族，如 HAR / alien race 派系）：{0}=种族名列表（顿号连接）。
        /// 与 <see cref="FACTION_SEG_MEMBERS"/> **互斥**：这类派系的成员在 Biotech 异种人体系里
        /// 一律登记为智人种（<c>Baseliner</c>），照异种人口径写会得到「智人种 100%」这类误导内容，
        /// 故成员面貌改由本句表达。
        /// </summary>
        public static string FACTION_SEG_MEMBER_RACES => "RTCK.Tpl.FactionMemberRaces".Translate();

        /// <summary>⑤ 成员异种人构成：{0}=「异种人名（约p%）」列表。</summary>
        public static string FACTION_SEG_MEMBERS => "RTCK.Tpl.FactionMembers".Translate();

        /// <summary>⑤ 单个异种人占比项：{0}=异种人名，{1}=百分比文本。</summary>
        public static string FACTION_SEG_MEMBERS_ITEM => "RTCK.Tpl.FactionMembersItem".Translate();

        // ---------- 派系界面「实际成员」块 ----------

        /// <summary>
        /// 派系界面「实际成员」（派系级分布不可信、只能按兵种列举时）：{0}=异种人名列表（顿号连接）。
        /// 与常识 ⑤ 段的 <see cref="FACTION_SEG_MEMBER_RACES"/> 口径一致，只是**不写占比**——
        /// 兵种级集合是无条件并集，同一异种人可能只出现在个别特殊兵种上，写占比会误导。
        /// </summary>
        public static string UI_COMPOSITION_KIND_XENOTYPES => "RTCK.Tpl.UiCompositionKindXenotypes".Translate();

        /// <summary>
        /// 派系界面「实际成员」中**外星种族**块的标题（对应原版 <c>MemberXenotypeChances</c> 段的标题位置）。
        /// 种族在 Def 层没有概率声明（<c>PawnKindDef.race</c> 是单一确定值），故只列名称、不写占比。
        /// </summary>
        public static string UI_COMPOSITION_MEMBER_RACES_TITLE => "RTCK.Tpl.UiCompositionMemberRacesTitle".Translate();

        /// <summary>
        /// 派系界面名称列表的每行前缀，对齐原版成员段 <c>ToLineList("  - ", false)</c> 的排版。
        /// 纯排版字符、无语言属性，不纳入翻译。
        /// </summary>
        public const string UI_LIST_ITEM_PREFIX = "  - ";

        // ---------- 我方派系介绍内容模板 ----------

        /// <summary>我方派系 ① 身份前半：{0}=派系名。</summary>
        public static string PLAYER_SEG_IDENTITY_PREFIX => "RTCK.Tpl.PlayerIdentityPrefix".Translate();

        /// <summary>
        /// 我方派系 ① 身份后半（固定句），与前半拼成「「{派系名}」是我们自己所属的派系。」。
        /// ⚠ 本句同时充当「我方派系条目」的**无名字识别标记**：派系改名后 tag 已变，
        ///   旧条目只能靠这句与名字无关的固定文案被找回并删除（见 <c>RimTalkMemoryBridge.FindManagedIdsByContent</c>）。
        ///   **不要为了改文案而改动译文**；译文一旦调整，必须同步
        ///   <see cref="PLAYER_IDENTITY_SUFFIX_VARIANTS"/>（含全部历史译文），否则存量存档里的旧条目将失去识别依据。
        /// </summary>
        public static string PLAYER_SEG_IDENTITY_SUFFIX => "RTCK.Tpl.PlayerIdentitySuffix".Translate();

        /// <summary>
        /// 我方派系识别标记的全部语言变体（简中 / 繁中 / 英文）。
        /// 玩家切换游戏语言后，新写入的条目用新语言文案，而库内旧语言条目仍需能被识别——
        /// 查找时按本数组逐变体匹配（见 <c>RimTalkMemoryBridge.FindManagedIdsByContent</c>）。
        /// ⚠ 识别标记：译文的任何调整都必须把旧值保留在本数组中（只增不减），
        ///   否则旧存档里以旧文案写入的我方条目无法找回，改名后会残留成重复项。
        /// </summary>
        public static readonly string[] PLAYER_IDENTITY_SUFFIX_VARIANTS =
        {
            "是我们自己所属的派系。",
            "是我們自己所屬的派系。",
            " is the faction we belong to.",
        };

        /// <summary>
        /// 我方派系 ① 身份段之后的开局剧本句：{0}=剧本名（如「迫降」「失落的部落」「赤裸的暴行」）。
        /// 读不到剧本时整句省略。
        /// </summary>
        public static string PLAYER_SEG_SCENARIO => "RTCK.Tpl.PlayerScenario".Translate();

        /// <summary>我方派系 ② 人口：{0}=殖民者数（不含奴隶 / 囚犯 / 临时成员），{1}=附加类别子句（无则空串）。</summary>
        public static string PLAYER_SEG_POPULATION => "RTCK.Tpl.PlayerPopulation".Translate();

        /// <summary>我方派系 ② 人口附加类别的引导语。</summary>
        public static string PLAYER_SEG_POPULATION_EXTRA_LEAD => "RTCK.Tpl.PlayerPopulationExtraLead".Translate();

        /// <summary>我方派系 ② 人口附加类别项：{0}=人数，{1}=类别名。</summary>
        public static string PLAYER_SEG_POPULATION_EXTRA_ITEM => "RTCK.Tpl.PlayerPopulationExtraItem".Translate();

        /// <summary>人口类别名：临时成员（任务寄居者，如难民 / 来做客的）。</summary>
        public static string PLAYER_POP_LABEL_TEMPORARY => "RTCK.Tpl.PlayerPopLabelTemporary".Translate();

        /// <summary>人口类别名：囚犯。</summary>
        public static string PLAYER_POP_LABEL_PRISONER => "RTCK.Tpl.PlayerPopLabelPrisoner".Translate();

        /// <summary>人口类别名：奴隶。</summary>
        public static string PLAYER_POP_LABEL_SLAVE => "RTCK.Tpl.PlayerPopLabelSlave".Translate();

        /// <summary>
        /// 我方派系 ③ 友方机械族（需 Biotech）：{0}=型号构成列表（「N 台某型」，以顿号连接）。
        /// 仅在启用 Biotech 时输出；未启用时整段跳过（此时不存在机械族，写了只是噪声）。
        /// </summary>
        public static string PLAYER_SEG_MECHS => "RTCK.Tpl.PlayerMechs".Translate();

        /// <summary>我方派系 ③ 未启用 Biotech / 尚未拥有机械族时的替代句（仅 Biotech 启用时输出）。</summary>
        public static string PLAYER_SEG_MECHS_NONE => "RTCK.Tpl.PlayerMechsNone".Translate();

        /// <summary>我方派系 ③ 机械族型号项：{0}=台数，{1}=型号名（如「清扫机」）。</summary>
        public static string PLAYER_SEG_MECHS_ITEM => "RTCK.Tpl.PlayerMechsItem".Translate();

        /// <summary>我方派系 ④：{0}=据点名列表。</summary>
        public static string PLAYER_SEG_SETTLEMENTS => "RTCK.Tpl.PlayerSettlements".Translate();

        /// <summary>我方派系 ④ 无据点时的替代句。</summary>
        public static string PLAYER_SEG_SETTLEMENTS_NONE => "RTCK.Tpl.PlayerSettlementsNone".Translate();

        /// <summary>我方派系 ⑤ 气候：{0}=主基地所属生物群系名（如「温带森林」）。</summary>
        public static string PLAYER_SEG_CLIMATE => "RTCK.Tpl.PlayerClimate".Translate();

        /// <summary>我方派系 ⑥ 可读停留时长：{0}=飞船名，{1}=停留时长。</summary>
        public static string PLAYER_SEG_GRAVSHIP => "RTCK.Tpl.PlayerGravship".Translate();

        /// <summary>我方派系 ⑥ 读不到着陆时刻：{0}=飞船名。</summary>
        public static string PLAYER_SEG_GRAVSHIP_BARE => "RTCK.Tpl.PlayerGravshipBare".Translate();

        /// <summary>我方派系 ⑦：{0}=时长文本。</summary>
        public static string PLAYER_SEG_AGE => "RTCK.Tpl.PlayerAge".Translate();

        /// <summary>我方派系 ⑧：{0}=财富值。</summary>
        public static string PLAYER_SEG_WEALTH => "RTCK.Tpl.PlayerWealth".Translate();

        // ---------- 异种人常识内容模板 ----------

        /// <summary>
        /// ① 是什么：{0}=异种人名，{1}=描述。
        /// 译文中**内嵌异种人内容前缀**（与 <see cref="XENOTYPE_CONTENT_PREFIX"/> 同键值），
        /// 保证前缀与正文语言一致、且可被前缀变体数组识别。
        /// </summary>
        public static string XENOTYPE_SEG_INTRO => "RTCK.Tpl.XenotypeIntro".Translate();

        /// <summary>① 是什么（无描述文本时）：{0}=异种人名。同样内嵌异种人内容前缀。</summary>
        public static string XENOTYPE_SEG_INTRO_BARE => "RTCK.Tpl.XenotypeIntroBare".Translate();

        /// <summary>② 标志性基因：{0}=基因列表。</summary>
        public static string XENOTYPE_SEG_GENES => "RTCK.Tpl.XenotypeGenes".Translate();

        /// <summary>② 无基因时的固定文案（基础异种人即此情况）。</summary>
        public static string XENOTYPE_NO_GENES => "RTCK.Tpl.XenotypeNoGenes".Translate();

        /// <summary>③ 出没派系：{0}=派系列表。</summary>
        public static string XENOTYPE_SEG_FACTIONS => "RTCK.Tpl.XenotypeFactions".Translate();
    }
}
