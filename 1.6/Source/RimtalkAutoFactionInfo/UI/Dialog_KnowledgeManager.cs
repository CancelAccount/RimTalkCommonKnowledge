using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 常识管理页面（FR-16 / D49）：本 mod 自建的 <see cref="Window"/>，由设置页入口按钮唤出。
    ///
    /// 动机：上游常识库界面在条目上万时，滚动内容高度 = 条目数 × 70，滚动条滑块被压到几乎不可拖；
    /// 且条目本身不记来源（<c>tag</c> 是 Def 标签，正文也不含块 / 模组名），无法按来源定位。
    /// 本页面用「先分组、再查阅」解决这两点：左侧按
    /// 「全部 / 本 mod 注入 / 玩家自建 / 预设库（块 → 模组）/ 自定义子页」分组，分页后列表自然变短，
    /// 右侧勾选条目并把勾选项收进玩家自建子页；勾选支持单击，也支持按住左键划过各行批量刷选。
    ///
    /// 数据全部经 <see cref="RimTalkMemoryBridge.GetAllSnapshots"/>（中立快照）与
    /// <see cref="KnowledgeSourceIndex"/>（来源反查）取得，不直接依赖上游类型；
    /// 自定义子页落在本 mod 设置（<see cref="FactionInfoSettings.customKnowledgePages"/>）里，不写入上游常识库。
    /// 本页面只做查阅与归集，不改变常识注入与匹配行为。
    /// </summary>
    public class Dialog_KnowledgeManager : Window
    {
        /// <summary>左侧导航的当前分类。</summary>
        private enum NavMode
        {
            /// <summary>库内全部条目。</summary>
            All,

            /// <summary>本 mod 注入的条目（内容带本 mod 前缀）。</summary>
            Own,

            /// <summary>既非本 mod、也不在随包预设库中的条目。</summary>
            User,

            /// <summary>某一预设块下的全部条目（含其全部模组）。</summary>
            Block,

            /// <summary>某一预设块下某一模组的条目。</summary>
            Mod,

            /// <summary>某个玩家自建子页。</summary>
            Custom
        }

        // ---------- 数据 ----------

        /// <summary>库内全部条目的中立快照（打开窗口与点「刷新」时重建）。</summary>
        private List<KnowledgeEntrySnapshot> allEntries = new List<KnowledgeEntrySnapshot>();

        /// <summary>条目 id → 快照（id 可能缺失，缺失项不入表）。</summary>
        private readonly Dictionary<string, KnowledgeEntrySnapshot> byId =
            new Dictionary<string, KnowledgeEntrySnapshot>(StringComparer.Ordinal);

        /// <summary>「标签 + 内容」键 → 快照列表（一个键可能对应多条：上游库允许重复）。</summary>
        private readonly Dictionary<string, List<KnowledgeEntrySnapshot>> byKey =
            new Dictionary<string, List<KnowledgeEntrySnapshot>>(StringComparer.Ordinal);

        /// <summary>当前勾选的条目 id。跨导航保留，便于从多个块挑选后一次性归集。</summary>
        private readonly HashSet<string> selectedIds = new HashSet<string>(StringComparer.Ordinal);

        // ---------- 导航状态 ----------

        /// <summary>当前导航分类。</summary>
        private NavMode navMode = NavMode.All;

        /// <summary>当前选中的块名（<see cref="NavMode.Block"/> / <see cref="NavMode.Mod"/> 时有效）。</summary>
        private string navBlock;

        /// <summary>当前选中的模组名（<see cref="NavMode.Mod"/> 时有效）。</summary>
        private string navMod;

        /// <summary>当前选中的自定义子页下标（<see cref="NavMode.Custom"/> 时有效，-1 表示无）。</summary>
        private int navPageIndex = -1;

        /// <summary>已展开的块名集合（仅影响导航显示）。</summary>
        private readonly HashSet<string> expandedBlocks = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 左侧「预设库」分组**实际可见**的块 / 模组（仅为库内确有條目命中的来源）。
        /// 未订阅 / 未启用的 mod 其条目不会被导入，库内自然没有它们，故在此剔除，
        /// 免得列出一个点进去空白的子页。与静态只读的 <see cref="KnowledgeSourceIndex.Blocks"/> 分开持有，
        /// 不改动那份索引。
        /// </summary>
        private readonly List<KnowledgeSourceIndex.BlockGroup> visibleBlocks =
            new List<KnowledgeSourceIndex.BlockGroup>();

        // ---------- 搜索与列表缓存 ----------

        /// <summary>搜索词（同时匹配标签、正文与来源块 / 模组名）。</summary>
        private string searchText = string.Empty;

        /// <summary>当前右侧应显示的条目（按导航与搜索词算出的缓存，避免每帧重算）。</summary>
        private List<KnowledgeEntrySnapshot> cachedList = new List<KnowledgeEntrySnapshot>();

        /// <summary>缓存是否失效；导航 / 搜索 / 勾选变化时置真。</summary>
        private bool listDirty = true;

        // ---------- 滚动位置 ----------

        /// <summary>左侧导航的滚动位置。</summary>
        private Vector2 navScrollPosition;

        /// <summary>右侧条目列表的滚动位置。</summary>
        private Vector2 listScrollPosition;

        // ---------- 内联重命名 / 删除确认 ----------

        /// <summary>是否处于「重命名当前子页」状态。</summary>
        private bool renamingPage;

        /// <summary>重命名输入框的缓冲文本。</summary>
        private string renameBuffer = string.Empty;

        /// <summary>是否处于「删除当前子页」确认状态。</summary>
        private bool confirmingDeletePage;

        /// <summary>初始化窗口尺寸与行为，并载入一次数据。</summary>
        public Dialog_KnowledgeManager()
        {
            doCloseX = true;
            doCloseButton = false;
            closeOnCancel = true;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            // 固定窗口位置：条目列表的滑动批量选择靠 Input 轮询（ToggleInvisibleDraggable 不抢 hotControl），
            // 若允许拖动，Window 末尾的 GUI.DragWindow() 会把「划过列表」当成拖窗口，整页跟着鼠标跑。
            draggable = false;
            resizeable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = true;

            Rebuild();
        }

        /// <summary>窗口初始尺寸。</summary>
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(
                    FactionKnowledgeConfig.MANAGER_WINDOW_WIDTH,
                    FactionKnowledgeConfig.MANAGER_WINDOW_HEIGHT);
            }
        }

        /// <summary>窗口内容边距（缩到 8 以多留可用面积；默认 18 过宽）。</summary>
        protected override float Margin
        {
            get { return FactionKnowledgeConfig.MANAGER_MARGIN; }
        }

        /// <summary>窗口正文：标题行 + 左侧导航 + 右侧内容（搜索 / 列表 / 操作栏）。</summary>
        /// <param name="inRect">可用区域（已扣掉窗口边距）。</param>
        public override void DoWindowContents(Rect inRect)
        {
            if (!RimTalkMemoryBridge.IsLibraryAvailable)
            {
                Widgets.Label(inRect, FactionKnowledgeConfig.MANAGER_NO_GAME);
                return;
            }

            // 标题行
            Rect headerRect = new Rect(
                inRect.x, inRect.y, inRect.width, FactionKnowledgeConfig.MANAGER_HEADER_HEIGHT);
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, FactionKnowledgeConfig.MANAGER_TITLE);
            Text.Font = previousFont;

            // 正文：左侧导航 + 右侧内容，均自标题行下方开始
            float bodyTop = headerRect.yMax + FactionKnowledgeConfig.MANAGER_GAP;
            float bodyHeight = inRect.yMax - bodyTop;
            if (bodyHeight <= 0f)
            {
                return;
            }

            Rect navRect = new Rect(
                inRect.x, bodyTop, FactionKnowledgeConfig.MANAGER_NAV_WIDTH, bodyHeight);
            float contentX = navRect.xMax + FactionKnowledgeConfig.MANAGER_GAP;
            Rect contentRect = new Rect(
                contentX, bodyTop, inRect.xMax - contentX, bodyHeight);

            DrawNav(navRect);
            DrawContent(contentRect);
        }

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

            ValidatePageIndex();
            listDirty = true;
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
            if (entry == null || string.IsNullOrEmpty(entry.Content))
            {
                return false;
            }
            return entry.Content.StartsWith(
                       FactionKnowledgeConfig.FACTION_CONTENT_PREFIX, StringComparison.Ordinal)
                   || entry.Content.StartsWith(
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

        // ==================== 左侧导航 ====================

        /// <summary>左侧导航的总内容高度（供滚动视图使用）。</summary>
        private float GetNavHeight()
        {
            float height = FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT * 3f; // 全部 / 本 mod / 玩家自建

            // 预设库分组：库内无任何预设库条目时整组不占位（未订阅 mod 不建空节点）
            if (visibleBlocks.Count > 0)
            {
                height += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;    // 预设库分组标题
                for (int i = 0; i < visibleBlocks.Count; i++)
                {
                    height += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;
                    if (expandedBlocks.Contains(visibleBlocks[i].Name))
                    {
                        height += visibleBlocks[i].Mods.Count * FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;
                    }
                }
            }

            height += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;        // 自定义子页分组标题
            height += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;           // 新建子页

            List<KnowledgeSubPage> pages = FactionInfoSettings.CustomKnowledgePages;
            if (pages != null)
            {
                height += pages.Count * FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;
            }

            return height + FactionKnowledgeConfig.MANAGER_GAP;
        }

        /// <summary>绘制左侧导航：三个固定的全库分类 + 预设库（块 → 模组）两级 + 自定义子页列表。</summary>
        /// <param name="rect">导航区域。</param>
        private void DrawNav(Rect rect)
        {
            // 导航自身也可能很长（约 60 块 + 各块模组），故同样放入滚动视图
            Rect viewRect = new Rect(
                0f,
                0f,
                rect.width - FactionKnowledgeConfig.MANAGER_SCROLLBAR_WIDTH,
                GetNavHeight());

            Widgets.BeginScrollView(rect, ref navScrollPosition, viewRect);

            float y = 0f;
            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
                    FactionKnowledgeConfig.MANAGER_NAV_ALL,
                    0f,
                    navMode == NavMode.All))
            {
                SwitchNav(NavMode.All, null, null, -1);
            }
            y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;

            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
                    FactionKnowledgeConfig.MANAGER_NAV_OWN,
                    0f,
                    navMode == NavMode.Own))
            {
                SwitchNav(NavMode.Own, null, null, -1);
            }
            y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;

            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
                    FactionKnowledgeConfig.MANAGER_NAV_USER,
                    0f,
                    navMode == NavMode.User))
            {
                SwitchNav(NavMode.User, null, null, -1);
            }
            y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;

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
                    if (DrawNavRow(
                            new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
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
                    y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;

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

                        if (DrawNavRow(
                                new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
                                mod.Name, FactionKnowledgeConfig.MANAGER_NAV_INDENT, modSelected))
                        {
                            SwitchNav(NavMode.Mod, block.Name, mod.Name, -1);
                        }
                        y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;
                    }
                }
            }

            // 自定义子页分组
            DrawNavHeader(
                new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT),
                FactionKnowledgeConfig.MANAGER_NAV_CUSTOM);
            y += FactionKnowledgeConfig.MANAGER_NAV_HEADER_HEIGHT;

            if (DrawNavRow(
                    new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
                    FactionKnowledgeConfig.MANAGER_NEW_PAGE, 0f, false))
            {
                CreatePage();
            }
            y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;

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

                    if (DrawNavRow(
                            new Rect(0f, y, viewRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT),
                            label, 0f, navMode == NavMode.Custom && navPageIndex == i))
                    {
                        SwitchNav(NavMode.Custom, null, null, i);
                        renamingPage = false;
                        confirmingDeletePage = false;
                        renameBuffer = string.Empty;
                    }
                    y += FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT;
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
        }

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

        /// <summary>绘制一行条目：左侧复选框 + 第一行「标签 —— 来源」+ 第二行正文预览；整行可点可按（滑动批量选择）。</summary>
        /// <param name="rect">行矩形（坐标位于滚动视图内容空间）。</param>
        /// <param name="entry">条目快照。</param>
        private void DrawRow(Rect rect, KnowledgeEntrySnapshot entry)
        {
            bool selected = selectedIds.Contains(entry.Id);

            // 复选框只负责显示，不再单独接收点击 —— 整行统一由 ToggleInvisibleDraggable 处理，
            // 否则「复选框 + 整行」两个热区重叠会让一次点击触发两次切换。
            Rect checkRect = new Rect(
                rect.x + FactionKnowledgeConfig.MANAGER_ROW_INDENT,
                rect.y + (rect.height - FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE) / 2f,
                FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE,
                FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE);

            if (selected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            // 整行作为可拖动切换区（paintable）：单击切换单行；按住左键划过各行则批量刷成同一状态，
            // 即「滑动批量选择」。笔刷状态由 Verse.Widgets 维护，鼠标抬起时自动复位。
            bool check = selected;
            Widgets.ToggleInvisibleDraggable(rect, ref check, true, true);
            if (check != selected)
            {
                SetSelection(entry.Id, check);
            }

            Widgets.CheckboxDraw(
                checkRect.x,
                checkRect.y,
                check,
                false,
                FactionKnowledgeConfig.MANAGER_CHECKBOX_SIZE,
                null,
                null);

            float textX = checkRect.xMax + FactionKnowledgeConfig.MANAGER_ROW_INDENT;
            float textWidth = rect.xMax - textX - FactionKnowledgeConfig.MANAGER_ROW_INDENT;
            Rect tagRect = new Rect(
                textX,
                rect.y + FactionKnowledgeConfig.MANAGER_ROW_LINE_Y,
                textWidth,
                FactionKnowledgeConfig.MANAGER_ROW_LINE_HEIGHT);
            Rect contentRect = new Rect(
                textX,
                tagRect.yMax,
                textWidth,
                FactionKnowledgeConfig.MANAGER_ROW_LINE_HEIGHT);

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(tagRect, FormatTagLine(entry));
            Color previousColor = GUI.color;
            GUI.color = FactionKnowledgeConfig.MANAGER_SECONDARY_COLOR;
            Widgets.Label(contentRect, Truncate(
                entry.Content, FactionKnowledgeConfig.MANAGER_CONTENT_MAX_CHARS));
            GUI.color = previousColor;
            Text.Anchor = previousAnchor;

            TooltipHandler.TipRegion(
                rect,
                (entry.Tag ?? string.Empty)
                    + FactionKnowledgeConfig.MANAGER_TIP_BLOCK_SEPARATOR
                    + (entry.Content ?? string.Empty));
        }

        /// <summary>拼出条目行第一行：标签（截断）+ 来源后缀。</summary>
        /// <param name="entry">条目快照。</param>
        private static string FormatTagLine(KnowledgeEntrySnapshot entry)
        {
            return Truncate(entry.Tag, FactionKnowledgeConfig.MANAGER_TAG_MAX_CHARS)
                + FactionKnowledgeConfig.MANAGER_ROW_SOURCE_SEPARATOR
                + BuildSourceText(entry);
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

        // ==================== 底部操作栏 ====================

        /// <summary>绘制底部操作栏；处于重命名 / 删除确认时改绘对应的内联条。</summary>
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

            float width = FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH;
            float height = FactionKnowledgeConfig.MANAGER_ACTION_ROW_HEIGHT;
            float gap = FactionKnowledgeConfig.MANAGER_GAP;

            // 第一行：勾选操作
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

            // 第二行：子页操作（无选中子页时置灰）
            float secondY = rect.y + height + gap;
            KnowledgeSubPage currentPage = CurrentPage;
            bool hasPage = currentPage != null;
            float x2 = rect.x;

            if (Widgets.ButtonText(
                    new Rect(x2, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_ADD_TO_PAGE, true, true, hasPage))
            {
                AddSelectionToPage();
            }
            x2 += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x2, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_REMOVE_FROM_PAGE, true, true, hasPage))
            {
                RemoveSelectionFromPage();
            }
            x2 += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x2, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_RENAME_PAGE, true, true, hasPage))
            {
                renamingPage = true;
                renameBuffer = currentPage.name;
            }
            x2 += width + gap;

            if (Widgets.ButtonText(
                    new Rect(x2, secondY, width, height),
                    FactionKnowledgeConfig.MANAGER_DELETE_PAGE, true, true, hasPage))
            {
                confirmingDeletePage = true;
            }

            if (!hasPage)
            {
                TooltipHandler.TipRegion(
                    new Rect(rect.x, secondY, rect.width, height),
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
    }
}
