using System;
using UnityEngine;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 常识条目编辑对话框（FR-18 / FR-19；D52 / D53）。
    /// 两种模式：<b>新增</b>（无快照）与<b>编辑</b>（传入待改条目快照）。
    /// 表单字段：标签、正文、重要度（0~1）、启用、允许提取 / 允许匹配；
    /// 仅当编辑「本 mod 派系注入条目」时显示锁定开关，且默认勾选——
    /// 保存即自动锁定（D52），防止改完被下次定时刷新覆盖。
    /// 保存前由本对话框完成非空、单行、重要度范围校验，失败时就地显示中文错误、不关闭窗口。
    /// </summary>
    public class Dialog_EditKnowledgeEntry : Window
    {
        // ---------- 模式与数据 ----------

        /// <summary>是否为新增模式；为假即编辑模式。</summary>
        private readonly bool isNew;

        /// <summary>编辑模式下的原条目快照；新增模式为 <c>null</c>。</summary>
        private readonly KnowledgeEntrySnapshot snapshot;

        /// <summary>保存成功后的回调（管理页据此重建快照与列表）。</summary>
        private readonly Action onSaved;

        // ---------- 表单缓冲 ----------

        /// <summary>标签输入缓冲。</summary>
        private string tagBuffer;

        /// <summary>正文输入缓冲。</summary>
        private string contentBuffer;

        /// <summary>重要度输入缓冲（按文本解析，允许玩家直接填数字）。</summary>
        private string importanceBuffer;

        /// <summary>启用复选框值。</summary>
        private bool enabledValue;

        /// <summary>锁定复选框值（仅锁定行可见时有效）。</summary>
        private bool lockValue;

        /// <summary>允许提取复选框值。</summary>
        private bool extractableValue;

        /// <summary>允许匹配复选框值。</summary>
        private bool matchableValue;

        /// <summary>当前校验错误信息；无错误为空字符串。</summary>
        private string errorMessage = string.Empty;

        /// <summary>
        /// 新增模式构造。
        /// </summary>
        /// <param name="onSaved">保存成功回调。</param>
        public Dialog_EditKnowledgeEntry(Action onSaved)
        {
            isNew = true;
            snapshot = null;
            this.onSaved = onSaved;

            tagBuffer = string.Empty;
            contentBuffer = string.Empty;
            // 默认重要度对齐「其它」档设置，避免裸魔数
            importanceBuffer = FactionInfoSettings.KnowledgeImportanceOther.ToString("0.##");
            enabledValue = true;
            lockValue = false;
            // 新增条目默认放开两个扩展开关（对齐派系注入口径）
            extractableValue = true;
            matchableValue = true;

            InitWindow();
        }

        /// <summary>
        /// 编辑模式构造。
        /// </summary>
        /// <param name="snapshot">待编辑条目的快照。</param>
        /// <param name="onSaved">保存成功回调。</param>
        public Dialog_EditKnowledgeEntry(KnowledgeEntrySnapshot snapshot, Action onSaved)
        {
            isNew = false;
            this.snapshot = snapshot;
            this.onSaved = onSaved;

            tagBuffer = snapshot.Tag ?? string.Empty;
            contentBuffer = snapshot.Content ?? string.Empty;
            importanceBuffer = snapshot.Importance.ToString("0.##");
            enabledValue = snapshot.IsEnabled;
            extractableValue = snapshot.CanBeExtracted;
            matchableValue = snapshot.CanBeMatched;
            // D52：编辑本 mod 派系注入条目时默认勾选锁定（保存即自动锁定）
            lockValue = ShowLockOptionFor(snapshot);

            InitWindow();
        }

        /// <summary>初始化窗口通用行为与尺寸策略。</summary>
        private void InitWindow()
        {
            doCloseX = true;
            doCloseButton = false;
            closeOnCancel = true;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            forcePause = false;
            draggable = true;
            resizeable = false;
            onlyOneOfTypeAllowed = false;
        }

        /// <summary>窗口初始尺寸。</summary>
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(
                    FactionKnowledgeConfig.EDITOR_WINDOW_WIDTH,
                    FactionKnowledgeConfig.EDITOR_WINDOW_HEIGHT);
            }
        }

        /// <summary>
        /// 是否应显示锁定行：编辑模式、条目内容为本 mod <b>派系注入</b>前缀。
        /// 异种人注入条目本就不参与定时刷新（只做缺失补齐），无需锁定，故不显示。
        /// </summary>
        private bool ShowLockOption
        {
            get { return !isNew && ShowLockOptionFor(snapshot); }
        }

        /// <summary>快照对应的条目是否为需要锁定保护的本 mod 派系注入条目。</summary>
        /// <param name="entry">待判定快照。</param>
        private static bool ShowLockOptionFor(KnowledgeEntrySnapshot entry)
        {
            return entry != null
                && !string.IsNullOrEmpty(entry.Content)
                && entry.Content.StartsWith(
                    FactionKnowledgeConfig.FACTION_CONTENT_PREFIX, StringComparison.Ordinal);
        }

        /// <summary>窗口正文：标题 + 表单（标签 / 正文 / 重要度+启用 / 锁定）+ 错误行 + 保存 / 取消。</summary>
        /// <param name="inRect">可用区域（已扣窗口边距）。</param>
        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, FactionKnowledgeConfig.MANAGER_HEADER_HEIGHT),
                isNew
                    ? FactionKnowledgeConfig.EDITOR_TITLE_NEW
                    : FactionKnowledgeConfig.EDITOR_TITLE_EDIT);
            Text.Font = previousFont;

            float y = inRect.y + FactionKnowledgeConfig.MANAGER_HEADER_HEIGHT + FactionKnowledgeConfig.EDITOR_GAP;
            float fieldHeight = FactionKnowledgeConfig.EDITOR_FIELD_HEIGHT;
            float lineGap = FactionKnowledgeConfig.EDITOR_GAP;

            // 标签行
            DrawLabeledField(
                inRect, y, FactionKnowledgeConfig.EDITOR_TAG_LABEL, fieldHeight,
                ref tagBuffer, FactionKnowledgeConfig.EDITOR_TAG_MAX_CHARS);
            y += fieldHeight + lineGap;

            // 正文多行文本域（10 行高，自动换行、可滚动）
            float contentAreaHeight = FactionKnowledgeConfig.EDITOR_CONTENT_AREA_HEIGHT;
            DrawLabeledTextArea(
                inRect, y, FactionKnowledgeConfig.EDITOR_CONTENT_LABEL, contentAreaHeight,
                ref contentBuffer, FactionKnowledgeConfig.EDITOR_CONTENT_MAX_CHARS);
            y += contentAreaHeight + lineGap;

            // 重要度 + 启用 同一行
            DrawImportanceEnabledRow(inRect, y, fieldHeight);
            y += fieldHeight + lineGap;

            // 扩展开关行：允许提取 / 允许匹配（两种模式均显示）
            DrawExtensionRow(inRect, y, fieldHeight);
            y += fieldHeight + lineGap;

            // 锁定行（仅本 mod 派系注入条目）
            if (ShowLockOption)
            {
                float checkX = inRect.x + FactionKnowledgeConfig.EDITOR_LABEL_WIDTH;
                Widgets.CheckboxLabeled(
                    new Rect(checkX, y, inRect.xMax - checkX, fieldHeight),
                    FactionKnowledgeConfig.EDITOR_LOCK,
                    ref lockValue);
                y += fieldHeight;

                // 锁定自动开启说明（灰字两行，顶对齐；文案较长，一行会被截断显示不全）
                Color previousColor = GUI.color;
                GUI.color = FactionKnowledgeConfig.MANAGER_HINT_COLOR;
                TextAnchor previousAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(
                    new Rect(checkX, y, inRect.xMax - checkX, FactionKnowledgeConfig.EDITOR_LOCK_TIP_HEIGHT),
                    FactionKnowledgeConfig.EDITOR_LOCK_TIP);
                Text.Anchor = previousAnchor;
                GUI.color = previousColor;
                y += FactionKnowledgeConfig.EDITOR_LOCK_TIP_HEIGHT + lineGap;
            }

            // 错误信息行（红字，位于按钮上方）
            float buttonsY = inRect.yMax - fieldHeight;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Color previousColor = GUI.color;
                GUI.color = FactionKnowledgeConfig.EDITOR_ERROR_COLOR;
                TextAnchor previousAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(
                    new Rect(inRect.x, buttonsY - fieldHeight - lineGap, inRect.width, fieldHeight),
                    errorMessage);
                Text.Anchor = previousAnchor;
                GUI.color = previousColor;
            }

            // 保存 / 取消（右下角）
            float buttonWidth = FactionKnowledgeConfig.EDITOR_BUTTON_WIDTH;
            float buttonsWidth = buttonWidth * 2f + lineGap;
            float x = inRect.xMax - buttonsWidth;
            if (Widgets.ButtonText(
                    new Rect(x, buttonsY, buttonWidth, fieldHeight),
                    FactionKnowledgeConfig.EDITOR_SAVE))
            {
                TrySave();
            }
            x += buttonWidth + lineGap;

            if (Widgets.ButtonText(
                    new Rect(x, buttonsY, buttonWidth, fieldHeight),
                    FactionKnowledgeConfig.MANAGER_CANCEL))
            {
                Close();
            }
        }

        /// <summary>
        /// 绘制「左标签 + 右单行输入框」一行。
        /// </summary>
        /// <param name="parent">父区域（用于取左右边界）。</param>
        /// <param name="y">本行顶边 y。</param>
        /// <param name="labelText">左侧字段标签。</param>
        /// <param name="fieldHeight">输入框高。</param>
        /// <param name="buffer">输入缓冲（按引用回写）。</param>
        /// <param name="maxChars">字符上限。</param>
        private void DrawLabeledField(
            Rect parent, float y, string labelText, float fieldHeight,
            ref string buffer, int maxChars)
        {
            float labelWidth = FactionKnowledgeConfig.EDITOR_LABEL_WIDTH;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(parent.x, y, labelWidth, fieldHeight), labelText);
            Text.Anchor = previousAnchor;

            Rect fieldRect = new Rect(
                parent.x + labelWidth, y, parent.width - labelWidth, fieldHeight);
            buffer = Widgets.TextField(fieldRect, buffer ?? string.Empty, maxChars);
        }

        /// <summary>
        /// 绘制「左标签 + 右多行文本域」：文本域自动换行、内容超高时内部滚动；
        /// 字符数超限时硬截断（<see cref="Widgets.TextArea"/> 不带字符上限参数）。
        /// </summary>
        /// <param name="parent">父区域（用于取左右边界）。</param>
        /// <param name="y">本块顶边 y。</param>
        /// <param name="labelText">左侧字段标签。</param>
        /// <param name="areaHeight">文本域高。</param>
        /// <param name="buffer">输入缓冲（按引用回写）。</param>
        /// <param name="maxChars">字符上限。</param>
        private void DrawLabeledTextArea(
            Rect parent, float y, string labelText, float areaHeight,
            ref string buffer, int maxChars)
        {
            float labelWidth = FactionKnowledgeConfig.EDITOR_LABEL_WIDTH;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(parent.x, y, labelWidth, areaHeight), labelText);
            Text.Anchor = previousAnchor;

            Rect areaRect = new Rect(
                parent.x + labelWidth, y, parent.width - labelWidth, areaHeight);

            // 显式控件名：让 GUI 稳定维持该文本域焦点（本框仅一个文本域，属防御性写法）
            GUI.SetNextControlName("CKEditorContentArea");
            string value = Widgets.TextArea(areaRect, buffer ?? string.Empty);
            buffer = ClampChars(value, maxChars);
        }

        /// <summary>按字符上限硬截断输入缓冲（不追加省略号，避免截断符混入保存内容）。</summary>
        /// <param name="text">原始文本。</param>
        /// <param name="maxChars">字符上限。</param>
        private static string ClampChars(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            {
                return text ?? string.Empty;
            }
            return text.Substring(0, maxChars);
        }

        /// <summary>
        /// 绘制重要度（标签 + 短输入框）与启用复选框同行。
        /// </summary>
        /// <param name="parent">父区域。</param>
        /// <param name="y">本行顶边 y。</param>
        /// <param name="fieldHeight">控件高。</param>
        private void DrawImportanceEnabledRow(Rect parent, float y, float fieldHeight)
        {
            float labelWidth = FactionKnowledgeConfig.EDITOR_LABEL_WIDTH;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                new Rect(parent.x, y, labelWidth, fieldHeight),
                FactionKnowledgeConfig.EDITOR_IMPORTANCE_LABEL);
            Text.Anchor = previousAnchor;

            float fieldX = parent.x + labelWidth;
            Rect importanceRect = new Rect(
                fieldX, y, FactionKnowledgeConfig.EDITOR_IMPORTANCE_FIELD_WIDTH, fieldHeight);
            importanceBuffer = Widgets.TextField(importanceRect, importanceBuffer ?? string.Empty);

            float enabledX = importanceRect.xMax + FactionKnowledgeConfig.EDITOR_GAP * 2f;
            Widgets.CheckboxLabeled(
                new Rect(enabledX, y, parent.xMax - enabledX, fieldHeight),
                FactionKnowledgeConfig.EDITOR_ENABLED,
                ref enabledValue);
        }

        /// <summary>
        /// 绘制扩展开关行：左标签「扩展」+「允许提取」「允许匹配」两个复选框并排，
        /// 每个复选框整块区域都可悬停查看作用说明。
        /// </summary>
        /// <param name="parent">父区域。</param>
        /// <param name="y">本行顶边 y。</param>
        /// <param name="fieldHeight">控件高。</param>
        private void DrawExtensionRow(Rect parent, float y, float fieldHeight)
        {
            float labelWidth = FactionKnowledgeConfig.EDITOR_LABEL_WIDTH;

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                new Rect(parent.x, y, labelWidth, fieldHeight),
                FactionKnowledgeConfig.EDITOR_EXTENSION_LABEL);
            Text.Anchor = previousAnchor;

            float checkX = parent.x + labelWidth;

            // 允许提取
            Rect extractRect = new Rect(
                checkX, y,
                FactionKnowledgeConfig.EDITOR_CHECKBOX_WIDTH, fieldHeight);
            Widgets.CheckboxLabeled(
                extractRect, FactionKnowledgeConfig.EDITOR_EXTRACT_LABEL, ref extractableValue);
            TooltipHandler.TipRegion(extractRect, FactionKnowledgeConfig.EDITOR_EXTRACT_TIP);

            // 允许匹配
            Rect matchRect = new Rect(
                extractRect.xMax + FactionKnowledgeConfig.EDITOR_GAP, y,
                FactionKnowledgeConfig.EDITOR_CHECKBOX_WIDTH, fieldHeight);
            Widgets.CheckboxLabeled(
                matchRect, FactionKnowledgeConfig.EDITOR_MATCH_LABEL, ref matchableValue);
            TooltipHandler.TipRegion(matchRect, FactionKnowledgeConfig.EDITOR_MATCH_TIP);
        }

        /// <summary>
        /// 校验表单并按模式执行新增 / 更新；任一校验失败只显示错误、不关闭窗口。
        /// </summary>
        private void TrySave()
        {
            string tag = tagBuffer == null ? string.Empty : tagBuffer.Trim();
            string content = contentBuffer == null ? string.Empty : contentBuffer.Trim();

            if (tag.Length == 0)
            {
                errorMessage = FactionKnowledgeConfig.EDITOR_ERROR_TAG_EMPTY;
                return;
            }
            if (content.Length == 0)
            {
                errorMessage = FactionKnowledgeConfig.EDITOR_ERROR_CONTENT_EMPTY;
                return;
            }
            if (ContainsLineBreak(tag) || ContainsLineBreak(content))
            {
                errorMessage = FactionKnowledgeConfig.EDITOR_ERROR_SINGLE_LINE;
                return;
            }

            float importance;
            if (!float.TryParse(importanceBuffer, out importance)
                || importance < 0f || importance > 1f)
            {
                errorMessage = FactionKnowledgeConfig.EDITOR_ERROR_IMPORTANCE;
                return;
            }

            if (isNew)
            {
                // D53：玩家自建条目复用 AddLore（含启用态与两个扩展开关）；无需锁定。
                string id = RimTalkMemoryBridge.AddLore(
                    tag, content, importance, enabledValue,
                    extractableValue, matchableValue);
                if (string.IsNullOrEmpty(id))
                {
                    errorMessage = FactionKnowledgeConfig.EDITOR_ADD_FAILED;
                    return;
                }
            }
            else
            {
                // D52：四字段经桥接层组合上游 Update API 一次写入，两个扩展开关随后同次设置。
                if (!RimTalkMemoryBridge.UpdateEntry(
                        snapshot.Id, tag, content, importance, enabledValue,
                        extractableValue, matchableValue))
                {
                    errorMessage = FactionKnowledgeConfig.EDITOR_SAVE_FAILED;
                    return;
                }

                // 按锁定开关更新：勾选（默认）即保存自动锁定，取消即解锁（D52 / D55）。
                if (ShowLockOption)
                {
                    FactionKnowledgeComponent.SetEntryLocked(snapshot.Id, lockValue);
                }
            }

            errorMessage = string.Empty;
            if (onSaved != null)
            {
                onSaved();
            }
            Close();
        }

        /// <summary>文本是否含换行（CR / LF）。</summary>
        /// <param name="text">待判定文本。</param>
        private static bool ContainsLineBreak(string text)
        {
            return text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0;
        }
    }
}
