using System;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 统一日志入口：本 mod 所有日志必须经此类输出，保证前缀、语言与分级口径一致。
    /// 每行结构为 <c>[派系常识] [模块名] 消息正文</c>：mod 前缀标识本 mod，模块前缀标识来源子系统
    /// （模块名常量与染色统一在 <see cref="KnowledgeLogConfig"/> 维护）。
    /// 分级口径：
    /// 汇总级 Summary 无条件输出；明细级 Detail 仅在 VerboseEnabled 为真时输出；
    /// 告警 Warn / WarnOnce 与错误 Error 始终输出。
    /// </summary>
    public static class KnowledgeLog
    {
        /// <summary>日志前缀别名（含淡黄染色），避免每行都写全限定名。</summary>
        private const string Prefix = FactionKnowledgeConfig.LOG_PREFIX_COLORED;

        /// <summary>
        /// 是否输出明细日志。门控只看**包类型**与**设置项**，与开发者模式（Prefs.DevMode）**无关**：
        /// Debug 包默认输出明细；Release 包仅在设置项 <c>enableVerboseLog</c> 开启时才输出，
        /// 否则 Release 下只保留汇总级（条目总量与计数），不写具体内容。
        /// 直接读设置而非缓存字段：设置页勾选后立即生效，无需额外同步。
        /// </summary>
        public static bool VerboseEnabled
        {
            get { return FactionKnowledgeConfig.IS_DEBUG_BUILD || FactionInfoSettings.EnableVerboseLog; }
        }

        /// <summary>
        /// 汇总级日志：单次注入的整体结果，无条件输出，每次注入只应调用一次。
        /// </summary>
        /// <param name="module">来源模块名，取 <see cref="KnowledgeLogConfig"/> 的 <c>MODULE_*</c> 常量。</param>
        /// <param name="message">日志正文。</param>
        public static void Summary(string module, string message)
        {
            Log.Message(BuildLine(module, message));
        }

        /// <summary>
        /// 启动横幅：由 <see cref="FactionInfoMod"/> 在构造期（主菜单阶段）调用一次，
        /// 表明本 mod 已被引擎加载，并报出本次运行是 Debug 包还是 Release 包（FR-8）。
        /// 与「进档后才有的注入汇总」分开，便于一眼确认 mod 是否真的被加载。
        /// 横幅属 mod 级事件，模块段固定为 <see cref="KnowledgeLogConfig.MODULE_CORE"/>。
        /// </summary>
        public static void Startup()
        {
            Summary(KnowledgeLogConfig.MODULE_CORE, string.Format(
                FactionKnowledgeConfig.LOG_MOD_BUILD,
                FactionKnowledgeConfig.IS_DEBUG_BUILD
                    ? FactionKnowledgeConfig.MOD_BUILD_DEBUG
                    : FactionKnowledgeConfig.MOD_BUILD_RELEASE));
        }

        /// <summary>
        /// 明细级日志：逐派系 / 逐异种人的细节，仅调试时输出。
        /// </summary>
        /// <param name="module">来源模块名，取 <see cref="KnowledgeLogConfig"/> 的 <c>MODULE_*</c> 常量。</param>
        /// <param name="message">日志正文。</param>
        public static void Detail(string module, string message)
        {
            if (VerboseEnabled)
            {
                Log.Message(BuildLine(module, message));
            }
        }

        /// <summary>
        /// 告警：可恢复的异常情况，始终输出。
        /// </summary>
        /// <param name="module">来源模块名，取 <see cref="KnowledgeLogConfig"/> 的 <c>MODULE_*</c> 常量。</param>
        /// <param name="message">日志正文。</param>
        public static void Warn(string module, string message)
        {
            Log.Warning(BuildLine(module, message));
        }

        /// <summary>
        /// 一次性告警：同一 key 在本次运行中只输出一次，用于避免同类告警刷屏。
        /// 去重由上游 Log.WarningOnce 实现，key 须取自常量而非裸数字。
        /// </summary>
        /// <param name="module">来源模块名，取 <see cref="KnowledgeLogConfig"/> 的 <c>MODULE_*</c> 常量。</param>
        /// <param name="key">去重键，取 <see cref="FactionKnowledgeConfig"/> 的 <c>LOG_KEY_*</c> 常量。</param>
        /// <param name="message">日志正文。</param>
        public static void WarnOnce(string module, int key, string message)
        {
            Log.WarningOnce(BuildLine(module, message), key);
        }

        /// <summary>
        /// 错误：需要开发者介入的问题，始终输出；调用方须保证单个对象失败不中断整体流程。
        /// </summary>
        /// <param name="module">来源模块名，取 <see cref="KnowledgeLogConfig"/> 的 <c>MODULE_*</c> 常量。</param>
        /// <param name="message">日志正文。</param>
        /// <param name="exception">关联异常；为 <c>null</c> 时不追加异常信息。</param>
        public static void Error(string module, string message, Exception exception = null)
        {
            Log.Error(BuildLine(module, message + (exception == null ? string.Empty : " —— " + exception)));
        }

        /// <summary>
        /// 拼装完整日志行：mod 前缀（淡黄）+ 模块前缀（浅青蓝）+ 正文，两段前缀间各留一个空格。
        /// </summary>
        /// <param name="module">来源模块名，取 <see cref="KnowledgeLogConfig"/> 的 <c>MODULE_*</c> 常量。</param>
        /// <param name="message">日志正文（含已拼好的异常信息）。</param>
        private static string BuildLine(string module, string message)
        {
            return Prefix + " " + KnowledgeLogConfig.FormatModulePrefix(module) + " " + message;
        }
    }
}
