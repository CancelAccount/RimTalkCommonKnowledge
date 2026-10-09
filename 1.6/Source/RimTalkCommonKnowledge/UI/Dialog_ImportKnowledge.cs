using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 常识条目导入对话框（D59）。
    /// 载体为<b>系统剪贴板</b>：打开时自动读取剪贴板内容，也可点「读取剪贴板」重新载入；
    /// 文本区接受上游标准格式（每行一条：<c>[标签|重要度|匹配模式|允许提取|允许匹配]正文</c>，
    /// 无头部的纯文本行按标签「通用」导入）。
    /// 因上游 <c>ImportFromText</c> 零去重，本对话框默认按「标签 + 正文」键
    /// 跳过库内已有条目及文本内部重复行，并显示「有效行数 / 将导入条数」供确认；
    /// 校验失败就地显示中文错误、不关闭窗口。
    /// </summary>
    public class Dialog_ImportKnowledge : Window
    {
        /// <summary>库内现有条目的「标签 + 正文」键集合（构造时快照，作为去重基准）。</summary>
        private readonly HashSet<string> existingKeys;

        /// <summary>导入成功后的回调（管理页据此重建快照与列表）。</summary>
        private readonly Action onImported;

        /// <summary>文本区缓冲：构造时默认取系统剪贴板。</summary>
        private string textBuffer;

        /// <summary>是否跳过重复条目（默认开启）。</summary>
        private bool skipDuplicates = true;

        /// <summary>当前校验错误信息；无错误为空字符串。</summary>
        private string errorMessage = string.Empty;

        /// <summary>
        /// 导入对话框构造。
        /// </summary>
        /// <param name="existingKeys">库内现有「标签 + 正文」键集合（本方法内复制，不改动调用方集合）。</param>
        /// <param name="onImported">导入成功回调。</param>
        public Dialog_ImportKnowledge(HashSet<string> existingKeys, Action onImported)
        {
            this.existingKeys = existingKeys != null
                ? new HashSet<string>(existingKeys, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            this.onImported = onImported;

            // 自动以剪贴板内容作为初始文本，省一次粘贴；剪贴板为空时给空串。
            textBuffer = GUIUtility.systemCopyBuffer ?? string.Empty;

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
                    FactionKnowledgeConfig.IMPORTER_WINDOW_WIDTH,
                    FactionKnowledgeConfig.IMPORTER_WINDOW_HEIGHT);
            }
        }

        /// <summary>窗口正文：标题 + 文本区 + 工具行 + 去重行 + 统计 / 错误行 + 确定 / 取消。</summary>
        /// <param name="inRect">可用区域（已扣窗口边距）。</param>
        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, FactionKnowledgeConfig.MANAGER_HEADER_HEIGHT),
                FactionKnowledgeConfig.IMPORTER_TITLE);
            Text.Font = previousFont;

            float gap = FactionKnowledgeConfig.EDITOR_GAP;
            float fieldHeight = FactionKnowledgeConfig.EDITOR_FIELD_HEIGHT;

            float y = inRect.y + FactionKnowledgeConfig.MANAGER_HEADER_HEIGHT + gap;

            // 大文本区（自动换行、超高内滚；导入文本不设字符上限）
            float areaHeight = FactionKnowledgeConfig.IMPORTER_CONTENT_AREA_HEIGHT;
            Rect areaRect = new Rect(inRect.x, y, inRect.width, areaHeight);
            GUI.SetNextControlName("CKImporterContentArea");
            textBuffer = Widgets.TextArea(areaRect, textBuffer ?? string.Empty);
            y += areaHeight + gap;

            // 工具行：读取剪贴板 / 清空
            float x = inRect.x;
            if (Widgets.ButtonText(
                    new Rect(x, y, FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH, fieldHeight),
                    FactionKnowledgeConfig.IMPORTER_PASTE))
            {
                textBuffer = GUIUtility.systemCopyBuffer ?? string.Empty;
                errorMessage = string.Empty;
            }
            x += FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH + gap;

            if (Widgets.ButtonText(
                    new Rect(x, y, FactionKnowledgeConfig.MANAGER_BUTTON_WIDTH, fieldHeight),
                    FactionKnowledgeConfig.IMPORTER_CLEAR))
            {
                textBuffer = string.Empty;
                errorMessage = string.Empty;
            }
            y += fieldHeight + gap;

            // 去重复选框行
            Widgets.CheckboxLabeled(
                new Rect(inRect.x, y, inRect.width, fieldHeight),
                FactionKnowledgeConfig.IMPORTER_SKIP_DUPLICATES,
                ref skipDuplicates);
            y += fieldHeight;

            // 统计行（灰色）或未去重警告（红色）
            int validCount;
            List<string> importLines = CollectImportLines(out validCount);
            Rect infoRect = new Rect(
                inRect.x, y, inRect.width, FactionKnowledgeConfig.MANAGER_NAV_ROW_HEIGHT);

            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (skipDuplicates)
            {
                GUI.color = FactionKnowledgeConfig.MANAGER_HINT_COLOR;
                Widgets.Label(
                    infoRect,
                    string.Format(
                        FactionKnowledgeConfig.IMPORTER_STATS_FORMAT,
                        validCount, importLines.Count));
            }
            else
            {
                GUI.color = FactionKnowledgeConfig.EDITOR_ERROR_COLOR;
                Widgets.Label(infoRect, FactionKnowledgeConfig.IMPORTER_DUPLICATE_WARNING);
            }
            Text.Anchor = previousAnchor;
            GUI.color = Color.white;

            // 错误信息行（红字，位于按钮上方）
            float buttonsY = inRect.yMax - fieldHeight;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Color previousColor = GUI.color;
                GUI.color = FactionKnowledgeConfig.EDITOR_ERROR_COLOR;
                TextAnchor errorAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(
                    new Rect(inRect.x, buttonsY - fieldHeight - gap, inRect.width, fieldHeight),
                    errorMessage);
                Text.Anchor = errorAnchor;
                GUI.color = previousColor;
            }

            // 确定 / 取消（右下角）
            float buttonWidth = FactionKnowledgeConfig.EDITOR_BUTTON_WIDTH;
            float buttonsWidth = buttonWidth * 2f + gap;
            float buttonX = inRect.xMax - buttonsWidth;

            if (Widgets.ButtonText(
                    new Rect(buttonX, buttonsY, buttonWidth, fieldHeight),
                    FactionKnowledgeConfig.MANAGER_CONFIRM))
            {
                TryImport(validCount, importLines);
            }
            buttonX += buttonWidth + gap;

            if (Widgets.ButtonText(
                    new Rect(buttonX, buttonsY, buttonWidth, fieldHeight),
                    FactionKnowledgeConfig.MANAGER_CANCEL))
            {
                Close();
            }
        }

        /// <summary>
        /// 解析文本区并收集实际要导入的行。
        /// </summary>
        /// <param name="validCount">输出：识别到的有效行总数（不含空行 / 正文缺失行）。</param>
        /// <returns>将导入的行文本（已按去重口径过滤）；未开启去重时为全部有效行。</returns>
        private List<string> CollectImportLines(out int validCount)
        {
            List<string> importLines = new List<string>();
            validCount = 0;

            if (string.IsNullOrEmpty(textBuffer))
            {
                return importLines;
            }

            // 去重基准：开启时以库内现有键为初值，并在文本内部继续去重；
            // 关闭时不做任何键判定（seen 保持空且不查询）。
            HashSet<string> seen = skipDuplicates
                ? new HashSet<string>(existingKeys, StringComparer.Ordinal)
                : null;

            // 与上游一致：按 CR / LF 拆行、RemoveEmptyEntries
            string[] lines = textBuffer.Split(
                new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                ParsedLine parsed = ParseForImport(lines[i]);
                if (!parsed.valid)
                {
                    continue;
                }

                validCount++;
                if (seen == null || seen.Add(parsed.key))
                {
                    importLines.Add(parsed.raw);
                }
            }
            return importLines;
        }

        /// <summary>
        /// 执行导入：先做空内容 / 全重复校验，再把行拼接交桥接层写入，成功后提示并关闭。
        /// </summary>
        /// <param name="validCount">有效行数（由绘制时同口径计算）。</param>
        /// <param name="importLines">将导入的行。</param>
        private void TryImport(int validCount, List<string> importLines)
        {
            if (validCount == 0)
            {
                errorMessage = FactionKnowledgeConfig.IMPORTER_ERROR_EMPTY;
                return;
            }
            if (skipDuplicates && importLines.Count == 0)
            {
                errorMessage = FactionKnowledgeConfig.IMPORTER_ERROR_ALL_DUPLICATE;
                return;
            }

            // 行之间用 LF 连接；上游按 CR/LF 再拆，口径一致。
            string payload = string.Join("\n", importLines);
            int count = RimTalkMemoryBridge.ImportEntries(payload);
            if (count <= 0)
            {
                errorMessage = FactionKnowledgeConfig.EDITOR_SAVE_FAILED;
                return;
            }

            errorMessage = string.Empty;
            Messages.Message(
                string.Format(FactionKnowledgeConfig.IMPORTER_RESULT_FORMAT, count),
                MessageTypeDefOf.SilentInput);

            if (onImported != null)
            {
                onImported();
            }
            Close();
        }

        /// <summary>单行解析结果：有效标记、判重键、trim 后原文。</summary>
        private struct ParsedLine
        {
            /// <summary>是否为可导入的有效行。</summary>
            public bool valid;

            /// <summary>「标签 + 正文」判重键。</summary>
            public string key;

            /// <summary>trim 后的原始行文本（用于回传上游导入）。</summary>
            public string raw;
        }

        /// <summary>
        /// 按上游 <c>ParseLine</c>（反编译 L1845~L1919）同口径解析一行，只提取去重所需的标签与正文：
        /// 无有效方括号头 → 标签「通用」、正文为整行；有头 → 标签取竖线前首段、正文取右括号之后；正文空则无效。
        /// </summary>
        /// <param name="rawLine">原始单行文本。</param>
        private static ParsedLine ParseForImport(string rawLine)
        {
            string line = rawLine == null ? string.Empty : rawLine.Trim();
            ParsedLine result = new ParsedLine { raw = line };

            if (line.Length == 0)
            {
                return result;
            }

            int open = line.IndexOf('[');
            int close = open >= 0 ? line.IndexOf(']', open + 1) : -1;

            // 无有效括号头：整条按「通用」标签导入（与上游一致）
            if (open == -1 || close == -1 || close <= open)
            {
                result.valid = true;
                result.key = RimTalkMemoryBridge.MakeTagContentKey(
                    FactionKnowledgeConfig.KNOWLEDGE_DEFAULT_TAG, line);
                return result;
            }

            string meta = line.Substring(open + 1, close - open - 1).Trim();
            string content = line.Substring(close + 1).Trim();

            // 正文缺失：上游 ParseLine 返回 null 不计数，故判无效
            if (content.Length == 0)
            {
                return result;
            }

            // 标签 = 头部竖线前首段；无竖线则整个头部即标签；空标签退「通用」
            string tag = meta;
            int pipe = meta.IndexOf('|');
            if (pipe >= 0)
            {
                tag = meta.Substring(0, pipe).Trim();
            }
            if (tag.Length == 0)
            {
                tag = FactionKnowledgeConfig.KNOWLEDGE_DEFAULT_TAG;
            }

            result.valid = true;
            result.key = RimTalkMemoryBridge.MakeTagContentKey(tag, content);
            return result;
        }
    }
}
