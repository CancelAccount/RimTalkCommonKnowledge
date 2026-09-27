using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 异种人（Xenotype）相关的取值与文案构建（FR-9）。
    /// 所有入口在未启用 Biotech 时一律返回空/假，不做任何 <c>XenotypeDef</c> 访问（证据 ㉕：未启用时
    /// <c>XenotypeDefOf.Baseliner</c> 等为 null，直接访问会 NRE）。
    /// </summary>
    public static class XenotypeKnowledgeBuilder
    {
        // ==================== 集合来源（FR-9.1 / D12） ====================

        /// <summary>
        /// 该派系**显式声明**的相关异种人（去重）：<c>FactionDef.xenotypeSet</c> ∪ 主理念 <c>memes[].xenotypeSet</c>。
        /// </summary>
        /// <remarks>
        /// 不含「基础异种人补差」——本方法用于建立「异种人 → 出没派系」的反向索引（FR-9.2 ③），
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

            // 主理念可追加异种人倾向，游戏在生成 pawn 时会把它们累加进分布（证据 ㉕）
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
            return result;
        }

        /// <summary>
        /// 该派系需要在常识库里**单独立条**的异种人（D12）= 显式集合 ∪ 基础异种人（仅当 <c>BaselinerChance &gt; 0</c>）。
        /// </summary>
        public static List<XenotypeDef> CollectEntryXenotypes(Faction faction)
        {
            List<XenotypeDef> result = CollectExplicitXenotypes(faction);
            if (!ModsConfig.BiotechActive || faction == null || faction.def == null)
            {
                return result;
            }

            // 基础异种人不在 xenotypeSet 里体现，其占比由 BaselinerChance 单独给出（证据 ㉕）
            if (faction.def.BaselinerChance > 0f && !result.Contains(XenotypeDefOf.Baseliner))
            {
                result.Add(XenotypeDefOf.Baseliner);
            }
            return result;
        }

        /// <summary>
        /// 合并多个派系「需单独立条」的异种人集合并去重（FR-9.1：世界级异种人集合 = 各有效派系的并集）。
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

        /// <summary>把 <paramref name="set"/> 中的异种人并入列表，跳过空项与重复项。</summary>
        private static void AppendXenotypes(List<XenotypeDef> result, XenotypeSet set)
        {
            if (set == null)
            {
                return;
            }
            // 底层 xenotypeChances 是 private，外部只能用 Count + 索引器遍历（证据 ㉕）
            for (int i = 0; i < set.Count; i++)
            {
                XenotypeChance item = set[i];
                if (item == null || item.xenotype == null)
                {
                    continue;
                }
                if (!result.Contains(item.xenotype))
                {
                    result.Add(item.xenotype);
                }
            }
        }

        // ==================== 成员构成与特征异种人（FR-3 ⑤ / D17） ====================

        /// <summary>
        /// 按**游戏自身口径**汇总「成员异种人概率」：<c>BaselinerChance</c>（&gt; 0 时）+
        /// <c>FactionDef.xenotypeSet</c>（排除基础异种人），按概率降序。
        /// </summary>
        /// <remarks>
        /// 算法与 <c>FactionDef.Description</c> 属性的「成员异种人概率」段**完全同源**（证据 ㉕），
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
        /// 且其占比**严格高于**基础异种人（智人种）的占比（D17）。
        /// </summary>
        /// <returns>
        /// 符合条件时返回该 <see cref="XenotypeDef"/>；否则返回 null，含义分别为：
        /// 未启用 Biotech / 该派系不以某异种人为主 / 占比并列最大（无唯一主流）/ 数据不可读。
        /// </returns>
        /// <remarks>
        /// 数据只读 <c>FactionDef.xenotypeSet</c>（证据 ㉕）。该字段为 null 时直接返回 null —— 这同时
        /// 落实了 RK-9 护栏：若派系改用 <c>PawnKindDef.xenotypeSet</c> 指定异种人，我们读不到，
        /// 此时**不下任何结论**（既不写内容段，也不并入标签），免得武断声称「全是智人种」。
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
                return null;
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
        /// 该派系是否存在「兵种级异种人设置」（RK-9）：存在 <c>kind.xenotypeSet != null</c> 且
        /// 该兵种 <c>useFactionXenotypes</c> 为假的兵种时为真。
        /// </summary>
        /// <remarks>
        /// 用于 RK-9 护栏：此时派系级 <c>xenotypeSet</c> 读不到真实分布，内容里的「成员构成」段必须整段跳过，
        /// 否则会武断写出「成员全是智人种」。兵种级覆盖是游戏内的局部特例（如帝国特殊兵种、吸血种兵种）。
        /// </remarks>
        public static bool HasKindLevelXenotypeOverride(Faction faction)
        {
            if (!ModsConfig.BiotechActive || faction == null || faction.def == null)
            {
                return false;
            }

            List<PawnGroupMaker> groupMakers = faction.def.pawnGroupMakers;
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
                    if (kind != null && kind.xenotypeSet != null && !kind.useFactionXenotypes)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // ==================== 异种人常识条目（FR-9.2） ====================

        /// <summary>
        /// 为单个异种人构建一条常识：<c>tag</c> = 异种人名（<c>LabelCap</c>），内容含「是什么 / 标志性基因 / 出没派系」三段。
        /// </summary>
        /// <param name="xenotype">目标异种人。</param>
        /// <param name="factionNames">该异种人出没的派系名（FR-9.2 ③ 的反向索引结果），可为空。</param>
        /// <param name="tag">输出：触发标签。</param>
        /// <param name="content">输出：单行注入内容。</param>
        public static bool TryBuild(XenotypeDef xenotype, List<string> factionNames, out string tag, out string content)
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

            // ① 是什么（description 优先，为空则退用 descriptionShort）
            string description = !string.IsNullOrEmpty(xenotype.description)
                ? xenotype.description
                : xenotype.descriptionShort;
            description = ToSingleLine(description);
            builder.Append(string.IsNullOrEmpty(description)
                ? string.Format(FactionKnowledgeConfig.XENOTYPE_SEG_INTRO_BARE, label)
                : string.Format(FactionKnowledgeConfig.XENOTYPE_SEG_INTRO, label, description));

            // ② 标志性基因（无基因 → 固定文案；有基因 → 按游戏展示顺序取前 N）
            AppendGeneSection(builder, xenotype);

            // ③ 出没派系
            if (factionNames != null && factionNames.Count > 0)
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
        /// <c>DEFAULT_XENOTYPE_GENE_COUNT</c> 为 0 时整段跳过。
        /// </summary>
        private static void AppendGeneSection(StringBuilder builder, XenotypeDef xenotype)
        {
            int geneCount = FactionKnowledgeConfig.DEFAULT_XENOTYPE_GENE_COUNT;
            if (geneCount <= 0)
            {
                return;
            }

            List<string> labels = PickSignatureGenes(xenotype, geneCount);
            if (labels.Count == 0)
            {
                // 真正没有基因（基础异种人即此情况，证据 ㉕）
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

        /// <summary>把多行文本压成单行：换行（含 Windows 的 <c>\r</c>）一律替换为空格（RK-4）。</summary>
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
