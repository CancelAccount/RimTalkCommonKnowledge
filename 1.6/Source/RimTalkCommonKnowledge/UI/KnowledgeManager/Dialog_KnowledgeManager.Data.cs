using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimTalkCommonKnowledge
{
        /// <summary>本文件为 Dialog_KnowledgeManager 的 partial 部分（D65 拆分）：全量快照 / 两级索引的重建、可见块计算与条目来源分类判定。</summary>
        public partial class Dialog_KnowledgeManager
        {
        // ==================== 数据 ====================

        /// <summary>重建全量快照与两级索引，并清理已失效的勾选与子页选中态。</summary>
        private void Rebuild()
        {
            allEntries = RimTalkMemoryBridge.IsLibraryAvailable
                ? RimTalkMemoryBridge.GetAllSnapshots()
                : new List<KnowledgeEntrySnapshot>();

            byId.Clear();
            byKey.Clear();
            for (int i = 0; i < allEntries.Count; i++)
            {
                KnowledgeEntrySnapshot entry = allEntries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Id))
                {
                    continue;
                }

                byId[entry.Id] = entry;

                string key = RimTalkMemoryBridge.MakeTagContentKey(entry.Tag, entry.Content);
                List<KnowledgeEntrySnapshot> bucket;
                if (!byKey.TryGetValue(key, out bucket))
                {
                    bucket = new List<KnowledgeEntrySnapshot>();
                    byKey[key] = bucket;
                }
                bucket.Add(entry);
            }

            // 条目被删掉后，勾选集合里可能残留无效 id
            selectedIds.RemoveWhere(id => !byId.ContainsKey(id));

            // 来源索引是首次访问才扫描（可能读盘），提前在此触发，避免绘制中途卡一下
            KnowledgeSourceIndex.EnsureBuilt();

            // 依赖上面的 byKey 与来源索引，故必须排在其后
            RebuildVisibleBlocks();
            RecountOwnEntries();

            ValidatePageIndex();
            listDirty = true;
        }

        /// <summary>
        /// 统计库内本 mod 两类注入条目数，供导航决定是否显示「派系信息 / 异种人信息」子行
        /// （库内为 0 的一类不建空节点，与可见块 / 模组的剔除口径一致）。
        /// </summary>
        private void RecountOwnEntries()
        {
            factionEntryCount = 0;
            xenotypeEntryCount = 0;

            for (int i = 0; i < allEntries.Count; i++)
            {
                if (IsFactionEntry(allEntries[i]))
                {
                    factionEntryCount++;
                }
                else if (IsXenotypeEntry(allEntries[i]))
                {
                    xenotypeEntryCount++;
                }
            }
        }

        /// <summary>校正当前子页下标：列表为空或越界时回到「全部条目」。</summary>
        private void ValidatePageIndex()
        {
            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages == null || pages.Count == 0 || navPageIndex >= pages.Count)
            {
                navPageIndex = -1;
            }
            if (navMode == NavMode.Custom && navPageIndex < 0)
            {
                navMode = NavMode.All;
            }
        }

        /// <summary>
        /// 重建「预设库」导航的可见块 / 模组：只保留在库内**确有條目命中**的模组（及其所在块）。
        /// 未订阅 / 未启用的 mod 条目不会被导入，库内自然没有它们，此处随之剔除，
        /// 免得左侧列出一个点进去空白的子页。
        /// </summary>
        private void RebuildVisibleBlocks()
        {
            visibleBlocks.Clear();

            IReadOnlyList<KnowledgeSourceIndex.BlockGroup> blocks = KnowledgeSourceIndex.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                KnowledgeSourceIndex.BlockGroup block = blocks[i];
                KnowledgeSourceIndex.BlockGroup visibleBlock = null;
                for (int j = 0; j < block.Mods.Count; j++)
                {
                    KnowledgeSourceIndex.ModGroup mod = block.Mods[j];
                    if (!HasAnyEntry(mod))
                    {
                        continue;
                    }
                    if (visibleBlock == null)
                    {
                        visibleBlock = new KnowledgeSourceIndex.BlockGroup { Name = block.Name };
                    }
                    visibleBlock.Mods.Add(mod);
                }
                if (visibleBlock != null)
                {
                    visibleBlocks.Add(visibleBlock);
                }
            }

            // 导航目标若落在已不可见的块 / 模组上（如刷新后订阅变化），回退到「全部」
            if (navMode == NavMode.Block || navMode == NavMode.Mod)
            {
                bool visible = false;
                for (int i = 0; i < visibleBlocks.Count; i++)
                {
                    if (string.Equals(visibleBlocks[i].Name, navBlock, StringComparison.Ordinal))
                    {
                        visible = true;
                        break;
                    }
                }
                if (!visible)
                {
                    navMode = NavMode.All;
                    navBlock = null;
                    navMod = null;
                }
            }
        }

        /// <summary>模组行是否有任一条目的「标签 + 内容」键命中当前库。</summary>
        /// <param name="mod">来源索引中的一个模组行。</param>
        private bool HasAnyEntry(KnowledgeSourceIndex.ModGroup mod)
        {
            for (int i = 0; i < mod.Keys.Count; i++)
            {
                if (byKey.ContainsKey(mod.Keys[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>当前选中的自定义子页；未选中或无有效下标时返回 <c>null</c>。</summary>
        private KnowledgeSubPage CurrentPage
        {
            get
            {
                List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
                if (pages == null || navPageIndex < 0 || navPageIndex >= pages.Count)
                {
                    return null;
                }
                return pages[navPageIndex];
            }
        }

        /// <summary>条目是否由本 mod 注入（内容带任一本 mod 内容前缀）。</summary>
        private static bool IsOwnEntry(KnowledgeEntrySnapshot entry)
        {
            return IsFactionEntry(entry) || IsXenotypeEntry(entry);
        }

        /// <summary>条目是否为本 mod 注入的派系信息（内容以派系前缀开头）。</summary>
        /// <param name="entry">待判定条目。</param>
        private static bool IsFactionEntry(KnowledgeEntrySnapshot entry)
        {
            return entry != null
                && !string.IsNullOrEmpty(entry.Content)
                && entry.Content.StartsWith(
                    FactionKnowledgeConfig.FACTION_CONTENT_PREFIX, StringComparison.Ordinal);
        }

        /// <summary>条目是否为本 mod 注入的异种人信息（内容以异种人前缀开头）。</summary>
        /// <param name="entry">待判定条目。</param>
        private static bool IsXenotypeEntry(KnowledgeEntrySnapshot entry)
        {
            return entry != null
                && !string.IsNullOrEmpty(entry.Content)
                && entry.Content.StartsWith(
                    FactionKnowledgeConfig.XENOTYPE_CONTENT_PREFIX, StringComparison.Ordinal);
        }

        /// <summary>条目是否来自随包预设库（能在来源索引里反查到块 / 模组）。</summary>
        private static bool IsPresetEntry(KnowledgeEntrySnapshot entry)
        {
            if (entry == null)
            {
                return false;
            }
            string block;
            string mod;
            return KnowledgeSourceIndex.TryGetSource(entry.Tag, entry.Content, out block, out mod);
        }

    }
}
