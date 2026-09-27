using LudeonTK;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 调试工具（FR-12）：只在开发者模式菜单里暴露按钮，不依赖 ModSettings（S4 尚未实施）。
    /// 目前提供一个「快速刷新」开关——把**非质变**覆写的冷却从 3 天缩短到 1 小时，
    /// 便于在游戏内直接观察刷新行为，不必真等 3 天。
    /// 开关只存在于内存、**不随存档保存**，重启游戏即复位。
    /// </summary>
    public static class KnowledgeDebug
    {
        private static bool fastRefreshEnabled;

        /// <summary>「快速刷新」是否开启。</summary>
        public static bool FastRefreshEnabled
        {
            get { return fastRefreshEnabled; }
        }

        /// <summary>当前生效的非质变覆写冷却（tick）：调试开启时为 1 小时，否则 3 天。</summary>
        public static int CurrentRewriteCooldownTicks
        {
            get
            {
                return fastRefreshEnabled
                    ? FactionKnowledgeConfig.DEBUG_REWRITE_COOLDOWN_TICKS
                    : FactionKnowledgeConfig.REWRITE_COOLDOWN_TICKS;
            }
        }

        /// <summary>
        /// 开发者菜单按钮：切换「快速刷新」。
        /// 入口为开发者菜单 → <c>RimtalkAutoFactionInfo</c> → 切换快速刷新（覆写冷却 3天 ↔ 1小时）。
        /// </summary>
        [DebugAction("RimtalkAutoFactionInfo", "切换快速刷新（覆写冷却 3天 ↔ 1小时）",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ToggleFastRefresh()
        {
            fastRefreshEnabled = !fastRefreshEnabled;

            string cooldownText = fastRefreshEnabled
                ? "1 小时"
                : (FactionKnowledgeConfig.REWRITE_COOLDOWN_TICKS / FactionKnowledgeConfig.TICKS_PER_DAY) + " 天";

            KnowledgeLog.Summary(
                "快速刷新已" + (fastRefreshEnabled ? "开启" : "关闭") +
                "：非质变覆写冷却 = " + cooldownText + "。");
        }
    }
}
