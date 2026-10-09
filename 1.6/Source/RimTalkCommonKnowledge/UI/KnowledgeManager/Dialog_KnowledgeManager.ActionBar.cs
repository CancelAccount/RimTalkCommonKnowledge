using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimTalkCommonKnowledge
{
        /// <summary>本文件为 Dialog_KnowledgeManager 的 partial 部分（D65 拆分）：底部操作栏、内联重命名 / 删除确认条，以及勾选、子页归集、批量启停等操作。</summary>
        public partial class Dialog_KnowledgeManager
        {
        // ==================== 底部操作栏 ====================

        /// <summary>绘制底部操作栏；处于重命名 / 删除确认（子页或条目）时改绘对应的内联条。</summary>
        /// <param name="rect">操作栏区域。</param>
        private void DrawActionBar(Rect rect)
        {
            if (renamingPage)
            {
                DrawRenameBar(rect);
                return;
            }
            if (confirmingDeletePage)
            {
                DrawDeleteConfirmBar(rect);
                return;
            }
            if (confirmingDeleteEntries)
            {
                DrawDeleteEntriesConfirmBar(rect);
                return;
            }

            float width = FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH;
            float height = FactionKnowledgeConfig.MANAGER_ACTION_ROW_HEIGHT;
            float gap = FactionKnowledgeConfig.MANAGER_GAP;
            // 第一 / 第二行都要用到勾选态：提前声明
            bool hasSelection = selectedIds.Count > 0;

            // 第一行：勾选操作 + 批量启用 / 停用（D64）
            float x = rect.x;
            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_SELECT_ALL))
            {
                SelectAllListed();
            }
            x += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_CLEAR_SELECTION))
            {
                selectedIds.Clear();
            }
            x += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_REFRESH))
            {
                KnowledgeSourceIndex.Reset();
                Rebuild();
            }
            x += width + gap;

            // 批量启用 / 停用：作用于全部勾选项；无勾选时置灰（操作可逆，无需二次确认）
            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height),
                    FactionKnowledgeConfig.MANAGER_BATCH_ENABLE, true, true, hasSelection))
            {
                BatchSetEnabled(true);
            }
            x += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height),
                    FactionKnowledgeConfig.MANAGER_BATCH_DISABLE, true, true, hasSelection))
            {
                BatchSetEnabled(false);
            }

            // 两个批量按钮无勾选时的置灰原因（位置：第 4 / 第 5 个）
            if (!hasSelection)
            {
                TooltipHandler.TipRegion(
                    new Rect(rect.x + (width + gap) * 3f, rect.y, width, height),
                    FactionKnowledgeConfig.MANAGER_NO_SELECTION_TIP);
                TooltipHandler.TipRegion(
                    new Rect(rect.x + (width + gap) * 4f, rect.y, width, height),
                    FactionKnowledgeConfig.MANAGER_NO_SELECTION_TIP);
            }

            // 第二行：条目操作（新增 / 编辑 / 删除，FR-18~FR-20）
            float secondY = rect.y + height + gap;
            bool exactlyOne = selectedIds.Count == 1;
            float xEntry = rect.x;

            if (Widgets.ButtonText(
                    new Rect(xEntry, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_NEW_ENTRY))
            {
                OpenNewEntry();
            }
            xEntry += width + gap;

            if (Widgets.ButtonText(
                    new Rect(xEntry, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_EDIT_ENTRY, true, true, exactlyOne))
            {
                OpenEditEntry();
            }
            xEntry += width + gap;

            if (Widgets.ButtonText(
                    new Rect(xEntry, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_DELETE_ENTRIES, true, true, hasSelection))
            {
                confirmingDeleteEntries = true;
            }
            xEntry += width + gap;

            // 导入 / 导出（D59；D61 起由第四行并入本行，与条目操作同排）
            if (Widgets.ButtonText(
                    new Rect(xEntry, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_IMPORT_ENTRIES))
            {
                OpenImportDialog();
            }
            xEntry += width + gap;

            if (Widgets.ButtonText(
                    new Rect(xEntry, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_EXPORT_ENTRIES))
            {
                ExportEntries();
            }

            // 置灰原因提示（复用第二行区域）
            if (!exactlyOne)
            {
                TooltipHandler.TipRegion(
                    new Rect(rect.x + width + gap, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_EDIT_SINGLE_TIP);
            }
            if (!hasSelection)
            {
                TooltipHandler.TipRegion(
                    new Rect(rect.x + (width + gap) * 2f, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_NO_SELECTION_TIP);
            }

            // 导入 / 导出悬停说明（与按钮同排，按第 4 / 第 5 个位置定位）
            TooltipHandler.TipRegion(
                new Rect(rect.x + (width + gap) * 3f, secondY, width, height),
                FactionKnowledgeConfig.MANAGER_IMPORT_TIP);
            TooltipHandler.TipRegion(
                new Rect(rect.x + (width + gap) * 4f, secondY, width, height),
                FactionKnowledgeConfig.MANAGER_EXPORT_TIP);

            // 第三行：子页操作（无选中子页时置灰）
            float thirdY = secondY + height + gap;
            KnowledgeSubPage currentPage = CurrentPage;
            bool hasPage = currentPage != null;
            float x2 = rect.x;

            if (Widgets.ButtonText(
                    new Rect(x2, thirdY, width, height),
                    FactionKnowledgeConfig.MANAGER_ADD_TO_PAGE, true, true, hasPage))
            {
                AddSelectionToPage();
            }
            x2 += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x2, thirdY, width, height),
                    FactionKnowledgeConfig.MANAGER_REMOVE_FROM_PAGE, true, true, hasPage))
            {
                RemoveSelectionFromPage();
            }
            x2 += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x2, thirdY, width, height),
                    FactionKnowledgeConfig.MANAGER_RENAME_PAGE, true, true, hasPage))
            {
                renamingPage = true;
                renameBuffer = currentPage.name;
            }
            x2 += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x2, thirdY, width, height),
                    FactionKnowledgeConfig.MANAGER_DELETE_PAGE, true, true, hasPage))
            {
                confirmingDeletePage = true;
            }

            if (!hasPage)
            {
                TooltipHandler.TipRegion(
                    new Rect(rect.x, thirdY, rect.width, height),
                    FactionKnowledgeConfig.MANAGER_NO_PAGE_TIP);
            }
        }

        /// <summary>重命名内联条：输入框 + 确定 / 取消。</summary>
        /// <param name="rect">操作栏区域。</param>
        private void DrawRenameBar(Rect rect)
        {
            float height = FactionKnowledgeConfig.MANAGER_ACTION_ROW_HEIGHT;
            float gap = FactionKnowledgeConfig.MANAGER_GAP;
            float width = FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH;

            Rect fieldRect = new Rect(
                rect.x, rect.y, FactionKnowledgeConfig.MANAGER_RENAME_FIELD_WIDTH, height);
            renameBuffer = Widgets.TextField(
                fieldRect,
                renameBuffer ?? string.Empty,
                FactionKnowledgeConfig.MANAGER_PAGE_NAME_MAX_CHARS);

            float x = fieldRect.xMax + gap;
            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_CONFIRM))
            {
                CommitRename();
            }
            x += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_CANCEL))
            {
                renamingPage = false;
                renameBuffer = string.Empty;
            }
        }

        /// <summary>删除确认内联条：提示文本 + 确定 / 取消。</summary>
        /// <param name="rect">操作栏区域。</param>
        private void DrawDeleteConfirmBar(Rect rect)
        {
            float height = FactionKnowledgeConfig.MANAGER_ACTION_ROW_HEIGHT;
            float gap = FactionKnowledgeConfig.MANAGER_GAP;
            float width = FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH;
            float buttonsWidth = width * 2f + gap;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                new Rect(rect.x, rect.y, rect.width - buttonsWidth - gap, height),
                FactionKnowledgeConfig.MANAGER_CONFIRM_DELETE);
            Text.Anchor = previousAnchor;

            float x = rect.xMax - buttonsWidth;
            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_CONFIRM))
            {
                CommitDelete();
            }
            x += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x, rect.y, width, height), FactionKnowledgeConfig.MANAGER_CANCEL))
            {
                confirmingDeletePage = false;
            }
        }

        /// <summary>
        /// 删除条目确认内联条（FR-20 / D54）：确认问题（带条数）+ 本 mod 注入警告 + 确定 / 取消。
        /// </summary>
        /// <param name="rect">操作栏区域（三行高）。</param>
        private void DrawDeleteEntriesConfirmBar(Rect rect)
        {
            float height = FactionKnowledgeConfig.MANAGER_ACTION_ROW_HEIGHT;
            float gap = FactionKnowledgeConfig.MANAGER_GAP;
            float width = FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                new Rect(rect.x, rect.y, rect.width, height),
                string.Format(
                    FactionKnowledgeConfig.MANAGER_CONFIRM_DELETE_ENTRIES_FORMAT, selectedIds.Count));

            // 仅当选区内含本 mod 注入条目时，第二行显示重注警告
            if (SelectionHasOwnEntry)
            {
                Widgets.Label(
                    new Rect(rect.x, rect.y + height, rect.width, height),
                    FactionKnowledgeConfig.MANAGER_DELETE_OWN_WARNING);
            }
            Text.Anchor = previousAnchor;

            // 确定 / 取消置于第三行右侧
            float buttonsWidth = width * 2f + gap;
            float x = rect.xMax - buttonsWidth;
            float buttonsY = rect.yMax - height;
            if (Widgets.ButtonText(
                    new Rect(x, buttonsY, width, height), FactionKnowledgeConfig.MANAGER_CONFIRM))
            {
                CommitDeleteEntries();
            }
            x += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x, buttonsY, width, height), FactionKnowledgeConfig.MANAGER_CANCEL))
            {
                confirmingDeleteEntries = false;
            }
        }

        /// <summary>当前勾选集合中是否含至少一条本 mod 注入条目。</summary>
        private bool SelectionHasOwnEntry
        {
            get
            {
                foreach (string id in selectedIds)
                {
                    KnowledgeEntrySnapshot entry;
                    if (byId.TryGetValue(id, out entry) && IsOwnEntry(entry))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // ==================== 操作 ====================

        /// <summary>
        /// 把某条目设为指定勾选状态（滑动批量选择按笔刷状态写入，单击时取反后的状态）。
        /// </summary>
        /// <param name="id">条目 id。</param>
        /// <param name="selected">目标勾选状态。</param>
        private void SetSelection(string id, bool selected)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }
            if (selected)
            {
                selectedIds.Add(id);
            }
            else
            {
                selectedIds.Remove(id);
            }
        }

        /// <summary>全选当前列表中的条目。</summary>
        private void SelectAllListed()
        {
            EnsureList();
            for (int i = 0; i < cachedList.Count; i++)
            {
                if (!string.IsNullOrEmpty(cachedList[i].Id))
                {
                    selectedIds.Add(cachedList[i].Id);
                }
            }
        }

        /// <summary>
        /// 批量把<b>全部勾选条目</b>设为指定启用状态（D64；勾选集合跨导航归集，与删除同口径）。
        /// 已是目标态的条目跳过；单条写入失败不影响其余条目；成功后直接改快照字段、
        /// 不 Rebuild（启用态不影响导航分类），仅置列表脏标记刷新视觉。
        /// </summary>
        /// <param name="enabled">目标启用状态：true=批量启用，false=批量停用。</param>
        private void BatchSetEnabled(bool enabled)
        {
            int changed = 0;
            foreach (string id in selectedIds)
            {
                if (!byId.TryGetValue(id, out KnowledgeEntrySnapshot entry)
                    || entry == null
                    || entry.IsEnabled == enabled)
                {
                    continue;
                }
                if (RimTalkMemoryBridge.SetEnabled(id, enabled))
                {
                    entry.IsEnabled = enabled;
                    changed++;
                }
            }

            if (changed > 0)
            {
                listDirty = true;
                Messages.Message(
                    string.Format(
                        enabled
                            ? FactionKnowledgeConfig.MANAGER_BATCH_ENABLE_RESULT_FORMAT
                            : FactionKnowledgeConfig.MANAGER_BATCH_DISABLE_RESULT_FORMAT,
                        changed),
                    MessageTypeDefOf.SilentInput);
            }
        }

        /// <summary>新建一个自定义子页，选中它并立即进入重命名，方便直接命名。</summary>
        private void CreatePage()
        {
            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages == null)
            {
                return;
            }

            string name = string.Format(
                FactionKnowledgeConfig.MANAGER_DEFAULT_PAGE_NAME, pages.Count + 1);
            pages.Add(new KnowledgeSubPage(name));

            navMode = NavMode.Custom;
            navPageIndex = pages.Count - 1;
            navBlock = null;
            navMod = null;
            listDirty = true;
            confirmingDeletePage = false;
            renamingPage = true;
            renameBuffer = name;
        }

        /// <summary>提交重命名：名称非空才写入（空名会被设置载入时的脏数据清理丢掉）。</summary>
        private void CommitRename()
        {
            KnowledgeSubPage page = CurrentPage;
            if (page != null)
            {
                string name = renameBuffer == null ? string.Empty : renameBuffer.Trim();
                if (name.Length > 0)
                {
                    page.name = name;
                }
            }
            renamingPage = false;
            renameBuffer = string.Empty;
        }

        /// <summary>提交删除当前子页，并校正导航选中态。</summary>
        private void CommitDelete()
        {
            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages != null && navPageIndex >= 0 && navPageIndex < pages.Count)
            {
                pages.RemoveAt(navPageIndex);
            }
            confirmingDeletePage = false;
            ValidatePageIndex();
            listDirty = true;
        }

        /// <summary>把已勾选条目加入当前子页（按库内顺序追加、去重）。</summary>
        private void AddSelectionToPage()
        {
            KnowledgeSubPage page = CurrentPage;
            if (page == null)
            {
                return;
            }
            if (page.entryIds == null)
            {
                page.entryIds = new List<string>();
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                string id = allEntries[i].Id;
                if (string.IsNullOrEmpty(id) || !selectedIds.Contains(id))
                {
                    continue;
                }
                if (!page.entryIds.Contains(id))
                {
                    page.entryIds.Add(id);
                }
            }
            listDirty = true;
        }

        /// <summary>把已勾选条目从当前子页移出。</summary>
        private void RemoveSelectionFromPage()
        {
            KnowledgeSubPage page = CurrentPage;
            if (page == null || page.entryIds == null)
            {
                return;
            }
            page.entryIds.RemoveAll(id => selectedIds.Contains(id));
            listDirty = true;
        }

        /// <summary>
        /// 提交删除选中条目（FR-20 / D54）：
        /// 从上游常识库删除，并同步清理全部自定义子页引用与锁定集合，最后重建。
        /// </summary>
        private void CommitDeleteEntries()
        {
            List<string> idsToDelete = new List<string>(selectedIds);

            for (int i = 0; i < idsToDelete.Count; i++)
            {
                RimTalkMemoryBridge.Remove(idsToDelete[i]);
            }

            // 子页只存 id 引用：逐条从全部子页中剔除，避免悬空 id
            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    KnowledgeSubPage page = pages[i];
                    if (page != null && page.entryIds != null)
                    {
                        page.entryIds.RemoveAll(id => selectedIds.Contains(id));
                    }
                }
            }

            // D54 / D55：删除即放弃锁定，清理锁定集合
            FactionKnowledgeComponent.RemoveLocks(idsToDelete);

            selectedIds.Clear();
            confirmingDeleteEntries = false;
            Rebuild();
        }
    }
}
