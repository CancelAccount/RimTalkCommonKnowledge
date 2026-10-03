using System;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 统一日志入口：本 mod 所有日志必须经此类输出，保证前缀、语言与分级口径一致。
    /// 分级口径：
    /// 汇总级 Summary 无条件输出；明细级 Detail 仅在 VerboseEnabled 为真时输出；
    /// 告警 Warn / WarnOnce 与错误 Error 始终输出。
    /// </summary>
    public static class KnowledgeLog
    {
        /// <summary>日志前缀别名，避免每行都写全限定名。</summary>
        private const string Prefix = FactionKnowledgeConfig.LOG_PREFIX;

        /// <summary>明细日志开关，由设置界面写入。</summary>
        private static bool verboseFlag;

        /// <summary>
        /// 是否输出明细日志。
        /// 开发者模式（Prefs.DevMode）开启时强制为真，便于临时排查；否则取设置值。
        /// </summary>
        public static bool VerboseEnabled
        {
            get { return verboseFlag || Prefs.DevMode; }
            set { verboseFlag = value; }
        }

        /// <summary>汇总级日志：单次注入的整体结果，无条件输出，每次注入只应调用一次。</summary>
        public static void Summary(string message)
        {
            Log.Message(Prefix + " " + message);
        }

        /// <summary>明细级日志：逐派系 / 逐异种人的细节，仅调试时输出。</summary>
        public static void Detail(string message)
        {
            if (VerboseEnabled)
            {
                Log.Message(Prefix + " " + message);
            }
        }

        /// <summary>告警：可恢复的异常情况，始终输出。</summary>
        public static void Warn(string message)
        {
            Log.Warning(Prefix + " " + message);
        }

        /// <summary>
        /// 一次性告警：同一 key 在本次运行中只输出一次，用于避免同类告警刷屏。
        /// 去重由上游 Log.WarningOnce 实现，key 须取自常量而非裸数字。
        /// </summary>
        public static void WarnOnce(int key, string message)
        {
            Log.WarningOnce(Prefix + " " + message, key);
        }

        /// <summary>错误：需要开发者介入的问题，始终输出；调用方须保证单个对象失败不中断整体流程。</summary>
        public static void Error(string message, Exception exception = null)
        {
            Log.Error(Prefix + " " + message + (exception == null ? string.Empty : " —— " + exception));
        }
    }
}
