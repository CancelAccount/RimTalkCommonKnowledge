using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimTalkCommonKnowledge
{
        /// <summary>本文件为 Dialog_KnowledgeManager 的 partial 部分（D65 拆分）：右侧条目列表——搜索过滤、虚拟列表、行绘制与滑动批量勾选。</summary>
        public partial class Dialog_KnowledgeManager
        {
        // ==================== 右侧内容 ====================

        /// <summary>绘制右侧内容：搜索行 + 条目列表 + 底部操作栏（重命名 / 删除确认时替换操作栏）。</summary>
        /// <param name="rect">内容区域。</param>
        private void DrawContent(Rect rect)
        {
            EnsureList();

            // 搜索框
            float searchWidth = Mathf.Min(rect.width * 0.45f, 320f);
            Rect searchRect = new Rect(
                rect.x, rect.y, searchWidth, FactionKnowledgeConfig.MANAGER_SEARCH_HEIGHT);
            string typed = Widgets.TextField(searchRect, searchText);
            if (!string.Equals(typed, searchText, StringComparison.Ordinal))
            {
                searchText = typed ?? string.Empty;
                listDirty = true;
            }
            if (string.IsNullOrEmpty(searchText))
            {
                Color previous = GUI.color;
                GUI.color = FactionKnowledgeConfig.MANAGER_HINT_COLOR;
                Widgets.Label(
                    new Rect(
                        searchRect.x + FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                        searchRect.y,
                        searchRect.width - FactionKnowledgeConfig.MANAGER_ROW_INDENT * 2f,
                        searchRect.height),
                    FactionKnowledgeConfig.MANAGER_SEARCH_PLACEHOLDER);
                GUI.color = previous;
            }

            // 计数（搜索框右侧）
            Rect countRect = new Rect(
                searchRect.xMax + FactionKnowledgeConfig.MANAGER_GAP,
                rect.y,
                rect.xMax - searchRect.xMax - FactionKnowledgeConfig.MANAGER_GAP,
                FactionKnowledgeConfig.MANAGER_SEARCH_HEIGHT);
            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                countRect,
                string.Format(
                    FactionKnowledgeConfig.MANAGER_COUNT_FORMAT,
                    cachedList.Count, allEntries.Count, selectedIds.Count));
            Text.Anchor = previousAnchor;

            // 列表与操作栏
            float listTop = searchRect.yMax + FactionKnowledgeConfig.MANAGER_GAP;
            float actionTop = rect.yMax - FactionKnowledgeConfig.MANAGER_ACTION_BAR_HEIGHT;
            Rect listRect = new Rect(rect.x, listTop, rect.width, actionTop - listTop);
            Rect actionRect = new Rect(
                rect.x, actionTop, rect.width, FactionKnowledgeConfig.MANAGER_ACTION_BAR_HEIGHT);

            if (listRect.height > 0f)
            {
                DrawList(listRect);
            }
            DrawActionBar(actionRect);

            // 松开兜底：松开位置不在任何行热区上时行级方法不会执行，在此统一结束勾选涂抹
            if (Input.GetMouseButtonUp(0))
            {
                paintSelectActive = false;
            }

            // 勾选滑动涂抹期间，光标旁显示目标勾选状态图标
            // （在窗口 group 内调用，Event.mousePosition 已是 group 局部坐标，与 ImmediateWindow 自洽）
            if (paintSelectActive)
            {
                GenUI.DrawMouseAttachment(paintSelectState
                    ? Widgets.CheckboxOnTex
                    : Widgets.CheckboxOffTex);
            }
        }

        /// <summary>按当前导航与搜索词重算右侧列表（仅在缓存失效时执行）。</summary>
        private void EnsureList()
        {
            if (!listDirty)
            {
                return;
            }
            listDirty = false;

            List<KnowledgeEntrySnapshot> result = new List<KnowledgeEntrySnapshot>();

            switch (navMode)
            {
                case NavMode.All:
                    for (int i = 0; i < allEntries.Count; i++)
                    {
                        if (MatchesSearch(allEntries[i]))
                        {
                            result.Add(allEntries[i]);
                        }
                    }
                    break;

                case NavMode.Own:
                    for (int i = 0; i < allEntries.Count; i++)
                    {
                        if (IsOwnEntry(allEntries[i]) && MatchesSearch(allEntries[i]))
                        {
                            result.Add(allEntries[i]);
                        }
                    }
                    break;

                case NavMode.OwnFaction:
                    for (int i = 0; i < allEntries.Count; i++)
                    {
                        if (IsFactionEntry(allEntries[i]) && MatchesSearch(allEntries[i]))
                        {
                            result.Add(allEntries[i]);
                        }
                    }
                    break;

                case NavMode.OwnXenotype:
                    for (int i = 0; i < allEntries.Count; i++)
                    {
                        if (IsXenotypeEntry(allEntries[i]) && MatchesSearch(allEntries[i]))
                        {
                            result.Add(allEntries[i]);
                        }
                    }
                    break;

                case NavMode.User:
                    for (int i = 0; i < allEntries.Count; i++)
                    {
                        KnowledgeEntrySnapshot entry = allEntries[i];
                        if (!IsOwnEntry(entry) && !IsPresetEntry(entry) && MatchesSearch(entry))
                        {
                            result.Add(entry);
                        }
                    }
                    break;

                case NavMode.Block:
                case NavMode.Mod:
                    HashSet<string> presetIds = BuildPresetScopeIds();
                    if (presetIds != null)
                    {
                        for (int i = 0; i < allEntries.Count; i++)
                        {
                            KnowledgeEntrySnapshot entry = allEntries[i];
                            if (presetIds.Contains(entry.Id) && MatchesSearch(entry))
                            {
                                result.Add(entry);
                            }
                        }
                    }
                    break;

                case NavMode.Custom:
                    KnowledgeSubPage page = CurrentPage;
                    if (page != null && page.entryIds != null)
                    {
                        HashSet<string> pageIds =
                            new HashSet<string>(page.entryIds, StringComparer.Ordinal);
                        for (int i = 0; i < allEntries.Count; i++)
                        {
                            KnowledgeEntrySnapshot entry = allEntries[i];
                            if (pageIds.Contains(entry.Id) && MatchesSearch(entry))
                            {
                                result.Add(entry);
                            }
                        }
                    }
                    break;
            }

            cachedList = result;
        }

        /// <summary>
        /// 取当前预设库导航目标（整块或单模组）所覆盖的条目 id 集合；目标已失效时返回 <c>null</c>。
        /// </summary>
        private HashSet<string> BuildPresetScopeIds()
        {
            if (string.IsNullOrEmpty(navBlock))
            {
                return null;
            }

            // 与左侧导航同源：只在可见块 / 模组中查找（未订阅 mod 不参与）
            for (int i = 0; i < visibleBlocks.Count; i++)
            {
                KnowledgeSourceIndex.BlockGroup block = visibleBlocks[i];
                if (!string.Equals(block.Name, navBlock, StringComparison.Ordinal))
                {
                    continue;
                }

                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                bool modOnly = navMode == NavMode.Mod;
                for (int j = 0; j < block.Mods.Count; j++)
                {
                    KnowledgeSourceIndex.ModGroup mod = block.Mods[j];
                    if (modOnly && !string.Equals(mod.Name, navMod, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    for (int k = 0; k < mod.Keys.Count; k++)
                    {
                        List<KnowledgeEntrySnapshot> bucket;
                        if (!byKey.TryGetValue(mod.Keys[k], out bucket))
                        {
                            continue;
                        }
                        for (int m = 0; m < bucket.Count; m++)
                        {
                            if (!string.IsNullOrEmpty(bucket[m].Id))
                            {
                                ids.Add(bucket[m].Id);
                            }
                        }
                    }
                }
                return ids;
            }

            return null;
        }

        /// <summary>条目是否命中当前搜索词（匹配标签、正文，以及来源块 / 模组名）。</summary>
        /// <param name="entry">待判定的条目。</param>
        private bool MatchesSearch(KnowledgeEntrySnapshot entry)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }
            if (ContainsIgnoreCase(entry.Tag, searchText) || ContainsIgnoreCase(entry.Content, searchText))
            {
                return true;
            }

            string block;
            string mod;
            if (KnowledgeSourceIndex.TryGetSource(entry.Tag, entry.Content, out block, out mod))
            {
                if (ContainsIgnoreCase(block, searchText) || ContainsIgnoreCase(mod, searchText))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>不区分大小写的子串包含判定。</summary>
        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrEmpty(source)
                && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 绘制条目列表。只绘制滚动视口内的行 —— 与上游界面补丁（D48）同思路，
        /// 避免「全部条目」上万行时每帧全画导致卡顿。
        /// </summary>
        /// <param name="rect">列表区域。</param>
        private void DrawList(Rect rect)
        {
            float rowHeight = FactionKnowledgeConfig.MANAGER_ROW_HEIGHT;
            Rect viewRect = new Rect(
                0f,
                0f,
                rect.width - FactionKnowledgeConfig.MANAGER_SCROLLBAR_WIDTH,
                Mathf.Max(rect.height, cachedList.Count * rowHeight));

            Widgets.BeginScrollView(rect, ref listScrollPosition, viewRect);

            int first = Mathf.Max(0, Mathf.FloorToInt(listScrollPosition.y / rowHeight) - 1);
            int last = Mathf.Min(
                cachedList.Count - 1,
                Mathf.CeilToInt((listScrollPosition.y + rect.height) / rowHeight) + 1);

            for (int i = first; i <= last; i++)
            {
                DrawRow(new Rect(0f, i * rowHeight, viewRect.width, rowHeight), cachedList[i]);
            }

            Widgets.EndScrollView();
        }

        /// <summary>单列「按下翻转 + 滑动涂抹」勾选的处理结果。</summary>
        private struct DraggableToggleResult
        {
            /// <summary>处理后的勾选值。</summary>
            public bool value;

            /// <summary>本帧值是否发生变化（单击翻转 / 起始拖动翻转 / 涂抹刷成目标值）。</summary>
            public bool changed;
        }

        /// <summary>
        /// 可拖动勾选：单击翻转当前值；按住左键拖过同列各行时，把各行批量刷成起始行翻转后的状态。
        /// 行为复刻 <see cref="Widgets.ToggleInvisibleDraggable"/> 的 paintable 模式，
        /// 但涂抹状态（是否正在涂抹 + 目标值）由调用方按列 ref 持有 —— 引擎的
        /// <c>Widgets.checkboxPainting</c> 是全局静态量，勾选列与启用列同时 paintable 时
        /// 斜向拖动会跨列串改，故必须按列隔离（D61，思路参照 RimFacilityCompat 的状态自持）。
        /// </summary>
        /// <param name="rect">热区（同列各行互不重叠，两列之间留间隙）。</param>
        /// <param name="current">当前勾选值。</param>
        /// <param name="painting">按列持有的「正在涂抹」标记。</param>
        /// <param name="paintingState">按列持有的涂抹目标值。</param>
        /// <returns>处理后的新值与本帧是否变化。</returns>
        private static DraggableToggleResult DraggableToggle(
            Rect rect, bool current, ref bool painting, ref bool paintingState)
        {
            // doMouseoverSound: true —— 热区悬停音由 ButtonInvisibleDraggable 内部播放
            Widgets.DraggableResult result = Widgets.ButtonInvisibleDraggable(rect, true);
            bool value = current;
            bool changed = false;

            if (result == Widgets.DraggableResult.Pressed)
            {
                // 单击（未拖动即松开）：翻转
                value = !value;
                changed = true;
            }
            else if (result == Widgets.DraggableResult.Dragged)
            {
                // 拖动越过阈值（只在越线那一帧触发一次）：翻转起始行并开启涂抹
                value = !value;
                changed = true;
                painting = true;
                paintingState = value;
            }

            // 涂抹期间鼠标悬停的后续行：与目标不一致即刷成目标值（幂等）
            if (painting && Mouse.IsOver(rect) && Input.GetMouseButton(0) && value != paintingState)
            {
                value = paintingState;
                changed = true;
            }

            if (changed)
            {
                // 翻转音效，与引擎 CheckboxLabeled 行为一致
                if (value)
                {
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
                }
                else
                {
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
                }
            }

            // 鼠标松开即结束本列涂抹（与 WidgetsOnGUI 的复位时机对齐）
            if (Input.GetMouseButtonUp(0))
            {
                painting = false;
            }

            return new DraggableToggleResult { value = value, changed = changed };
        }

        /// <summary>绘制一行条目：左侧复选框 + 第一行「标签 —— 来源」+ 两行正文预览 + 停用红竖条；整行热区可点可按（滑动批量选择）。</summary>
        /// <param name="rect">行矩形（坐标位于滚动视图内容空间）。</param>
        /// <param name="entry">条目快照。</param>
        private void DrawRow(Rect rect, KnowledgeEntrySnapshot entry)
        {
            bool selected = selectedIds.Contains(entry.Id);

            // 复选框只负责显示，不再单独接收点击 —— 整行统一由 DraggableToggle 处理，
            // 否则「复选框 + 整行」两个热区重叠会让一次点击触发两次切换。
            Rect checkRect = new Rect(
                rect.x + FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                rect.y + (rect.height - FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE) / 2f,
                FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE,
                FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE);

            // 右侧停用标记竖色条（只绘不点；启用行不绘制，界面保持干净）
            Rect badgeRect = new Rect(
                rect.xMax - FactionKnowledgeConfig.MANAGER_ROW_BADGE_WIDTH,
                rect.y,
                FactionKnowledgeConfig.MANAGER_ROW_BADGE_WIDTH,
                rect.height);

            // 行内已无独立可点控件，整行都是勾选热区（角标位置点击等同勾选该行）
            if (selected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            // 整行作为可拖动切换区：单击切换单行；按住左键划过各行批量刷成同一状态（D61）。
            DraggableToggleResult selectResult = DraggableToggle(
                rect, selected, ref paintSelectActive, ref paintSelectState);
            if (selectResult.changed)
            {
                SetSelection(entry.Id, selectResult.value);
            }
            bool check = selectResult.value;

            Widgets.CheckboxDraw(
                checkRect.x,
                checkRect.y,
                check,
                false,
                FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE,
                null,
                null);

            float textX = checkRect.xMax + FactionKnowledgeConfig.MANAGER_ROW_INDENT;
            // 文本右界止于竖色条左侧并留常规缩进
            float textWidth = badgeRect.xMin - FactionKnowledgeConfig.MANAGER_ROW_INDENT - textX;
            Rect tagRect = new Rect(
                textX,
                rect.y + FactionKnowledgeConfig.MANAGER_ROW_LINE_Y,
                textWidth,
                FactionKnowledgeConfig.MANAGER_ROW_LINE_HEIGHT);
            Rect contentRect = new Rect(
                textX,
                tagRect.yMax,
                textWidth,
                FactionKnowledgeConfig.MANAGER_ROW_LINE_HEIGHT
                    * FactionKnowledgeConfig.MANAGER_ROW_CONTENT_LINES);

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            // 标签默认白色，停用压灰
            Color previousColor = GUI.color;
            GUI.color = entry.IsEnabled
                ? Color.white
                : FactionKnowledgeConfig.MANAGER_DISABLED_COLOR;
            Widgets.Label(tagRect, FormatTagLine(entry));
            // 正文两行：顶对齐自动换行，避免 MiddleLeft 把两行文本块整体压偏；停用同样压灰
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = entry.IsEnabled
                ? FactionKnowledgeConfig.MANAGER_SECONDARY_COLOR
                : FactionKnowledgeConfig.MANAGER_DISABLED_COLOR;
            Widgets.Label(contentRect, Truncate(
                entry.Content, FactionKnowledgeConfig.MANAGER_CONTENT_MAX_CHARS));
            GUI.color = previousColor;
            Text.Anchor = previousAnchor;

            // 全文 Tooltip 恢复挂整行（行内无其它可点控件）
            TooltipHandler.TipRegion(
                rect,
                (entry.Tag ?? string.Empty)
                    + FactionKnowledgeConfig.MANAGER_TIP_BLOCK_SEPARATOR
                    + (entry.Content ?? string.Empty));

            // 停用红竖条最后绘制，保证不被高亮 / 其它控件覆盖
            if (!entry.IsEnabled)
            {
                Widgets.DrawBoxSolid(
                    badgeRect, FactionKnowledgeConfig.MANAGER_DISABLED_BADGE_COLOR);
            }
        }

        /// <summary>拼出条目行第一行：标签（截断）+ 来源后缀；锁定条目再追加锁定标识（停用态由灰字 + 红竖条表达，不占文字）。</summary>
        /// <param name="entry">条目快照。</param>
        private static string FormatTagLine(KnowledgeEntrySnapshot entry)
        {
            string line = Truncate(entry.Tag, FactionKnowledgeConfig.MANAGER_TAG_MAX_CHARS)
                + FactionKnowledgeConfig.MANAGER_ROW_SOURCE_SEPARATOR
                + BuildSourceText(entry);

            if (RimTalkMemoryBridge.IsLocked(entry.Id))
            {
                line += FactionKnowledgeConfig.MANAGER_LOCKED_SUFFIX;
            }
            return line;
        }

        /// <summary>条目的来源文本：预设库显示「块 / 模组」，本 mod 显示「本 mod 注入」，其余显示「玩家自建」。</summary>
        /// <param name="entry">条目快照。</param>
        private static string BuildSourceText(KnowledgeEntrySnapshot entry)
        {
            if (IsOwnEntry(entry))
            {
                return FactionKnowledgeConfig.MANAGER_NAV_OWN;
            }

            string block;
            string mod;
            if (KnowledgeSourceIndex.TryGetSource(entry.Tag, entry.Content, out block, out mod))
            {
                if (string.IsNullOrEmpty(mod))
                {
                    return block;
                }
                return block + FactionKnowledgeConfig.MANAGER_SOURCE_JOINER + mod;
            }

            return FactionKnowledgeConfig.MANAGER_NAV_USER;
        }

        /// <summary>按字符数截断文本，超出时追加省略号。</summary>
        /// <param name="text">原文。</param>
        /// <param name="maxChars">最大字符数。</param>
        private static string Truncate(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            {
                return text ?? string.Empty;
            }
            return text.Substring(0, maxChars) + FactionKnowledgeConfig.MANAGER_ELLIPSIS;
        }

    }
}
