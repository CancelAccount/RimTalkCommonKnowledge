using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 异种人（Xenotype）相关的取值与文案构建。
    /// 所有入口在未启用 Biotech 时一律返回空/假，不做任何 <c>XenotypeDef</c> 访问（未启用时
    /// <c>XenotypeDefOf.Baseliner</c> 等为 null，直接访问会 NRE）。
    /// </summary>
    public static class XenotypeKnowledgeBuilder
    {
        // ==================== 集合来源 ====================

        /// <summary>
        /// 该派系成员**可能出现**的异种人（去重）：<c>FactionDef.xenotypeSet</c> ∪ 主理念 <c>memes[].xenotypeSet</c>
        /// ∪ **兵种级 <c>PawnKindDef.xenotypeSet</c>**（见 <see cref="CollectKindLevelXenotypes"/>）。
        /// </summary>
        /// <remarks>
        /// 来源口径与游戏生成成员时的 <c>PawnGenerator.XenotypesAvailableFor</c> 一致（三者累加）；
        /// 漏掉兵种级会扫不出「派系级未声明、特色异种人只写在兵种上」的派系。
        /// 不含「基础异种人补差」——本方法用于建立「异种人 → 出没派系」的反向索引，
        /// 只应统计真正被显式指定的关系，否则每个派系都会把智人种列进去，列表噪声极大。
        /// </remarks>
        public static List<XenotypeDef> CollectExplicitXenotypes(Faction faction)
        {
            List<XenotypeDef> result = new List<XenotypeDef>();
            if (!ModsConfig.BiotechActive || faction == null || faction.def == null)
            {
                return result;
            }

            AppendXenotypes(result, faction.def.xenotypeSet);

            // 主理念可追加异种人倾向，游戏在生成 pawn 时会把它们累加进分布
            if (faction.ideos != null && faction.ideos.PrimaryIdeo != null)
            {
                List<MemeDef> memes = faction.ideos.PrimaryIdeo.memes;
                if (memes != null)
                {
                    for (int i = 0; i < memes.Count; i++)
                    {
                        if (memes[i] != null)
                        {
                            AppendXenotypes(result, memes[i].xenotypeSet);
                        }
                    }
                }
            }

            // 兵种级：派系级未声明时，特色异种人常常只写在 PawnKindDef 上（如混编机械族的 mod 派系）
            // 说的就是你，走地鸡
            List<XenotypeDef> kindLevel = CollectKindLevelXenotypes(faction);
            for (int i = 0; i < kindLevel.Count; i++)
            {
                if (!result.Contains(kindLevel[i]))
                {
                    result.Add(kindLevel[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// 该派系**兵种级**声明的异种人（去重，不含基础异种人）：
        /// 遍历 <c>FactionDef.pawnGroupMakers[].options[].kind.xenotypeSet</c>。
        /// </summary>
        /// <remarks>
        /// 游戏在 <c>PawnGenerator.XenotypesAvailableFor(kind, factionDef, faction)</c> 里把
        /// 「派系级（仅当 <c>kind.useFactionXenotypes</c>）+ 主理念 + **兵种级（无条件）**」三处累加。
        /// 只读派系级会漏掉「派系级未声明、靠兵种级指定特色异种人」的派系：
        /// 这类派系的派系级分布会退化成「智人种 100%」，其特色异种人也扫不出来。
        /// </remarks>
        public static List<XenotypeDef> CollectKindLevelXenotypes(Faction faction)
        {
            return faction != null
                ? CollectKindLevelXenotypes(faction.def)
                : new List<XenotypeDef>();
        }

        /// <summary>
        /// 该派系 Def 的**兵种级**声明的异种人（去重，不含基础异种人）。
        /// 供**只有 <see cref="FactionDef"/> 的场景**使用（如派系界面的介绍文本补丁）。
        /// </summary>
        /// <param name="def">目标派系 Def。</param>
        public static List<XenotypeDef> CollectKindLevelXenotypes(FactionDef def)
        {
            List<XenotypeDef> result = new List<XenotypeDef>();
            if (!ModsConfig.BiotechActive || def == null)
            {
                return result;
            }

            List<PawnGroupMaker> groupMakers = def.pawnGroupMakers;
            if (groupMakers == null)
            {
                return result;
            }

            for (int i = 0; i < groupMakers.Count; i++)
            {
                PawnGroupMaker groupMaker = groupMakers[i];
                if (groupMaker == null || groupMaker.options == null)
                {
                    continue;
                }
                for (int j = 0; j < groupMaker.options.Count; j++)
                {
                    PawnGenOption option = groupMaker.options[j];
                    PawnKindDef kind = option != null ? option.kind : null;
                    if (kind != null)
                    {
                        AppendXenotypes(result, kind.xenotypeSet);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 该派系需要在常识库里**单独立条**的异种人 = 显式集合 ∪ 基础异种人（仅当 <c>BaselinerChance &gt; 0</c>）。
        /// </summary>
        public static List<XenotypeDef> CollectEntryXenotypes(Faction faction)
        {
            List<XenotypeDef> result = CollectExplicitXenotypes(faction);
            if (!ModsConfig.BiotechActive || faction == null || faction.def == null)
            {
                return result;
            }

            // 基础异种人不在 xenotypeSet 里体现，其占比由 BaselinerChance 单独给出
            if (faction.def.BaselinerChance > 0f && !result.Contains(XenotypeDefOf.Baseliner))
            {
                result.Add(XenotypeDefOf.Baseliner);
            }
            return result;
        }

        /// <summary>
        /// 合并多个派系「需单独立条」的异种人集合并去重（世界级异种人集合 = 各有效派系的并集）。
        /// </summary>
        public static List<XenotypeDef> CollectEntryXenotypesFromAll(List<Faction> factions)
        {
            List<XenotypeDef> result = new List<XenotypeDef>();
            if (factions == null)
            {
                return result;
            }

            for (int i = 0; i < factions.Count; i++)
            {
                List<XenotypeDef> items = CollectEntryXenotypes(factions[i]);
                for (int j = 0; j < items.Count; j++)
                {
                    if (!result.Contains(items[j]))
                    {
                        result.Add(items[j]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 把 <paramref name="set"/> 中的异种人并入列表，跳过空项、重复项与**基础异种人**。
        /// 基础异种人的占比一律由 <c>BaselinerChance</c> / 补差表达，不在此处当作「显式关联」统计。
        /// </summary>
        private static void AppendXenotypes(List<XenotypeDef> result, XenotypeSet set)
        {
            if (set == null)
            {
                return;
            }
            // 底层 xenotypeChances 是 private，外部只能用 Count + 索引器遍历
            for (int i = 0; i < set.Count; i++)
            {
                XenotypeChance item = set[i];
                // item.xenotype == null 一项同时兜住「未启用 Biotech 时 XenotypeDefOf.Baseliner 为 null」
                if (item == null || item.xenotype == null || item.xenotype == XenotypeDefOf.Baseliner)
                {
                    continue;
                }
                if (!result.Contains(item.xenotype))
                {
                    result.Add(item.xenotype);
                }
            }
        }

        // ==================== 成员构成与特征异种人 ====================

        /// <summary>
        /// 按**游戏自身口径**汇总「成员异种人概率」：<c>BaselinerChance</c>（&gt; 0 时）+
        /// <c>FactionDef.xenotypeSet</c>（排除基础异种人），按概率降序。
        /// </summary>
        /// <remarks>
        /// 算法与 <c>FactionDef.Description</c> 属性的「成员异种人概率」段**完全同源**，
        /// 因此本 mod 呈现的占比与游戏内工具提示一致，不自创估算。
        /// </remarks>
        /// <returns>不适用时返回 null：未启用 Biotech / 非人形派系 / <c>def</c> 缺失。</returns>
        public static List<XenotypeChance> CollectMemberComposition(Faction faction)
        {
            if (!ModsConfig.BiotechActive || faction == null)
            {
                return null;
            }
            FactionDef factionDef = faction.def;
            if (factionDef == null || !factionDef.humanlikeFaction)
            {
                return null;
            }

            List<XenotypeChance> result = new List<XenotypeChance>();
            if (factionDef.BaselinerChance > 0f)
            {
                result.Add(new XenotypeChance(XenotypeDefOf.Baseliner, factionDef.BaselinerChance));
            }
            if (factionDef.xenotypeSet != null)
            {
                for (int i = 0; i < factionDef.xenotypeSet.Count; i++)
                {
                    XenotypeChance item = factionDef.xenotypeSet[i];
                    if (item != null && item.xenotype != null && item.xenotype != XenotypeDefOf.Baseliner)
                    {
                        result.Add(item);
                    }
                }
            }

            result.Sort(CompareXenotypeChanceDescending);
            return result;
        }

        /// <summary>按概率降序排列（用于成员构成展示）。</summary>
        private static int CompareXenotypeChanceDescending(XenotypeChance a, XenotypeChance b)
        {
            return b.chance.CompareTo(a.chance);
        }

        /// <summary>
        /// 找出派系的**特征异种人**：成员构成中占比最高的**非基础异种人**，
        /// 且其占比**严格高于**基础异种人（智人种）的占比。
        /// </summary>
        /// <returns>
        /// 符合条件时返回该 <see cref="XenotypeDef"/>；否则返回 null，含义分别为：
        /// 未启用 Biotech / 该派系不以某异种人为主 / 占比并列最大（无唯一主流）/ 数据不可读。
        /// </returns>
        /// <remarks>
        /// 数据优先读 <c>FactionDef.xenotypeSet</c>。该字段为 null 时，退用兵种级
        /// <c>PawnKindDef.xenotypeSet</c>（见 <see cref="CollectKindLevelXenotypes"/>）：
        /// 其非基础异种人**唯一**时认定为主流，多个则**不下任何结论**（既不写内容段，也不并入标签），
        /// 免得武断声称「全是智人种」或错认特征。
        /// </remarks>
        public static XenotypeDef FindDominantXenotype(Faction faction)
        {
            if (!ModsConfig.BiotechActive || faction == null || faction.def == null)
            {
                return null;
            }

            FactionDef factionDef = faction.def;
            XenotypeSet xenotypeSet = factionDef.xenotypeSet;
            if (xenotypeSet == null)
            {
                // 派系级未声明：退用兵种级集合，且只在其非基础异种人唯一时才认定为主流
                //（多种异种人分散在不同兵种里时，无从判断谁是「特征」，宁可不下结论）
                List<XenotypeDef> kindLevel = CollectKindLevelXenotypes(faction);
                return kindLevel.Count == 1 ? kindLevel[0] : null;
            }

            XenotypeDef best = null;
            float bestChance = 0f;
            bool tied = false;

            for (int i = 0; i < xenotypeSet.Count; i++)
            {
                XenotypeChance item = xenotypeSet[i];
                if (item == null || item.xenotype == null)
                {
                    continue;
                }
                // 基础异种人单独参与比较（它的占比是 BaselinerChance，不一定出现在集合里）
                if (item.xenotype == XenotypeDefOf.Baseliner)
                {
                    continue;
                }

                if (item.chance > bestChance)
                {
                    best = item.xenotype;
                    bestChance = item.chance;
                    tied = false;
                }
                else if (item.chance == bestChance)
                {
                    tied = true;
                }
            }

            if (best == null || tied)
            {
                return null;
            }
            if (bestChance <= factionDef.BaselinerChance)
            {
                return null;
            }
            return best;
        }

        /// <summary>
        /// 派系级「成员异种人概率」（<c>CollectMemberComposition</c> 的结果）是否**不可信**：
        /// 为真时调用方必须跳过「成员构成」段，不得照派系级数据写出「成员构成：智人种 100%」。
        /// </summary>
        /// <remarks>
        /// 只有一种情形：派系级 <c>xenotypeSet</c> 为 null，但兵种级另有 <c>kind.xenotypeSet</c>
        /// —— 成员异种人只写在兵种上的派系（如混编异种人与机械族的 mod 派系）。
        /// 此时 <c>FactionDef.BaselinerChance</c> 恒为 <c>1f</c>，照抄即误报「智人种 100%」。
        /// <para>
        /// **不**把「存在 <c>useFactionXenotypes == false</c> 的覆盖型兵种」也算作不可信：
        /// 那类兵种只代表个别特殊兵种（如帝国的贵族 / 奴隶兵种、吸血种兵种），
        /// 派系级分布对**一般成员**依然成立——帝国派系级就明确声明了
        /// 智人种 67.5% + 骠骑种 15% + 智灵种 10% + 尼安德特人 5% + 星跃种 2.5%。
        /// 若据个别兵种整段跳过，会误伤帝国这类原版派系。
        /// </para>
        /// </remarks>
        public static bool IsFactionCompositionUnreliable(Faction faction)
        {
            return faction != null && IsFactionCompositionUnreliable(faction.def);
        }

        /// <summary>
        /// 派系 Def 的「成员异种人概率」是否**不可信**（口径同 <see cref="IsFactionCompositionUnreliable(Faction)"/>，
        /// 供派系界面补丁这类只有 <see cref="FactionDef"/> 的场景使用）。
        /// </summary>
        /// <param name="def">目标派系 Def。</param>
        public static bool IsFactionCompositionUnreliable(FactionDef def)
        {
            if (!ModsConfig.BiotechActive || def == null)
            {
                return false;
            }
            // 派系级已声明分布 → 可信（个别兵种的覆盖不改变一般成员的构成口径）
            if (def.xenotypeSet != null)
            {
                return false;
            }
            // 派系级未声明：仅当兵种级另有声明时，「智人种 100%」才是假象
            return AnyKindMatching(def, KindsWithXenotypeSet);
        }

        /// <summary>该兵种自带异种人设置（<c>kind.xenotypeSet != null</c>）。</summary>
        private static bool KindsWithXenotypeSet(PawnKindDef kind)
        {
            return kind.xenotypeSet != null;
        }

        /// <summary>
        /// 遍历该派系 Def 的 <c>pawnGroupMakers</c> 中的全部兵种，判断是否有任一兵种满足 <paramref name="predicate"/>。
        /// 无 <c>pawnGroupMakers</c>（部分隐藏派系）时返回假。
        /// </summary>
        /// <param name="def">目标派系 Def。</param>
        /// <param name="predicate">兵种判定。</param>
        private static bool AnyKindMatching(FactionDef def, Func<PawnKindDef, bool> predicate)
        {
            List<PawnGroupMaker> groupMakers = def != null ? def.pawnGroupMakers : null;
            if (groupMakers == null)
            {
                return false;
            }

            for (int i = 0; i < groupMakers.Count; i++)
            {
                PawnGroupMaker groupMaker = groupMakers[i];
                if (groupMaker == null || groupMaker.options == null)
                {
                    continue;
                }
                for (int j = 0; j < groupMaker.options.Count; j++)
                {
                    PawnGenOption option = groupMaker.options[j];
                    PawnKindDef kind = option != null ? option.kind : null;
                    if (kind != null && predicate(kind))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // ==================== 异种人常识条目 ====================

        /// <summary>
        /// 为单个异种人构建一条常识：<c>tag</c> = 异种人名（<c>LabelCap</c>），内容含「是什么 / 标志性基因 / 出没派系」三段。
        /// </summary>
        /// <param name="xenotype">目标异种人。</param>
        /// <param name="factionNames">该异种人出没的派系名（反向索引结果），可为空。</param>
        /// <param name="suppressDescription">
        /// 为真时**省略 ① 段的定义原文**、退用只留异种人名的模板
        /// （社区常识库已覆盖该异种人，社区条目已是其描述的改写；避免重复介绍）。
        /// </param>
        /// <param name="tag">输出：触发标签。</param>
        /// <param name="content">输出：单行注入内容。</param>
        public static bool TryBuild(
            XenotypeDef xenotype, List<string> factionNames, bool suppressDescription,
            out string tag, out string content)
        {
            tag = null;
            content = null;

            if (xenotype == null)
            {
                return false;
            }

            // LabelCap 是 TaggedString，拼接/格式化时隐式转为文本
            string label = xenotype.LabelCap;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            tag = label;
            StringBuilder builder = new StringBuilder();

            // ① 是什么（description 优先，为空则退用 descriptionShort；社区已覆盖该 Def 时整段退为只留名字）
            string description = suppressDescription
                ? null
                : ToSingleLine(!string.IsNullOrEmpty(xenotype.description)
                    ? xenotype.description
                    : xenotype.descriptionShort);
            builder.Append(string.IsNullOrEmpty(description)
                ? string.Format(FactionKnowledgeConfig.XENOTYPE_SEG_INTRO_BARE, label)
                : string.Format(FactionKnowledgeConfig.XENOTYPE_SEG_INTRO, label, description));

            // ② 标志性基因（无基因 → 固定文案；有基因 → 按游戏展示顺序取前 N）
            AppendGeneSection(builder, xenotype);

            // ③ 出没派系（属于双向引用，见 FR-7 / D13：关闭后本段与派系 ⑤ 段一并缺席）
            if (FactionInfoSettings.IncludeFactionXenotypeRefs &&
                factionNames != null && factionNames.Count > 0)
            {
                builder.Append(string.Format(
                    FactionKnowledgeConfig.XENOTYPE_SEG_FACTIONS,
                    string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, factionNames.ToArray())));
            }

            content = builder.ToString();
            return true;
        }

        /// <summary>
        /// 追加②「标志性基因」段：<c>genes</c> 为空 → 固定文案；否则按游戏自身展示顺序取前 N 个。
        /// N 取设置项 <c>xenotypeGeneCount</c>（默认 4），为 0 时整段跳过。
        /// </summary>
        private static void AppendGeneSection(StringBuilder builder, XenotypeDef xenotype)
        {
            int geneCount = FactionInfoSettings.XenotypeGeneCount;
            if (geneCount <= 0)
            {
                return;
            }

            List<string> labels = PickSignatureGenes(xenotype, geneCount);
            if (labels.Count == 0)
            {
                // 真正没有基因（基础异种人即此情况）
                builder.Append(FactionKnowledgeConfig.XENOTYPE_NO_GENES);
                return;
            }

            builder.Append(string.Format(
                FactionKnowledgeConfig.XENOTYPE_SEG_GENES,
                string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, labels.ToArray())));
        }

        /// <summary>
        /// 挑选「标志性基因」的名称列表，口径为游戏自身的展示顺序（不自创排序）：
        /// 候选 = <c>genes</c> 中 <c>displayCategory != GeneCategoryDefOf.Miscellaneous</c> 者；
        /// 排序键 = <c>displayCategory.displayPriorityInXenotype</c> 降序 → <c>displayOrderInCategory</c> 升序。
        /// </summary>
        /// <remarks>
        /// 若过滤后候选为空（该异种人只有外观类杂项基因），退回全集，避免整段丢失。
        /// </remarks>
        private static List<string> PickSignatureGenes(XenotypeDef xenotype, int count)
        {
            List<string> labels = new List<string>();
            if (xenotype.genes == null || xenotype.genes.Count == 0)
            {
                return labels;
            }

            List<GeneDef> candidates = new List<GeneDef>();
            for (int i = 0; i < xenotype.genes.Count; i++)
            {
                GeneDef gene = xenotype.genes[i];
                if (gene != null && gene.displayCategory != GeneCategoryDefOf.Miscellaneous)
                {
                    candidates.Add(gene);
                }
            }
            if (candidates.Count == 0)
            {
                for (int i = 0; i < xenotype.genes.Count; i++)
                {
                    if (xenotype.genes[i] != null)
                    {
                        candidates.Add(xenotype.genes[i]);
                    }
                }
            }

            candidates.Sort(CompareGenesByDisplayOrder);
            int take = candidates.Count < count ? candidates.Count : count;
            for (int i = 0; i < take; i++)
            {
                labels.Add(candidates[i].LabelCap);
            }
            return labels;
        }

        /// <summary>基因展示顺序：类别优先级降序 → 类内顺序升序。</summary>
        private static int CompareGenesByDisplayOrder(GeneDef a, GeneDef b)
        {
            float priorityA = a.displayCategory != null ? a.displayCategory.displayPriorityInXenotype : 0f;
            float priorityB = b.displayCategory != null ? b.displayCategory.displayPriorityInXenotype : 0f;
            if (priorityA != priorityB)
            {
                return priorityB.CompareTo(priorityA);
            }
            return a.displayOrderInCategory.CompareTo(b.displayOrderInCategory);
        }

        /// <summary>把多行文本压成单行：换行（含 Windows 的 <c>\r</c>）一律替换为空格。</summary>
        private static string ToSingleLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
