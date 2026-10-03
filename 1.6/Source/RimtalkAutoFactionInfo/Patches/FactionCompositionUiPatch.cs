using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 派系界面「实际成员」补丁的共用逻辑：把 <see cref="FactionKnowledgeBuilder.BuildUiCompositionBlock"/>
    /// 的文本追加到派系介绍末尾，修正游戏自带成员统计在 HAR 种族 / 兵种级异种人派系上的误报。
    /// </summary>
    /// <remarks>
    /// 补丁宿主有两个，分别对应两处界面文本源，二者共用同一构建器与设置开关：
    /// ① <c>FactionDef.Description</c>（属性）—— 派系列表行悬停提示（<c>FactionUIUtility</c>）、
    ///    世界派系界面（<c>WorldFactionsUIUtility</c>）、奖励配置（<c>Dialog_RewardPrefsConfig</c>）；
    /// ② <c>Faction.GetReportText</c>（属性）—— 派系信息卡（<c>Dialog_InfoCard</c>）的「描述」条目
    ///    （它读 <c>def.description</c> **字段**，本就不含游戏自带的成员统计段，故同样需要补）。
    /// ⚠ 常识注入读的是 <c>def.description</c> 字段、不经这两个属性，故本补丁不影响注入内容。
    /// </remarks>
    internal static class FactionCompositionUi
    {
        /// <summary>是否显示：设置尚未载入时按「显示」处理（默认开）。</summary>
        public static bool Enabled
        {
            get
            {
                FactionInfoSettings settings = FactionInfoMod.Settings;
                return settings == null || settings.showCompositionInFactionUi;
            }
        }

        /// <summary>
        /// 把构成块追加到原有介绍文本末尾；块为空时原样返回，原文为空时直接返回块（避免前导空行）。
        /// </summary>
        /// <param name="original">游戏原本的介绍文本，可为 <c>null</c>。</param>
        /// <param name="block">本 mod 的构成块，可为 <c>null</c>。</param>
        public static string Append(string original, string block)
        {
            if (string.IsNullOrEmpty(block))
            {
                return original;
            }
            return string.IsNullOrEmpty(original) ? block : original + "\n\n" + block;
        }

        /// <summary>
        /// 安全构建构成块：任何异常都降级为「本次不追加」，绝不把异常抛进界面绘制——
        /// 这两处属性每帧都可能被 tooltip 触发，异常会连累整个派系页并刷屏。
        /// </summary>
        /// <param name="def">目标派系 Def，可为 <c>null</c>。</param>
        public static string BuildSafely(FactionDef def)
        {
            try
            {
                return FactionKnowledgeBuilder.BuildUiCompositionBlock(def);
            }
            catch (Exception exception)
            {
                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_UI_COMPOSITION_FAILED,
                    "构建派系界面「实际成员」失败，本次不追加：" + exception);
                return null;
            }
        }
    }

    /// <summary>补丁宿主①：<c>FactionDef.Description</c> 属性 getter（派系列表悬停提示等）。</summary>
    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.Description), MethodType.Getter)]
    public static class FactionDefDescriptionPatch
    {
        /// <summary>在游戏原本的描述末尾追加「实际成员」块。</summary>
        /// <param name="__instance">被访问的派系 Def。</param>
        /// <param name="__result">原返回值（引用传递，直接改写即可）。</param>
        public static void Postfix(FactionDef __instance, ref string __result)
        {
            if (!FactionCompositionUi.Enabled)
            {
                return;
            }
            __result = FactionCompositionUi.Append(
                __result, FactionCompositionUi.BuildSafely(__instance));
        }
    }

    /// <summary>补丁宿主②：<c>Faction.GetReportText</c> 属性 getter（派系信息卡的「描述」条目）。</summary>
    [HarmonyPatch(typeof(Faction), nameof(Faction.GetReportText), MethodType.Getter)]
    public static class FactionReportTextPatch
    {
        /// <summary>在游戏原本的描述末尾追加「实际成员」块。</summary>
        /// <param name="__instance">被访问的派系。</param>
        /// <param name="__result">原返回值（引用传递，直接改写即可）。</param>
        public static void Postfix(Faction __instance, ref string __result)
        {
            if (!FactionCompositionUi.Enabled)
            {
                return;
            }
            __result = FactionCompositionUi.Append(
                __result,
                FactionCompositionUi.BuildSafely(__instance != null ? __instance.def : null));
        }
    }
}
