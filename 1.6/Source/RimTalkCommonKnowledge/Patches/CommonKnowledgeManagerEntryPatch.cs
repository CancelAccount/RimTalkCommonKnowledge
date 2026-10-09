using System;
using System.Reflection;
using HarmonyLib;
using RimTalk.Memory.UI;
using UnityEngine;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 在上游常识库界面（<see cref="Dialog_CommonKnowledge"/>）的工具栏里追加入口按钮，
    /// 点击打开本 mod 的常识管理页面（<see cref="Dialog_KnowledgeManager"/>，FR-16 / FR-17）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 宿主为上游工具栏绘制方法 <c>Dialog_CommonKnowledge.DrawToolbar(Rect)</c>。用一个后缀补丁，
    /// 等上游把搜索框与右侧按钮组画完后，在搜索框右侧的空白处补一个按钮——不改上游布局与事件处理。
    /// </para>
    /// <para>
    /// 与 <see cref="CommonKnowledgeUiOptimization"/> 同一套安全策略：运行期解析目标方法（上游改名 / 移除
    /// 即自动跳过），未打上补丁时不绘制按钮，且绘制内的任何异常一律吞掉——绝不把异常抛进上游界面。
    /// </para>
    /// </remarks>
    internal static class CommonKnowledgeManagerEntryPatch
    {
        /// <summary>是否已成功打上补丁；为 <c>false</c> 时不绘制按钮。</summary>
        private static bool applied;

        /// <summary>
        /// 解析上游工具栏方法并打后缀补丁。由 <see cref="FactionInfoMod"/> 在构造期调用一次；
        /// 任何失败都只记一次警告，不影响本 mod 其余功能。
        /// </summary>
        /// <param name="harmony">本 mod 的 Harmony 实例。</param>
        public static void TryApply(Harmony harmony)
        {
            try
            {
                MethodInfo drawToolbar = AccessTools.Method(
                    typeof(Dialog_CommonKnowledge),
                    FactionKnowledgeConfig.UPSTREAM_UI_METHOD_DRAW_TOOLBAR,
                    new[] { typeof(Rect) });

                if (drawToolbar == null)
                {
                    WarnPatchFailed(FactionKnowledgeConfig.UPSTREAM_UI_PATCH_TARGET_MISSING);
                    return;
                }

                harmony.Patch(
                    drawToolbar,
                    postfix: new HarmonyMethod(
                        typeof(CommonKnowledgeManagerEntryPatch), nameof(DrawEntryButtonPostfix)));

                applied = true;
                KnowledgeLog.Detail(
                    KnowledgeLogConfig.MODULE_PATCH,
                    "常识库界面入口按钮补丁已应用：工具栏新增「常识管理」按钮。");
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
            applied = false;
            KnowledgeLog.WarnOnce(
                KnowledgeLogConfig.MODULE_PATCH,
                FactionKnowledgeConfig.LOG_KEY_COMMON_KNOWLEDGE_MANAGER_BUTTON_PATCH_FAILED,
                string.Format(
                    FactionKnowledgeConfig.WARN_COMMON_KNOWLEDGE_MANAGER_BUTTON_PATCH_FAILED, detail));
        }

        /// <summary>
        /// 工具栏后缀：在搜索框右侧的空白处绘制「常识管理」按钮并挂悬停说明；
        /// 点击时打开本 mod 的管理窗口（已打开则不再重复开窗）。
        /// </summary>
        /// <param name="rect">上游工具栏外框（内容坐标，高 45）。</param>
        private static void DrawEntryButtonPostfix(Rect rect)
        {
            if (!applied)
            {
                return;
            }

            try
            {
                Rect buttonRect = new Rect(
                    rect.x + FactionKnowledgeConfig.UPSTREAM_UI_MANAGER_BUTTON_OFFSET_X,
                    rect.y + FactionKnowledgeConfig.UPSTREAM_UI_MANAGER_BUTTON_OFFSET_Y,
                    FactionKnowledgeConfig.UPSTREAM_UI_MANAGER_BUTTON_WIDTH,
                    FactionKnowledgeConfig.UPSTREAM_UI_MANAGER_BUTTON_HEIGHT);

                if (Widgets.ButtonText(
                        buttonRect, FactionKnowledgeConfig.UPSTREAM_UI_MANAGER_BUTTON_LABEL))
                {
                    OpenManager();
                }

                TooltipHandler.TipRegion(
                    buttonRect,
                    new TipSignal(FactionKnowledgeConfig.UPSTREAM_UI_MANAGER_BUTTON_TIP));
            }
            catch (Exception)
            {
                // 按钮绘制异常一律吞掉：绝不能让本 mod 的补丁弄崩上游常识界面。
            }
        }

        /// <summary>打开本 mod 的常识管理窗口；已打开时保持现状，避免叠出多个同款窗口。</summary>
        private static void OpenManager()
        {
            if (Find.WindowStack == null || Find.WindowStack.IsOpen<Dialog_KnowledgeManager>())
            {
                return;
            }
            Find.WindowStack.Add(new Dialog_KnowledgeManager());
        }
    }
}
