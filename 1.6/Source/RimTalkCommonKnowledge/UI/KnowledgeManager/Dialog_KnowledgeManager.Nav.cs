using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimTalkCommonKnowledge
{
        /// <summary>本文件为 Dialog_KnowledgeManager 的 partial 部分（D65 拆分）：左侧导航的测量、绘制与分类切换。</summary>
        public partial class Dialog_KnowledgeManager
        {
        // ==================== 左侧导航 ====================

        /// <summary>
        /// 测量一个导航子页行的实际高度：按标签文本在该行可用宽度内的排版高度计算，
        /// 钳制在「单行高 ~ 两行上限」之间。高度测量（<see cref="GetNavHeight"/>）与
        /// 实际绘制（<see cref="DrawNav"/>）都只走本方法，保证两处行高严格一致、不会错位。
        /// </summary>
        /// <param name="label">行标签文本。</param>
        /// <param name="indent">文本左缩进（模组 / 子行用）。</param>
        /// <param name="viewWidth">导航滚动视图内容总宽。</param>
        private static float MeasureNavRow(string label, float indent, float viewWidth)
        {
            float textWidth = viewWidth - indent - FactionKnowledgeConfig.MANAGER_ROW_INDENT;
            float textHeight = Text.CalcHeight(label ?? string.Empty, Mathf.Max(textWidth, 1f));
            return Mathf.Clamp(
                textHeight,
                FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT,
                FactionKnowledgeConfig.MANAGER_NAV_ROW_MAX_HEIGHT);
        }

        /// <summary>左侧导航的总内容高度（供滚动视图使用）：逐行按标签实际行高累加。</summary>
        /// <param name="viewWidth">导航滚动视图内容总宽（与绘制时一致）。</param>
        private float GetNavHeight(float viewWidth)
        {
            float gap = FactionKnowledgeConfig.MANAGER_GAP;
            float height = 0f;

            // 三个固定全库分类
            height += MeasureNavRow(FactionKnowledgeConfig.MANAGER_NAV_ALL, 0f, viewWidth);
            height += MeasureNavRow(FactionKnowledgeConfig.MANAGER_NAV_OWN, 0f, viewWidth);

            // 派系信息子行（缩进，标签带计数；仅库内有该类条目时占位）
            if (factionEntryCount > 0)
            {
                height += MeasureNavRow(
                    string.Format(
                        FactionKnowledgeConfig.MANAGER_PAGE_ROW_FORMAT,
                        FactionKnowledgeConfig.MANAGER_NAV_OWN_FACTION, factionEntryCount),
                    FactionKnowledgeConfig.MANAGER_NAV_INDENT, viewWidth);
            }

            // 异种人信息子行
            if (xenotypeEntryCount > 0)
            {
                height += MeasureNavRow(
                    string.Format(
                        FactionKnowledgeConfig.MANAGER_PAGE_ROW_FORMAT,
                        FactionKnowledgeConfig.MANAGER_NAV_OWN_XENOTYPE, xenotypeEntryCount),
                    FactionKnowledgeConfig.MANAGER_NAV_INDENT, viewWidth);
            }

            // 玩家自建固定分类
            height += MeasureNavRow(FactionKnowledgeConfig.MANAGER_NAV_USER, 0f, viewWidth);

            // 预设库分组：库内无任何预设库条目时整组不占位（未订阅 mod 不建空节点）
            if (visibleBlocks.Count > 0)
            {
                height += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;    // 分组标题
                for (int i = 0; i < visibleBlocks.Count; i++)
                {
                    KnowledgeSourceIndex.BlockGroup block = visibleBlocks[i];
                    bool expanded = expandedBlocks.Contains(block.Name);

                    // 块行标签口径必须与 DrawNav 完全一致（展开 / 折叠前缀 + 块行格式）
                    string blockLabel = (expanded
                        ? FactionKnowledgeConfig.MANAGER_EXPANDED_PREFIX
                        : FactionKnowledgeConfig.MANAGER_COLLAPSED_PREFIX)
                        + string.Format(
                            FactionKnowledgeConfig.MANAGER_BLOCK_ROW_FORMAT, block.Name, block.Mods.Count);
                    height += MeasureNavRow(blockLabel, 0f, viewWidth);

                    if (expanded)
                    {
                        for (int j = 0; j < block.Mods.Count; j++)
                        {
                            height += MeasureNavRow(
                                block.Mods[j].Name,
                                FactionKnowledgeConfig.MANAGER_NAV_INDENT, viewWidth);
                        }
                    }
                }
            }

            // 自定义子页分组
            height += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;        // 分组标题
            height += MeasureNavRow(FactionKnowledgeConfig.MANAGER_NEW_PAGE, 0f, viewWidth);

            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    KnowledgeSubPage page = pages[i];
                    if (page == null)
                    {
                        continue;
                    }
                    int count = page.entryIds == null ? 0 : page.entryIds.Count;
                    height += MeasureNavRow(
                        string.Format(
                            FactionKnowledgeConfig.MANAGER_PAGE_ROW_FORMAT, page.name, count),
                        0f, viewWidth);
                }
            }

            return height + gap;
        }

        /// <summary>绘制左侧导航：三个固定的全库分类（「本 mod 注入」下挂派系 / 异种人两个子行）+ 预设库（块 → 模组）两级 + 自定义子页列表；各子页标题最长可折两行（适应长 mod 名）。</summary>
        /// <param name="rect">导航区域。</param>
        private void DrawNav(Rect rect)
        {
            // 导航自身也可能很长（约 60 块 + 各块模组），故同样放入滚动视图
            float viewWidth = rect.width - FactionKnowledgeConfig.MANAGER_SCROLLBAR_WIDTH;
            Rect viewRect = new Rect(0f, 0f, viewWidth, GetNavHeight(viewWidth));

            Widgets.BeginScrollView(rect, ref navScrollPosition, viewRect);

            float y = 0f;
            float rowHeight;

            // ---- 全部条目 ----
            rowHeight = MeasureNavRow(
                FactionKnowledgeConfig.MANAGER_NAV_ALL, 0f, viewRect.width);
            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, rowHeight),
                    FactionKnowledgeConfig.MANAGER_NAV_ALL,
                    0f,
                    navMode == NavMode.All))
            {
                SwitchNav(NavMode.All, null, null, -1);
            }
            y += rowHeight;

            // ---- 本 mod 注入（总览行）----
            rowHeight = MeasureNavRow(
                FactionKnowledgeConfig.MANAGER_NAV_OWN, 0f, viewRect.width);
            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, rowHeight),
                    FactionKnowledgeConfig.MANAGER_NAV_OWN,
                    0f,
                    navMode == NavMode.Own
                        || navMode == NavMode.OwnFaction
                        || navMode == NavMode.OwnXenotype))
            {
                SwitchNav(NavMode.Own, null, null, -1);
            }
            y += rowHeight;

            // 派系信息子行（仅库内有派系条目时显示；标签带条目计数）
            if (factionEntryCount > 0)
            {
                string label = string.Format(
                    FactionKnowledgeConfig.MANAGER_PAGE_ROW_FORMAT,
                    FactionKnowledgeConfig.MANAGER_NAV_OWN_FACTION, factionEntryCount);
                rowHeight = MeasureNavRow(
                    label, FactionKnowledgeConfig.MANAGER_NAV_INDENT, viewRect.width);
                if (DrawNavRow(
                        new Rect(0f, y, viewRect.width, rowHeight),
                        label,
                        FactionKnowledgeConfig.MANAGER_NAV_INDENT,
                        navMode == NavMode.OwnFaction))
                {
                    SwitchNav(NavMode.OwnFaction, null, null, -1);
                }
                y += rowHeight;
            }

            // 异种人信息子行（仅库内有异种人条目时显示；标签带条目计数）
            if (xenotypeEntryCount > 0)
            {
                string label = string.Format(
                    FactionKnowledgeConfig.MANAGER_PAGE_ROW_FORMAT,
                    FactionKnowledgeConfig.MANAGER_NAV_OWN_XENOTYPE, xenotypeEntryCount);
                rowHeight = MeasureNavRow(
                    label, FactionKnowledgeConfig.MANAGER_NAV_INDENT, viewRect.width);
                if (DrawNavRow(
                        new Rect(0f, y, viewRect.width, rowHeight),
                        label,
                        FactionKnowledgeConfig.MANAGER_NAV_INDENT,
                        navMode == NavMode.OwnXenotype))
                {
                    SwitchNav(NavMode.OwnXenotype, null, null, -1);
                }
                y += rowHeight;
            }

            // ---- 玩家自建 ----
            rowHeight = MeasureNavRow(
                FactionKnowledgeConfig.MANAGER_NAV_USER, 0f, viewRect.width);
            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, rowHeight),
                    FactionKnowledgeConfig.MANAGER_NAV_USER,
                    0f,
                    navMode == NavMode.User))
            {
                SwitchNav(NavMode.User, null, null, -1);
            }
            y += rowHeight;

            // 预设库分组：库内无任何预设库条目时整组不显示（未订阅 mod 不建空节点）
            if (visibleBlocks.Count > 0)
            {
                DrawNavHeader(
                    new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT),
                    FactionKnowledgeConfig.MANAGER_NAV_PRESET);
                y += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;

                for (int i = 0; i < visibleBlocks.Count; i++)
                {
                    KnowledgeSourceIndex.BlockGroup block = visibleBlocks[i];
                    bool expanded = expandedBlocks.Contains(block.Name);
                    bool blockSelected =
                        (navMode == NavMode.Block || navMode == NavMode.Mod)
                        && string.Equals(navBlock, block.Name, StringComparison.Ordinal);

                    string label = (expanded
                        ? FactionKnowledgeConfig.MANAGER_EXPANDED_PREFIX
                        : FactionKnowledgeConfig.MANAGER_COLLAPSED_PREFIX)
                        + string.Format(
                            FactionKnowledgeConfig.MANAGER_BLOCK_ROW_FORMAT, block.Name, block.Mods.Count);

                    // 点块行：既切到「整块」视图，又切换展开状态（省一个单独的三角形按钮）
                    rowHeight = MeasureNavRow(label, 0f, viewRect.width);
                    if (DrawNavRow(
                            new Rect(0f, y, viewRect.width, rowHeight),
                            label, 0f, blockSelected))
                    {
                        if (expanded)
                        {
                            expandedBlocks.Remove(block.Name);
                        }
                        else
                        {
                            expandedBlocks.Add(block.Name);
                        }
                        SwitchNav(NavMode.Block, block.Name, null, -1);
                    }
                    y += rowHeight;

                    if (!expanded)
                    {
                        continue;
                    }

                    for (int j = 0; j < block.Mods.Count; j++)
                    {
                        KnowledgeSourceIndex.ModGroup mod = block.Mods[j];
                        bool modSelected = navMode == NavMode.Mod
                            && string.Equals(navBlock, block.Name, StringComparison.Ordinal)
                            && string.Equals(navMod, mod.Name, StringComparison.Ordinal);

                        rowHeight = MeasureNavRow(
                            mod.Name, FactionKnowledgeConfig.MANAGER_NAV_INDENT, viewRect.width);
                        if (DrawNavRow(
                                new Rect(0f, y, viewRect.width, rowHeight),
                                mod.Name, FactionKnowledgeConfig.MANAGER_NAV_INDENT, modSelected))
                        {
                            SwitchNav(NavMode.Mod, block.Name, mod.Name, -1);
                        }
                        y += rowHeight;
                    }
                }
            }

            // 自定义子页分组
            DrawNavHeader(
                new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT),
                FactionKnowledgeConfig.MANAGER_NAV_CUSTOM);
            y += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;

            rowHeight = MeasureNavRow(
                FactionKnowledgeConfig.MANAGER_NEW_PAGE, 0f, viewRect.width);
            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, rowHeight),
                    FactionKnowledgeConfig.MANAGER_NEW_PAGE, 0f, false))
            {
                CreatePage();
            }
            y += rowHeight;

            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    KnowledgeSubPage page = pages[i];
                    if (page == null)
                    {
                        continue;
                    }
                    int count = page.entryIds == null ? 0 : page.entryIds.Count;
                    string label = string.Format(
                        FactionKnowledgeConfig.MANAGER_PAGE_ROW_FORMAT, page.name, count);

                    rowHeight = MeasureNavRow(label, 0f, viewRect.width);
                    if (DrawNavRow(
                            new Rect(0f, y, viewRect.width, rowHeight),
                            label, 0f, navMode == NavMode.Custom && navPageIndex == i))
                    {
                        SwitchNav(NavMode.Custom, null, null, i);
                        renamingPage = false;
                        confirmingDeletePage = false;
                        renameBuffer = string.Empty;
                    }
                    y += rowHeight;
                }
            }

            Widgets.EndScrollView();
        }

        /// <summary>绘制一个导航行（选中高亮 / 悬停高亮 + 左对齐文本），返回本帧是否被点击。</summary>
        /// <param name="rect">行矩形。</param>
        /// <param name="label">显示文本。</param>
        /// <param name="indent">文本左缩进（模组行用）。</param>
        /// <param name="selected">是否处于选中态。</param>
        private static bool DrawNavRow(Rect rect, string label, float indent, bool selected)
        {
            if (selected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            bool clicked = Widgets.ButtonInvisible(rect);

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                new Rect(
                    rect.x + indent + FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                    rect.y,
                    rect.width - indent - FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                    rect.height),
                label);
            Text.Anchor = previousAnchor;
            return clicked;
        }

        /// <summary>绘制导航分组标题（无交互的次级文本）。</summary>
        /// <param name="rect">标题矩形。</param>
        /// <param name="label">标题文本。</param>
        private static void DrawNavHeader(Rect rect, string label)
        {
            Color previous = GUI.color;
            GUI.color = FactionKnowledgeConfig.MANAGER_HINT_COLOR;
            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                new Rect(
                    rect.x + FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                    rect.y,
                    rect.width - FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                    rect.height),
                label);
            Text.Anchor = previousAnchor;
            GUI.color = previous;
        }

        /// <summary>切换导航目标并让右侧列表失效。</summary>
        /// <param name="mode">目标分类。</param>
        /// <param name="block">块名（非预设库分类传 <c>null</c>）。</param>
        /// <param name="mod">模组名（非模组分类传 <c>null</c>）。</param>
        /// <param name="pageIndex">子页下标（非子页分类传 -1）。</param>
        private void SwitchNav(NavMode mode, string block, string mod, int pageIndex)
        {
            navMode = mode;
            navBlock = block;
            navMod = mod;
            navPageIndex = pageIndex;
            listDirty = true;
            confirmingDeletePage = false;
            confirmingDeleteEntries = false;
        }

    }
}
