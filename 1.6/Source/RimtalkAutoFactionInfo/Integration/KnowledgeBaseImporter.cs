using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 随包预设库消费逻辑（FR-6 / S8 / D20 / D44 / D45 / D47）。
    ///
    /// 包粒度：**每个模组一个文件**（<c>&lt;块目录&gt;/&lt;模组名&gt;.txt</c>），另有块级通用面
    /// <c>_公共.txt</c>，以及游戏本体的 <c>本体-&lt;DLC&gt;.txt</c>。
    ///
    /// 流程：读 <c>KnowledgeBase/模组溯源映射.tsv</c>（<c>块名 / 模组名 / packageId</c>）
    /// → **逐文件**按当前已启用 mod 的 <c>packageId</c> 判定是否导入 → 与库内现有条目的
    /// 「标签 + 内容」键集合做差集 → 经上游 <c>ImportFromText(text, clearExisting: false)</c>
    /// 追加导入**缺失的整行原文**（保留社区原始重要度 / 匹配模式 / 扩展开关）。
    ///
    /// 门槛（D47）：
    /// ① 模组文件：需 mod 块开关开启，且其 <c>packageId</c> 处于已启用集合；
    /// ② <c>本体-&lt;DLC&gt;.txt</c>：需本体块开关开启，且对应 DLC 已持有（Core 恒真）；
    /// ③ <c>_公共.txt</c>：块内有模组文件时「任一模组命中即导」；本体块 / 纯公共块则由本体块开关控制。
    ///
    /// 只在新档 <c>StartedNewGame</c> 与读档 <c>LoadedGame</c> 调用（由
    /// <see cref="FactionKnowledgeComponent.RunInitialSync"/> 挂载），**不入定时校正**（D44）。
    /// 与剩余注入路径互相独立：本类自带外层 try/catch，自身失败不影响派系 / 异种人条目注入。
    /// </summary>
    public static class KnowledgeBaseImporter
    {
        /// <summary>单个块的目录构成（一次枚举、多处复用）。</summary>
        private sealed class BlockFiles
        {
            /// <summary>块目录名（= 块名，与索引表第 1 列一致）。</summary>
            public string Name;

            /// <summary>模组文件绝对路径（不含 <c>_公共.txt</c> 与 <c>本体-*.txt</c>）。</summary>
            public readonly List<string> ModFiles = new List<string>();

            /// <summary>本体文件绝对路径（<c>本体-*.txt</c>）。</summary>
            public readonly List<string> BuiltinFiles = new List<string>();

            /// <summary><c>_公共.txt</c> 绝对路径；块内无此文件时为 <c>null</c>。</summary>
            public string CommonPath;
        }

        /// <summary>
        /// 执行一次预设库导入。
        /// </summary>
        /// <returns>本次实际新增的条数（供调用方汇总）。</returns>
        public static int Run()
        {
            if (!FactionInfoSettings.EnableKnowledgeBaseImport && !FactionInfoSettings.EnableBuiltinKnowledgeImport)
            {
                KnowledgeLog.Summary(FactionKnowledgeConfig.LOG_KNOWLEDGE_IMPORT_DISABLED);
                return 0;
            }

            // 库不可用时写入会静默丢失（见 RimTalkMemoryBridge.IsLibraryAvailable），整体跳过。
            if (!RimTalkMemoryBridge.IsLibraryAvailable)
            {
                return 0;
            }

            try
            {
                return ImportCore();
            }
            catch (Exception exception)
            {
                KnowledgeLog.Error("预设库导入整体失败（派系 / 异种人常识注入不受影响）。", exception);
                return 0;
            }
        }

        /// <summary>导入主流程：定位数据 → 枚举块 → 逐文件判定 → 一次性差集导入。</summary>
        private static int ImportCore()
        {
            string baseDir = ResolveKnowledgeBaseDir();
            if (baseDir == null)
            {
                return 0;
            }

            string packDir = FindBlockPackDir(baseDir);
            if (packDir == null)
            {
                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_KNOWLEDGE_BLOCK_PACK_MISSING,
                    string.Format(
                        FactionKnowledgeConfig.WARN_KNOWLEDGE_BLOCK_PACK_MISSING,
                        FactionKnowledgeConfig.KNOWLEDGE_BLOCK_PACK_PREFIX));
                return 0;
            }

            Dictionary<string, Dictionary<string, string>> sourceMap =
                ReadSourceMap(Path.Combine(baseDir, FactionKnowledgeConfig.KNOWLEDGE_SOURCE_MAP_FILE));
            HashSet<string> activePackages = CollectActivePackageIds();

            List<BlockFiles> blocks = CollectBlocks(packDir);
            WarnSourceMapMismatch(sourceMap, blocks);
            WarnMultiBlockPackages(sourceMap, activePackages);

            // 差集基准：库内现有全部条目的「标签 + 内容」键（D45）。
            HashSet<string> existingKeys = RimTalkMemoryBridge.GetExistingTagContentKeys();

            List<string> pendingLines = new List<string>();
            int importedFiles = 0;
            int skippedFiles = 0;
            int skippedExisting = 0;

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockFiles block = blocks[i];
                bool anyModImported = false;

                // ① 模组文件：逐文件按 packageId 判定。
                for (int j = 0; j < block.ModFiles.Count; j++)
                {
                    string path = block.ModFiles[j];
                    string modName = Path.GetFileNameWithoutExtension(path);
                    if (!ShouldImportModFile(block.Name, modName, sourceMap, activePackages))
                    {
                        skippedFiles++;
                        continue;
                    }

                    if (AppendFileLines(path, existingKeys, pendingLines, ref skippedExisting))
                    {
                        importedFiles++;
                        anyModImported = true;
                    }
                    else
                    {
                        skippedFiles++;
                    }
                }

                // ② 本体文件：按持有的 DLC 取用。
                for (int j = 0; j < block.BuiltinFiles.Count; j++)
                {
                    string path = block.BuiltinFiles[j];
                    if (!ShouldImportBuiltinFile(Path.GetFileNameWithoutExtension(path)))
                    {
                        skippedFiles++;
                        continue;
                    }

                    if (AppendFileLines(path, existingKeys, pendingLines, ref skippedExisting))
                    {
                        importedFiles++;
                    }
                    else
                    {
                        skippedFiles++;
                    }
                }

                // ③ 公共面：块内有模组文件时「任一模组命中即导」；本体块 / 纯公共块由本体块开关控制。
                if (block.CommonPath != null)
                {
                    bool isBuiltinOrPureCommon =
                        block.BuiltinFiles.Count > 0 || block.ModFiles.Count == 0;
                    bool importCommon = isBuiltinOrPureCommon
                        ? FactionInfoSettings.EnableBuiltinKnowledgeImport
                        : anyModImported;

                    if (importCommon)
                    {
                        if (AppendFileLines(block.CommonPath, existingKeys, pendingLines, ref skippedExisting))
                        {
                            importedFiles++;
                        }
                        else
                        {
                            skippedFiles++;
                        }
                    }
                    else
                    {
                        skippedFiles++;
                    }
                }
            }

            int imported = 0;
            if (pendingLines.Count > 0)
            {
                imported = RimTalkMemoryBridge.ImportKnowledgeText(
                    string.Join("\n", pendingLines.ToArray()));
            }

            KnowledgeLog.Summary(string.Format(
                FactionKnowledgeConfig.LOG_KNOWLEDGE_IMPORT_SUMMARY,
                importedFiles, imported, skippedExisting, skippedFiles));
            return imported;
        }

        /// <summary>
        /// 定位随包预设库目录 <c>&lt;mod 根目录&gt;/KnowledgeBase</c>。
        /// 未找到时输出一次性告警并返回 <c>null</c>。
        /// </summary>
        private static string ResolveKnowledgeBaseDir()
        {
            string root = FactionInfoMod.ContentRootDir;
            string baseDir = string.IsNullOrEmpty(root)
                ? null
                : Path.Combine(root, FactionKnowledgeConfig.KNOWLEDGE_BASE_FOLDER);

            if (baseDir == null || !Directory.Exists(baseDir))
            {
                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_KNOWLEDGE_BASE_MISSING,
                    string.Format(
                        FactionKnowledgeConfig.WARN_KNOWLEDGE_BASE_MISSING,
                        baseDir ?? "(mod 根目录未知)"));
                return null;
            }
            return baseDir;
        }

        /// <summary>
        /// 在预设库目录下按前缀找导入包目录（真实目录名带版本戳，如「mod层导入包-1008-0543」）。
        /// 多个共存时取字典序最大者（版本戳升序 → 最新）。
        /// </summary>
        private static string FindBlockPackDir(string baseDir)
        {
            string[] dirs = Directory.GetDirectories(
                baseDir, FactionKnowledgeConfig.KNOWLEDGE_BLOCK_PACK_PREFIX + "*");
            if (dirs.Length == 0)
            {
                return null;
            }
            Array.Sort(dirs, StringComparer.Ordinal);
            return dirs[dirs.Length - 1];
        }

        /// <summary>枚举导入包下的全部块目录，并按「模组 / 本体 / 公共」三类归位文件。</summary>
        private static List<BlockFiles> CollectBlocks(string packDir)
        {
            string[] dirs = Directory.GetDirectories(packDir);
            Array.Sort(dirs, StringComparer.Ordinal);

            List<BlockFiles> result = new List<BlockFiles>();
            for (int i = 0; i < dirs.Length; i++)
            {
                BlockFiles block = new BlockFiles
                {
                    Name = Path.GetFileName(dirs[i])
                };

                string[] files = Directory.GetFiles(
                    dirs[i], "*" + FactionKnowledgeConfig.KNOWLEDGE_BLOCK_FILE_EXTENSION);
                Array.Sort(files, StringComparer.Ordinal);

                for (int j = 0; j < files.Length; j++)
                {
                    string fileName = Path.GetFileName(files[j]);
                    if (string.Equals(
                            fileName,
                            FactionKnowledgeConfig.KNOWLEDGE_COMMON_FILE_NAME,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        block.CommonPath = files[j];
                    }
                    else if (fileName.StartsWith(
                                 FactionKnowledgeConfig.KNOWLEDGE_BUILTIN_FILE_PREFIX,
                                 StringComparison.Ordinal))
                    {
                        block.BuiltinFiles.Add(files[j]);
                    }
                    else
                    {
                        block.ModFiles.Add(files[j]);
                    }
                }
                result.Add(block);
            }
            return result;
        }

        /// <summary>
        /// 读「<c>块名 → (模组名 → packageId)</c>」二级索引
        /// （表头与空行 / 缺列行自动跳过，<c>packageId</c> 已规范化）。
        /// 文件缺失或读取失败时返回空表（此时模组文件一律视为「无索引」→ 不导入，本体块不受影响）。
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> ReadSourceMap(string path)
        {
            Dictionary<string, Dictionary<string, string>> result =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path))
            {
                return result;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                KnowledgeLog.Error("读取预设库索引文件失败：" + path, exception);
                return result;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string[] columns = line.Split(FactionKnowledgeConfig.KNOWLEDGE_SOURCE_MAP_SEPARATOR);
                if (columns.Length < 3)
                {
                    continue;
                }

                string blockName = columns[0].Trim();
                string modName = columns[1].Trim();
                string packageId = columns[2].Trim();
                if (blockName.Length == 0 || modName.Length == 0 || packageId.Length == 0)
                {
                    continue;
                }
                // 表头行：第 3 列是「packageId」字样，须排除。
                if (string.Equals(packageId, "packageId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Dictionary<string, string> mods;
                if (!result.TryGetValue(blockName, out mods))
                {
                    mods = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    result[blockName] = mods;
                }
                mods[modName] = NormalizePackageId(packageId);
            }
            return result;
        }

        /// <summary>收集当前已启用 mod 的 <c>packageId</c>（已规范化：小写 + 去 Steam 后缀）。</summary>
        private static HashSet<string> CollectActivePackageIds()
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod == null)
                {
                    continue;
                }
                string id = NormalizePackageId(mod.PackageId);
                if (id.Length > 0)
                {
                    result.Add(id);
                }
            }
            return result;
        }

        /// <summary>
        /// 规范化 <c>packageId</c>：转小写并去掉 Steam 版后缀 <c>_steam</c>。
        /// 两侧（索引表 / 已启用列表）都过一遍，避免大小写与后缀差异导致漏文件。
        /// </summary>
        private static string NormalizePackageId(string packageId)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return string.Empty;
            }

            string normalized = packageId.Trim().ToLowerInvariant();
            if (normalized.EndsWith(FactionKnowledgeConfig.PACKAGE_ID_STEAM_POSTFIX, StringComparison.Ordinal))
            {
                normalized = normalized.Substring(
                    0, normalized.Length - FactionKnowledgeConfig.PACKAGE_ID_STEAM_POSTFIX.Length);
            }
            return normalized;
        }

        /// <summary>
        /// 判断某个模组文件是否应导入：需 mod 块开关开启，且索引表内该「块名 + 模组名」的
        /// <c>packageId</c> 处于已启用集合（索引缺行视为不导入）。
        /// </summary>
        private static bool ShouldImportModFile(
            string blockName,
            string modName,
            Dictionary<string, Dictionary<string, string>> sourceMap,
            HashSet<string> activePackages)
        {
            if (!FactionInfoSettings.EnableKnowledgeBaseImport)
            {
                return false;
            }

            Dictionary<string, string> mods;
            if (!sourceMap.TryGetValue(blockName, out mods))
            {
                return false;
            }

            string packageId;
            if (!mods.TryGetValue(modName, out packageId))
            {
                return false;
            }

            return activePackages.Contains(packageId);
        }

        /// <summary>
        /// 判断某个本体文件是否应导入：需本体块开关开启，且文件名 <c>本体-&lt;DLC&gt;</c> 对应的
        /// DLC 已持有（Core 恒真，未知 DLC 不导入）。
        /// </summary>
        private static bool ShouldImportBuiltinFile(string fileName)
        {
            if (!FactionInfoSettings.EnableBuiltinKnowledgeImport)
            {
                return false;
            }

            string dlc = fileName.Substring(FactionKnowledgeConfig.KNOWLEDGE_BUILTIN_FILE_PREFIX.Length);
            return IsDlcActive(dlc);
        }

        /// <summary>按 <c>本体-&lt;DLC&gt;</c> 的 DLC 名判定该本体文件是否适用（Core 恒真）。</summary>
        private static bool IsDlcActive(string dlc)
        {
            if (string.Equals(dlc, FactionKnowledgeConfig.KNOWLEDGE_DLC_CORE, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(dlc, FactionKnowledgeConfig.KNOWLEDGE_DLC_ROYALTY, StringComparison.OrdinalIgnoreCase))
            {
                return ModsConfig.RoyaltyActive;
            }
            if (string.Equals(dlc, FactionKnowledgeConfig.KNOWLEDGE_DLC_IDEOLOGY, StringComparison.OrdinalIgnoreCase))
            {
                return ModsConfig.IdeologyActive;
            }
            if (string.Equals(dlc, FactionKnowledgeConfig.KNOWLEDGE_DLC_BIOTECH, StringComparison.OrdinalIgnoreCase))
            {
                return ModsConfig.BiotechActive;
            }
            if (string.Equals(dlc, FactionKnowledgeConfig.KNOWLEDGE_DLC_ANOMALY, StringComparison.OrdinalIgnoreCase))
            {
                return ModsConfig.AnomalyActive;
            }
            if (string.Equals(dlc, FactionKnowledgeConfig.KNOWLEDGE_DLC_ODYSSEY, StringComparison.OrdinalIgnoreCase))
            {
                return ModsConfig.OdysseyActive;
            }
            return false;
        }

        /// <summary>
        /// 告警：① 索引表列出、但包内缺失的（块 / 模组）文件；② 包内存在、但索引表缺行的模组文件
        /// （后者会导致该文件「静默不导入」，是包更新时的主要风险点）。各按 key 只输出一次。
        /// </summary>
        private static void WarnSourceMapMismatch(
            Dictionary<string, Dictionary<string, string>> sourceMap, List<BlockFiles> blocks)
        {
            // 包内实际存在的模组文件：块名 → 模组名集合
            Dictionary<string, HashSet<string>> present =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockFiles block = blocks[i];
                HashSet<string> mods;
                if (!present.TryGetValue(block.Name, out mods))
                {
                    mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    present[block.Name] = mods;
                }
                for (int j = 0; j < block.ModFiles.Count; j++)
                {
                    mods.Add(Path.GetFileNameWithoutExtension(block.ModFiles[j]));
                }
            }

            // ① 索引有、包内无
            List<string> mapWithoutFile = new List<string>();
            foreach (KeyValuePair<string, Dictionary<string, string>> pair in sourceMap)
            {
                HashSet<string> mods;
                bool blockPresent = present.TryGetValue(pair.Key, out mods);
                foreach (string modName in pair.Value.Keys)
                {
                    if (!blockPresent || !mods.Contains(modName))
                    {
                        mapWithoutFile.Add(pair.Key + "/" + modName);
                    }
                }
            }
            if (mapWithoutFile.Count > 0)
            {
                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_KNOWLEDGE_MAP_FILE_MISSING,
                    string.Format(
                        FactionKnowledgeConfig.WARN_KNOWLEDGE_MAP_FILE_MISSING,
                        string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, mapWithoutFile.ToArray())));
            }

            // ② 包内有、索引无
            List<string> fileWithoutMap = new List<string>();
            foreach (KeyValuePair<string, HashSet<string>> pair in present)
            {
                Dictionary<string, string> mods;
                bool blockMapped = sourceMap.TryGetValue(pair.Key, out mods);
                foreach (string modName in pair.Value)
                {
                    if (!blockMapped || !mods.ContainsKey(modName))
                    {
                        fileWithoutMap.Add(pair.Key + "/" + modName);
                    }
                }
            }
            if (fileWithoutMap.Count > 0)
            {
                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_KNOWLEDGE_PACK_FILE_UNMAPPED,
                    string.Format(
                        FactionKnowledgeConfig.WARN_KNOWLEDGE_PACK_FILE_UNMAPPED,
                        string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, fileWithoutMap.ToArray())));
            }
        }

        /// <summary>
        /// 告警：某个**已启用** packageId 同时对应多个块（正常现象，仅提示一例以便排查）。
        /// </summary>
        private static void WarnMultiBlockPackages(
            Dictionary<string, Dictionary<string, string>> sourceMap, HashSet<string> activePackages)
        {
            Dictionary<string, List<string>> blocksByPackage =
                new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, Dictionary<string, string>> pair in sourceMap)
            {
                foreach (KeyValuePair<string, string> entry in pair.Value)
                {
                    string packageId = entry.Value;
                    if (!activePackages.Contains(packageId))
                    {
                        continue;
                    }

                    List<string> blocks;
                    if (!blocksByPackage.TryGetValue(packageId, out blocks))
                    {
                        blocks = new List<string>();
                        blocksByPackage[packageId] = blocks;
                    }
                    if (!blocks.Contains(pair.Key))
                    {
                        blocks.Add(pair.Key);
                    }
                }
            }

            foreach (KeyValuePair<string, List<string>> pair in blocksByPackage)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_KNOWLEDGE_PACKAGE_MULTI_BLOCK,
                    string.Format(
                        FactionKnowledgeConfig.WARN_KNOWLEDGE_PACKAGE_MULTI_BLOCK,
                        pair.Key,
                        string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, pair.Value.ToArray())));
                return;
            }
        }

        /// <summary>读单个文件并追加其缺失条目到待导入集合；失败时输出告警并返回 <c>false</c>。</summary>
        private static bool AppendFileLines(
            string filePath,
            HashSet<string> existingKeys,
            List<string> pendingLines,
            ref int skippedExisting)
        {
            string[] lines = ReadFileLines(filePath, Path.GetFileNameWithoutExtension(filePath));
            if (lines == null)
            {
                return false;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i] == null ? null : lines[i].Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                string tag;
                string content;
                if (!TryParseLine(trimmed, out tag, out content))
                {
                    continue;
                }

                // Add 返回 false 即「库内已有同标签 + 同内容」→ 跳过（同时天然覆盖文件间重复）。
                if (!existingKeys.Add(RimTalkMemoryBridge.MakeTagContentKey(tag, content)))
                {
                    skippedExisting++;
                    continue;
                }
                pendingLines.Add(trimmed);
            }
            return true;
        }

        /// <summary>读单个文件的所有行；失败时输出告警并返回 <c>null</c>。</summary>
        private static string[] ReadFileLines(string filePath, string displayName)
        {
            try
            {
                return File.ReadAllLines(filePath, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                KnowledgeLog.Warn(string.Format(
                    FactionKnowledgeConfig.WARN_KNOWLEDGE_BLOCK_READ_FAILED, displayName, exception.Message));
                return null;
            }
        }

        /// <summary>
        /// 按上游 <c>CommonKnowledgeLibrary.ParseLine</c> 的口径解析出「标签」与「内容」，用于判重。
        /// 两者必须与上游完全一致，否则差集算错会导致重复导入。
        /// </summary>
        /// <param name="line">已 Trim 的单行块文本。</param>
        /// <param name="tag">输出：标签（第 1 个子字段；整段缺失时取兜底标签）。</param>
        /// <param name="content">输出：标签框之后的全部内容（含展示层分类提示）。</param>
        /// <returns>解析出可导入条目返回 <c>true</c>；内容为空返回 <c>false</c>（上游会弃行）。</returns>
        private static bool TryParseLine(string line, out string tag, out string content)
        {
            tag = null;
            content = null;

            int tagStart = line.IndexOf('[');
            int tagEnd = tagStart >= 0 ? line.IndexOf(']', tagStart + 1) : -1;

            if (tagStart == -1 || tagEnd == -1 || tagEnd <= tagStart)
            {
                // 上游：整行作为内容、标签取兜底值。
                tag = FactionKnowledgeConfig.KNOWLEDGE_DEFAULT_TAG;
                content = line;
                return !string.IsNullOrEmpty(content);
            }

            string tagPart = line.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim();
            content = line.Substring(tagEnd + 1).Trim();
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            int fieldEnd = tagPart.IndexOf(FactionKnowledgeConfig.KNOWLEDGE_TAG_FIELD_SEPARATOR);
            tag = (fieldEnd >= 0 ? tagPart.Substring(0, fieldEnd) : tagPart).Trim();
            return true;
        }
    }
}
