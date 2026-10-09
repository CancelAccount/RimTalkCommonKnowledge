using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimTalkCommonKnowledge
{
        /// <summary>本文件为 Dialog_KnowledgeManager 的 partial 部分（D65 拆分）：条目新增 / 编辑入口与导入 / 导出等写操作。</summary>
        public partial class Dialog_KnowledgeManager
        {
        // ---------- 条目写操作（FR-18~FR-20） ----------

        /// <summary>打开「新增条目」对话框；保存成功后重建本页快照与列表。</summary>
        private void OpenNewEntry()
        {
            Find.WindowStack.Add(new Dialog_EditKnowledgeEntry(AfterEntryChanged));
        }

        /// <summary>打开「编辑选中条目」对话框；仅在恰好勾选一条时可触发。</summary>
        private void OpenEditEntry()
        {
            if (selectedIds.Count != 1)
            {
                return;
            }

            KnowledgeEntrySnapshot target = null;
            foreach (string id in selectedIds)
            {
                byId.TryGetValue(id, out target);
                break;
            }
            if (target == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_EditKnowledgeEntry(target, AfterEntryChanged));
        }

        /// <summary>条目新增 / 编辑保存成功后的刷新动作：重建快照、索引与列表。</summary>
        private void AfterEntryChanged()
        {
            Rebuild();
        }

        /// <summary>
        /// 导出条目到系统剪贴板（D59）：有勾选项时导出<b>全部勾选项</b>（跨导航归集口径，同删除），
        /// 否则导出当前列表全部条目；导出后以屏幕消息告知条数。
        /// </summary>
        private void ExportEntries()
        {
            // 确定导出 id 范围：勾选优先，否则当前列表
            List<string> ids = new List<string>();
            if (selectedIds.Count > 0)
            {
                ids.AddRange(selectedIds);
            }
            else if (cachedList != null)
            {
                for (int i = 0; i < cachedList.Count; i++)
                {
                    KnowledgeEntrySnapshot entry = cachedList[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.Id))
                    {
                        ids.Add(entry.Id);
                    }
                }
            }

            string text = RimTalkMemoryBridge.ExportEntriesToText(ids);
            if (string.IsNullOrEmpty(text))
            {
                Messages.Message(
                    FactionKnowledgeConfig.MANAGER_EXPORT_EMPTY,
                    MessageTypeDefOf.SilentInput);
                return;
            }

            GUIUtility.systemCopyBuffer = text;

            // 桥接层对每条有效条目写一行（AppendLine 以 LF 结尾），按换行数即实际导出条数
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }

            Messages.Message(
                string.Format(FactionKnowledgeConfig.MANAGER_EXPORT_RESULT_FORMAT, count),
                MessageTypeDefOf.SilentInput);
        }

        /// <summary>
        /// 打开导入对话框：先取库内现有「标签 + 正文」键集合作为去重基准；
        /// 同时快照<b>当前库内全部 id</b>与<b>当前选中的自定义子页</b>——
        /// 导入成功后据此识别新条目，若打开时选中了子页则把新条目一并录入（D63）。
        /// 对话框为模态，期间玩家无法切换导航，故目标页在此捕获即可。
        /// </summary>
        private void OpenImportDialog()
        {
            HashSet<string> existingKeys = RimTalkMemoryBridge.GetExistingTagContentKeys();

            // 导入前库内全部 id（用于导入后求差集，得到本次新增 id）
            HashSet<string> previousIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < allEntries.Count; i++)
            {
                KnowledgeEntrySnapshot entry = allEntries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Id))
                {
                    previousIds.Add(entry.Id);
                }
            }

            KnowledgeSubPage targetPage = CurrentPage;
            Find.WindowStack.Add(new Dialog_ImportKnowledge(
                existingKeys, () => OnImportFinished(previousIds, targetPage)));
        }

        /// <summary>
        /// 导入成功后的处理（替代普通 <see cref="AfterEntryChanged"/> 用于导入场景）：
        /// 先重建全量快照，再把本次<b>新增</b>条目录入选定子页。
        /// </summary>
        /// <param name="previousIds">打开导入框时库内已有的全部 id。</param>
        /// <param name="targetPage">打开导入框时选中的自定义子页；为 <c>null</c> 表示无子页、只重建。</param>
        private void OnImportFinished(HashSet<string> previousIds, KnowledgeSubPage targetPage)
        {
            Rebuild();

            // 未选中子页：保持 D59 原行为，仅重建
            if (targetPage == null)
            {
                return;
            }
            if (targetPage.entryIds == null)
            {
                targetPage.entryIds = new List<string>();
            }

            // 差集 = 导入后出现、导入前不存在的 id，即本次新导入条目
            int added = 0;
            for (int i = 0; i < allEntries.Count; i++)
            {
                string id = allEntries[i].Id;
                if (string.IsNullOrEmpty(id) || previousIds.Contains(id))
                {
                    continue;
                }
                if (!targetPage.entryIds.Contains(id))
                {
                    targetPage.entryIds.Add(id);
                    added++;
                }
            }

            if (added > 0)
            {
                // Rebuild 已置 listDirty；此处再显式标记，保证当前子页列表与导航计数立即刷新
                listDirty = true;
                Messages.Message(
                    string.Format(
                        FactionKnowledgeConfig.IMPORTER_ADDED_TO_PAGE_FORMAT,
                        added, targetPage.name),
                    MessageTypeDefOf.SilentInput);
            }
        }
    }
}
