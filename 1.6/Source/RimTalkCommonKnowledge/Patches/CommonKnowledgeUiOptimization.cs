using System;
using System.Reflection;
using HarmonyLib;
using RimTalk.Memory;
using RimTalk.Memory.UI;
using UnityEngine;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 上游常识库界面（<see cref="Dialog_CommonKnowledge"/>）的绘制裁剪补丁（D48）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 问题：上游中心列表已把虚拟列表控件 <c>VirtualListView</c> 建好、也每帧 <c>SetItems</c>，
    /// 却从未调用其 <c>Draw</c>，而是手写 <c>foreach</c> 把**全部**条目逐行绘制。
    /// IMGUI 不做自动裁剪，条目成千后每帧要跑上千次控件绘制，打开与滚动都会明显卡顿。
    /// </para>
    /// <para>
    /// 做法：只加两个前缀补丁——在中心列表入口记下「本帧滚动视口」（内容坐标的上 / 下边界），
    /// 在单行绘制入口判断该行是否落在视口内，视口外的行直接跳过绘制。
    /// 不改上游任何逻辑、不改变可见行的绘制顺序与控件参数，故界面表现与上游一致。
    /// </para>
    /// <para>
    /// 安全：目标方法或字段缺失（上游改名 / 移除）时只记一次警告并放弃打补丁，绝不抛异常；
    /// 逐行判定里的任何异常一律降级为「照常绘制」。玩家可在设置里关闭本优化。
    /// </para>
    /// </remarks>
    internal static class CommonKnowledgeUiOptimization
    {
        /// <summary>上游中心列表的滚动位置字段（运行期解析；未解析到时为 <c>null</c>，补丁整体不生效）。</summary>
        private static FieldInfo scrollPositionField;

        /// <summary>本帧滚动视口的上边界（内容坐标）。</summary>
        private static float viewportTop;

        /// <summary>本帧滚动视口的下边界（内容坐标）。</summary>
        private static float viewportBottom;

        /// <summary>本帧是否已记下视口；为 <c>false</c> 时不做裁剪（照常绘制）。</summary>
        private static bool viewportKnown;

        /// <summary>生效开关：设置未载入时按默认「开」处理。</summary>
        private static bool Enabled
        {
            get { return FactionInfoSettings.EnableKnowledgeUiOptimization; }
        }

        /// <summary>
        /// 解析上游目标并打补丁。由 <see cref="FactionInfoMod"/> 在构造期调用一次；
        /// 任何失败都只记一次警告，不影响本 mod 其余功能。
        /// </summary>
        /// <param name="harmony">本 mod 的 Harmony 实例。</param>
        public static void TryApply(Harmony harmony)
        {
            try
            {
                Type dialogType = typeof(Dialog_CommonKnowledge);
                MethodInfo drawCenterList = AccessTools.Method(
                    dialogType,
                    FactionKnowledgeConfig.UPSTREAM_UI_METHOD_DRAW_CENTER_LIST,
                    new[] { typeof(Rect) });
                MethodInfo drawEntryRow = AccessTools.Method(
                    dialogType,
                    FactionKnowledgeConfig.UPSTREAM_UI_METHOD_DRAW_ENTRY_ROW,
                    new[] { typeof(Rect), typeof(CommonKnowledgeEntry) });
                FieldInfo scrollField = AccessTools.Field(
                    dialogType,
                    FactionKnowledgeConfig.UPSTREAM_UI_FIELD_LIST_SCROLL_POSITION);

                if (drawCenterList == null || drawEntryRow == null || scrollField == null)
                {
                    WarnPatchFailed(FactionKnowledgeConfig.UPSTREAM_UI_PATCH_TARGET_MISSING);
                    return;
                }

                scrollPositionField = scrollField;

                harmony.Patch(
                    drawCenterList,
                    prefix: new HarmonyMethod(
                        typeof(CommonKnowledgeUiOptimization), nameof(CaptureViewportPrefix)));
                harmony.Patch(
                    drawEntryRow,
                    prefix: new HarmonyMethod(
                        typeof(CommonKnowledgeUiOptimization), nameof(SkipInvisibleRowPrefix)));

                KnowledgeLog.Detail(
                    KnowledgeLogConfig.MODULE_PATCH,
                    "常识库界面优化补丁已应用：仅绘制滚动视口内的条目行。");
            }
            catch (Exception exception)
            {
                WarnPatchFailed(exception.Message);
            }
        }

        /// <summary>记一次「补丁未生效」告警（同 key 只输出一次）。</summary>
        /// <param name="detail">失败细节（异常摘要或目标缺失说明）。</param>
        private static void WarnPatchFailed(string detail)
        {
            scrollPositionField = null;
            KnowledgeLog.WarnOnce(
                KnowledgeLogConfig.MODULE_PATCH,
                FactionKnowledgeConfig.LOG_KEY_COMMON_KNOWLEDGE_UI_PATCH_FAILED,
                string.Format(FactionKnowledgeConfig.WARN_COMMON_KNOWLEDGE_UI_PATCH_FAILED, detail));
        }

        /// <summary>
        /// 中心列表前缀：记下本帧滚动视口的内容坐标范围，供逐行判定使用。
        /// 滚动值每帧只读一次（反射），避免在逐行判定里重复取值。
        /// </summary>
        /// <param name="rect">中心列表外框（屏幕坐标）。</param>
        /// <param name="__instance">被补丁的对话框实例。</param>
        private static void CaptureViewportPrefix(Rect rect, Dialog_CommonKnowledge __instance)
        {
            try
            {
                if (!Enabled || scrollPositionField == null)
                {
                    viewportKnown = false;
                    return;
                }

                float scrollY = ((Vector2)scrollPositionField.GetValue(__instance)).y;
                viewportTop = scrollY;
                viewportBottom = scrollY + rect.height - FactionKnowledgeConfig.UPSTREAM_UI_VIEWPORT_PADDING;
                viewportKnown = true;
            }
            catch (Exception)
            {
                // 视口取不到就退回「不裁剪」，界面最多是慢，不会错。
                viewportKnown = false;
            }
        }

        /// <summary>
        /// 单行前缀：行完全落在滚动视口之外时返回 <c>false</c>，跳过该行的绘制。
        /// 视口内（含边界余量）的行照常绘制，故界面表现与上游一致。
        /// </summary>
        /// <param name="rect">该行的绘制矩形（内容坐标：y 从 0 起、随滚动不变）。</param>
        /// <returns><c>true</c>=照常绘制；<c>false</c>=跳过绘制。</returns>
        private static bool SkipInvisibleRowPrefix(Rect rect)
        {
            try
            {
                if (!viewportKnown)
                {
                    return true;
                }

                float margin = FactionKnowledgeConfig.UPSTREAM_UI_CULL_MARGIN;
                if (rect.yMax < viewportTop - margin)
                {
                    return false;
                }
                return rect.y <= viewportBottom + margin;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
