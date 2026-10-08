using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 本 mod 的入口：<see cref="Verse.Mod"/> 子类由引擎在启动期反射实例化
    /// （<c>LoadedModManager.CreateModClasses</c>，构造签名必须为 <c>(ModContentPack)</c>）。
    /// 职责：① 载入可持久化设置（FR-7）；② 应用 Harmony 补丁，把「实际成员构成」追加到派系界面（FR-14）；
    /// ③ 提供设置页：注入开关、内容段开关、重要度与异种人选项、明细日志、界面增强，以及「立即重注入」按钮；
    /// ④ 启动横幅：构造期输出一次，表明本 mod 已被引擎加载（FR-8）。
    /// 与 <see cref="FactionKnowledgeComponent"/>（常识注入）分工明确：本类只管设置与界面增强，注入逻辑不在此。
    /// </summary>
    public class FactionInfoMod : Mod
    {
        /// <summary>
        /// 全局设置实例：构造时载入，供业务代码与补丁只读访问。
        /// 补丁理论上晚于本构造（补丁就在本构造里应用），但访问方仍做 null 兜底
        /// （见 <see cref="FactionInfoSettings"/> 的静态生效值属性）。
        /// </summary>
        public static FactionInfoSettings Settings;

        /// <summary>
        /// 本 mod 内容根目录（= <c>ModContentPack.RootDir</c>）：随包预设库 <c>KnowledgeBase/</c> 即坐落于此。
        /// 构造期立即赋值，供 <see cref="KnowledgeBaseImporter"/> 定位随包数据。
        /// </summary>
        public static string ContentRootDir;

        /// <summary>本 mod 的 Harmony 实例，持有补丁集以便排查冲突。</summary>
        private readonly Harmony harmony;

        /// <summary>设置页的滚动位置。设置项较多，一屏放不下，故整体放入滚动视图。</summary>
        private Vector2 settingsScrollPosition;

        /// <summary>引擎反射实例化入口：载入设置后立即应用补丁。</summary>
        /// <param name="content">本 mod 的内容包。</param>
        public FactionInfoMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FactionInfoSettings>();
            ContentRootDir = content != null ? content.RootDir : null;

            // 启动日志：主菜单阶段即输出，确认本 mod 已被引擎加载（FR-8）；
            // 置于 PatchAll 之前，即便补丁失败也能证明 mod 类已被反射实例化。
            KnowledgeLog.Startup();

            harmony = new Harmony(FactionKnowledgeConfig.HARMONY_ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // 上游常识库界面的绘制裁剪补丁（D48）：目标位于上游程序集，不能靠 PatchAll 的静态标注，
            // 且必须在目标缺失时安全跳过，故在此手动、运行期应用。
            CommonKnowledgeUiOptimization.TryApply(harmony);

            // 上游常识库界面的「常识管理」入口按钮（FR-17）：同上，目标在上游程序集，运行期安全应用。
            CommonKnowledgeManagerEntryPatch.TryApply(harmony);
        }

        /// <summary>Mod 设置页的分类名（返回空串会使本 mod 从设置列表消失）。</summary>
        public override string SettingsCategory()
        {
            return FactionKnowledgeConfig.MOD_SETTINGS_CATEGORY;
        }

        /// <summary>
        /// Mod 设置页内容：按功能分组的注入选项（FR-7）+ 派系界面开关（FR-14）+ 立即重注入按钮。
        /// 所有控件直接读写 <see cref="Settings"/> 的字段；设置由引擎在关闭设置窗时落盘
        /// （<c>ModSettings.ExposeData</c>），此处无需手动保存。
        /// </summary>
        /// <param name="inRect">设置页可用区域。</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 内容高于可视区域，故先声明一块固定高度的画布再滚动；
            // 高度取常量（MOD_SETTINGS_CONTENT_HEIGHT），新增设置项时须同步调大。
            Rect viewRect = new Rect(
                0f,
                0f,
                inRect.width - FactionKnowledgeConfig.MOD_SETTINGS_SCROLLBAR_WIDTH,
                FactionKnowledgeConfig.MOD_SETTINGS_CONTENT_HEIGHT);

            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            AppendInjectionSection(listing);
            AppendFactionSectionToggles(listing);
            AppendImportanceSection(listing);
            AppendXenotypeSection(listing);
            AppendLogSection(listing);
            AppendUiSection(listing);
            AppendReinjectButton(listing);

            listing.End();
            Widgets.EndScrollView();
        }

        /// <summary>「注入时机」分组：总开关、读档校正、定时校正、两个数值滑块，以及预设库导入两个开关（FR-6）。</summary>
        /// <param name="listing">设置页列表。</param>
        private void AppendInjectionSection(Listing_Standard listing)
        {
            AppendSectionHeader(listing, FactionKnowledgeConfig.MOD_SETTINGS_SECTION_INJECTION);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_INJECTION,
                ref Settings.enableInjection,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_INJECTION_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_BACKFILL_ON_LOAD,
                ref Settings.enableBackfillOnLoad,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_BACKFILL_ON_LOAD_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_PERIODIC_REFRESH,
                ref Settings.enablePeriodicRefresh,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_PERIODIC_REFRESH_TIP);

            Settings.refreshIntervalHours = AppendIntSlider(
                listing,
                FactionKnowledgeConfig.MOD_SETTINGS_REFRESH_INTERVAL_HOURS,
                Settings.refreshIntervalHours,
                FactionKnowledgeConfig.REFRESH_INTERVAL_HOURS_MIN,
                FactionKnowledgeConfig.REFRESH_INTERVAL_HOURS_MAX,
                FactionKnowledgeConfig.MOD_SETTINGS_REFRESH_INTERVAL_HOURS_TIP,
                FactionKnowledgeConfig.MOD_SETTINGS_UNIT_HOURS);

            Settings.goodwillRefreshThreshold = AppendIntSlider(
                listing,
                FactionKnowledgeConfig.MOD_SETTINGS_GOODWILL_REFRESH_THRESHOLD,
                Settings.goodwillRefreshThreshold,
                FactionKnowledgeConfig.GOODWILL_THRESHOLD_MIN,
                FactionKnowledgeConfig.GOODWILL_THRESHOLD_MAX,
                FactionKnowledgeConfig.MOD_SETTINGS_GOODWILL_REFRESH_THRESHOLD_TIP,
                FactionKnowledgeConfig.MOD_SETTINGS_UNIT_NONE);

            // 预设库导入（FR-6 / D44 / D45）：mod 块开关默认开、本体块开关默认关，各自独立。
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_KNOWLEDGE_BASE_IMPORT,
                ref Settings.enableKnowledgeBaseImport,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_KNOWLEDGE_BASE_IMPORT_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_BUILTIN_KNOWLEDGE_IMPORT,
                ref Settings.enableBuiltinKnowledgeImport,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_BUILTIN_KNOWLEDGE_IMPORT_TIP);
        }

        /// <summary>「派系常识内容段」分组：四个段开关（普通派系；我方派系条目固定写全）。</summary>
        /// <param name="listing">设置页列表。</param>
        private void AppendFactionSectionToggles(Listing_Standard listing)
        {
            AppendSectionHeader(listing, FactionKnowledgeConfig.MOD_SETTINGS_SECTION_FACTION_SECTIONS);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_IDENTITY,
                ref Settings.includeIdentity,
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_IDENTITY_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_IDEOLOGY,
                ref Settings.includeIdeology,
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_IDEOLOGY_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_RELATION,
                ref Settings.includeRelation,
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_RELATION_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_SETTLEMENTS,
                ref Settings.includeSettlements,
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_SETTLEMENTS_TIP);
        }

        /// <summary>
        /// 「重要度与分类」分组：我方派系 / 其它派系与异种人 两个重要度滑块 + 分类固定开关。
        /// 分两档是为了对齐社区常识库的档位（0.95 顶级 / 0.80 派系本体），见 D43。
        /// </summary>
        /// <param name="listing">设置页列表。</param>
        private void AppendImportanceSection(Listing_Standard listing)
        {
            AppendSectionHeader(listing, FactionKnowledgeConfig.MOD_SETTINGS_SECTION_IMPORTANCE);

            Settings.knowledgeImportancePlayer = AppendFloatSlider(
                listing,
                FactionKnowledgeConfig.MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_PLAYER,
                Settings.knowledgeImportancePlayer,
                FactionKnowledgeConfig.IMPORTANCE_MIN,
                FactionKnowledgeConfig.IMPORTANCE_MAX,
                FactionKnowledgeConfig.MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_PLAYER_TIP,
                FactionKnowledgeConfig.MOD_SETTINGS_UNIT_NONE);

            Settings.knowledgeImportanceOther = AppendFloatSlider(
                listing,
                FactionKnowledgeConfig.MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_OTHER,
                Settings.knowledgeImportanceOther,
                FactionKnowledgeConfig.IMPORTANCE_MIN,
                FactionKnowledgeConfig.IMPORTANCE_MAX,
                FactionKnowledgeConfig.MOD_SETTINGS_KNOWLEDGE_IMPORTANCE_OTHER_TIP,
                FactionKnowledgeConfig.MOD_SETTINGS_UNIT_NONE);

            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_CATEGORY_ALWAYS_LORE,
                ref Settings.categoryAlwaysLore,
                FactionKnowledgeConfig.MOD_SETTINGS_CATEGORY_ALWAYS_LORE_TIP);
        }

        /// <summary>「异种人常识」分组：独立条目开关、双向引用开关与基因条数滑块。</summary>
        /// <param name="listing">设置页列表。</param>
        private void AppendXenotypeSection(Listing_Standard listing)
        {
            AppendSectionHeader(listing, FactionKnowledgeConfig.MOD_SETTINGS_SECTION_XENOTYPE);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_XENOTYPES,
                ref Settings.includeXenotypes,
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_XENOTYPES_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_FACTION_XENOTYPE_REFS,
                ref Settings.includeFactionXenotypeRefs,
                FactionKnowledgeConfig.MOD_SETTINGS_INCLUDE_FACTION_XENOTYPE_REFS_TIP);

            Settings.xenotypeGeneCount = AppendIntSlider(
                listing,
                FactionKnowledgeConfig.MOD_SETTINGS_XENOTYPE_GENE_COUNT,
                Settings.xenotypeGeneCount,
                FactionKnowledgeConfig.XENOTYPE_GENE_COUNT_MIN,
                FactionKnowledgeConfig.XENOTYPE_GENE_COUNT_MAX,
                FactionKnowledgeConfig.MOD_SETTINGS_XENOTYPE_GENE_COUNT_TIP,
                FactionKnowledgeConfig.MOD_SETTINGS_UNIT_ENTRIES);
        }

        /// <summary>「日志」分组：明细日志开关。</summary>
        /// <param name="listing">设置页列表。</param>
        private void AppendLogSection(Listing_Standard listing)
        {
            AppendSectionHeader(listing, FactionKnowledgeConfig.MOD_SETTINGS_SECTION_LOG);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_VERBOSE_LOG,
                ref Settings.enableVerboseLog,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_VERBOSE_LOG_TIP);
        }

        /// <summary>
        /// 「界面增强」分组：派系界面实际成员构成开关（FR-14）、上游常识库界面滚动优化开关（D48），
        /// 以及常识管理页面入口按钮（FR-16 / D49）。
        /// </summary>
        /// <param name="listing">设置页列表。</param>
        private void AppendUiSection(Listing_Standard listing)
        {
            AppendSectionHeader(listing, FactionKnowledgeConfig.MOD_SETTINGS_SECTION_UI);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_SHOW_UI_COMPOSITION,
                ref Settings.showCompositionInFactionUi,
                FactionKnowledgeConfig.MOD_SETTINGS_SHOW_UI_COMPOSITION_TIP);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_KNOWLEDGE_UI_OPTIMIZATION,
                ref Settings.enableKnowledgeUiOptimization,
                FactionKnowledgeConfig.MOD_SETTINGS_ENABLE_KNOWLEDGE_UI_OPTIMIZATION_TIP);
            AppendKnowledgeManagerButton(listing);
        }

        /// <summary>
        /// 「打开常识管理页面」按钮（FR-16 / D49）：唤出本 mod 自建的常识管理窗口。
        /// 无存档时按钮置灰（上游常识库在无存档时不可用），并在悬停提示里说明原因。
        /// </summary>
        /// <param name="listing">设置页列表。</param>
        private static void AppendKnowledgeManagerButton(Listing_Standard listing)
        {
            Rect rect = listing.GetRect(FactionKnowledgeConfig.MOD_SETTINGS_BUTTON_HEIGHT);
            bool available = RimTalkMemoryBridge.IsLibraryAvailable;

            if (Widgets.ButtonText(
                rect, FactionKnowledgeConfig.MOD_SETTINGS_OPEN_MANAGER, true, true, available, null))
            {
                Find.WindowStack.Add(new Dialog_KnowledgeManager());
            }

            TooltipHandler.TipRegion(
                rect,
                available
                    ? FactionKnowledgeConfig.MOD_SETTINGS_OPEN_MANAGER_TIP
                    : FactionKnowledgeConfig.MOD_SETTINGS_OPEN_MANAGER_TIP
                        + FactionKnowledgeConfig.MANAGER_TIP_BLOCK_SEPARATOR
                        + FactionKnowledgeConfig.MANAGER_NO_GAME);
            listing.Gap();
        }

        /// <summary>
        /// 「立即对当前存档重新注入」按钮：对当前存档重跑一次注入与校正，便于改完设置立刻看效果。
        /// 无存档时按钮置灰，并在悬停提示里说明原因。
        /// </summary>
        /// <param name="listing">设置页列表。</param>
        private static void AppendReinjectButton(Listing_Standard listing)
        {
            Rect rect = listing.GetRect(FactionKnowledgeConfig.MOD_SETTINGS_BUTTON_HEIGHT);
            bool available = Current.Game != null;

            if (Widgets.ButtonText(
                rect, FactionKnowledgeConfig.MOD_SETTINGS_REINJECT, true, true, available, null))
            {
                FactionKnowledgeComponent.ReinjectCurrentGame();
            }
            else if (!available)
            {
                TooltipHandler.TipRegion(rect, FactionKnowledgeConfig.MOD_SETTINGS_REINJECT_NO_GAME_TIP);
            }
            listing.Gap();
        }

        /// <summary>绘制分组标题：先留一段间隔，再以正文行呈现。</summary>
        /// <param name="listing">设置页列表。</param>
        /// <param name="title">分组标题。</param>
        private static void AppendSectionHeader(Listing_Standard listing, string title)
        {
            listing.Gap(FactionKnowledgeConfig.MOD_SETTINGS_SECTION_GAP);
            listing.Label(title);
        }

        /// <summary>
        /// 整数滑块行：标题里带上当前值，返回值取整。
        /// 上游 <c>SliderLabeled</c> 返回的是滑块上的连续浮点值，故必须四舍五入到整数，
        /// 否则会出现 3.9997 这种残值被写进设置。
        /// </summary>
        /// <param name="listing">设置页列表。</param>
        /// <param name="label">设置项标题。</param>
        /// <param name="value">当前值。</param>
        /// <param name="min">最小值。</param>
        /// <param name="max">最大值。</param>
        /// <param name="tip">悬停说明。</param>
        /// <param name="unit">单位后缀。</param>
        /// <returns>用户调整后的新值（整数）。</returns>
        private static int AppendIntSlider(
            Listing_Standard listing, string label, int value, int min, int max, string tip, string unit)
        {
            string labeled = string.Format(
                FactionKnowledgeConfig.MOD_SETTINGS_SLIDER_LABEL_FORMAT, label, value, unit);
            float result = listing.SliderLabeled(
                labeled, value, min, max, FactionKnowledgeConfig.MOD_SETTINGS_SLIDER_LABEL_PCT, tip);
            return Mathf.RoundToInt(result);
        }

        /// <summary>
        /// 百分比滑块行：标题里带上当前值，返回值按百分位取整。
        /// 上游 <c>SliderLabeled</c> 返回的是连续浮点值，而标题按百分比显示，
        /// 故取整到 1% 粒度，避免出现「显示 95%、实际存 0.9537」这类表里不一。
        /// </summary>
        /// <param name="listing">设置页列表。</param>
        /// <param name="label">设置项标题。</param>
        /// <param name="value">当前值（0~1）。</param>
        /// <param name="min">最小值。</param>
        /// <param name="max">最大值。</param>
        /// <param name="tip">悬停说明。</param>
        /// <param name="unit">单位后缀。</param>
        /// <returns>用户调整后的新值（0~1，百分位取整）。</returns>
        private static float AppendFloatSlider(
            Listing_Standard listing, string label, float value, float min, float max, string tip, string unit)
        {
            string labeled = string.Format(
                FactionKnowledgeConfig.MOD_SETTINGS_SLIDER_LABEL_FORMAT,
                label, value.ToStringPercent(), unit);
            float result = listing.SliderLabeled(
                labeled, value, min, max, FactionKnowledgeConfig.MOD_SETTINGS_SLIDER_LABEL_PCT, tip);
            return Mathf.Round(result * 100f) / 100f;
        }
    }
}
