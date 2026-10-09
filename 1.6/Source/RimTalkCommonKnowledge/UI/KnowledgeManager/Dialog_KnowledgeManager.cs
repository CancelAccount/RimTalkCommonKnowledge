using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RimTalkCommonKnowledge
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
    /// 页面支持条目级写操作：新增、编辑、删除条目，并可锁定本 mod 注入条目防止被自动刷新覆盖；
    /// 归集（勾选 → 自定义子页）仍只存 id 引用。
    /// </summary>
    public partial class Dialog_KnowledgeManager : Window
    {
        /// <summary>左侧导航的当前分类。</summary>
        private enum NavMode
        {
            /// <summary>库内全部条目。</summary>
            All,

            /// <summary>本 mod 注入的条目（内容带本 mod 前缀）。</summary>
            Own,

            /// <summary>本 mod 注入的派系信息条目（内容以派系前缀开头）。</summary>
            OwnFaction,

            /// <summary>本 mod 注入的异种人信息条目（内容以异种人前缀开头）。</summary>
            OwnXenotype,

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

        // ---------- 滑动批量涂抹状态（勾选列，D61 / D64） ----------
        // 启用 / 停用自 D64 起改走底部按钮（作用于 selectedIds），行内不再有开关列，
        // 故只需为勾选列维护滑动涂抹状态。

        /// <summary>勾选列滑动涂抹是否激活。</summary>
        private bool paintSelectActive;

        /// <summary>勾选列滑动涂抹的目标勾选值。</summary>
        private bool paintSelectState;

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

        /// <summary>库内本 mod 派系信息条目数（重建时统计，为 0 则不显示对应导航子行）。</summary>
        private int factionEntryCount;

        /// <summary>库内本 mod 异种人信息条目数（重建时统计，为 0 则不显示对应导航子行）。</summary>
        private int xenotypeEntryCount;

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

        /// <summary>是否处于「删除选中条目」确认状态。</summary>
        private bool confirmingDeleteEntries;

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
    }
}
