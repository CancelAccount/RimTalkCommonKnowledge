namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 日志模块前缀配置：集中管理「日志来自本 mod 内哪个模块」的模块名常量、染色与拼接格式。
    /// 最终每行日志的结构为：<c>[派系常识] [模块名] 消息正文</c>——
    /// mod 前缀（淡黄）见 <see cref="FactionKnowledgeConfig.LOG_PREFIX_COLORED"/>，
    /// 模块前缀（浅青蓝）由 <see cref="FormatModulePrefix"/> 生成，两级前缀均可在此一处调整。
    /// 模块名均为两字短词，避免日志行首过长；新增模块时先在此登记常量，业务代码只引用常量、不写裸字面量。
    /// </summary>
    public static class KnowledgeLogConfig
    {
        // ---------- 模块名（输出形态为 [模块名]）----------

        /// <summary>核心模块：mod 加载、启动横幅等 mod 级事件。</summary>
        public const string MODULE_CORE = "核心";

        /// <summary>注入模块：派系 / 异种人常识的写入时机与内容校正调度。</summary>
        public const string MODULE_INJECTION = "注入";

        /// <summary>构建模块：逐条常识内容的拼装逻辑。</summary>
        public const string MODULE_BUILDER = "构建";

        /// <summary>导入模块：随包预设库导入与来源索引。</summary>
        public const string MODULE_IMPORT = "导入";

        /// <summary>补丁模块：所有 Harmony 补丁的挂载与失效告警。</summary>
        public const string MODULE_PATCH = "补丁";

        /// <summary>调试模块：开发者菜单里的调试开关与按钮。</summary>
        public const string MODULE_DEBUG = "调试";

        // ---------- 模块前缀染色 ----------

        /// <summary>
        /// 模块前缀的染色值（RGB 168,230,255，浅青蓝）。
        /// 刻意区别于 mod 前缀的淡黄（<see cref="FactionKnowledgeConfig.LOG_PREFIX_COLOR"/>），
        /// 使日志窗口中两级前缀一眼可分。
        /// </summary>
        public const string MODULE_PREFIX_COLOR = "#FFFFC6";

        /// <summary>
        /// 生成染色后的模块前缀：给模块名套上方括号与浅青蓝染色，形如 <c>[注入]</c>。
        /// 与 mod 前缀同理：日志窗口按富文本渲染为浅青蓝；写入 Player.log 时保留方括号文本，
        /// 按「[模块名]」搜索仍可命中。
        /// </summary>
        /// <param name="module">模块名，须取本类的 <c>MODULE_*</c> 常量；传空则返回空串（不输出模块段）。</param>
        /// <returns>染色的模块前缀；<paramref name="module"/> 为空时返回 <see cref="string.Empty"/>。</returns>
        public static string FormatModulePrefix(string module)
        {
            if (string.IsNullOrEmpty(module))
            {
                return string.Empty;
            }

            return "<color=" + MODULE_PREFIX_COLOR + ">[" + module + "]</color>";
        }
    }
}
