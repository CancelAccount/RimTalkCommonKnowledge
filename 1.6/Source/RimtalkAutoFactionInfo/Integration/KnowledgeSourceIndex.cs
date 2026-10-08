using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 随包预设库的「块 → 模组 → 条目键」来源索引（FR-16 / D49）。
    ///
    /// 常识条目的 <c>tag</c> 是 Def 标签、正文里也不含来源信息，故「某条常识来自哪个块 / 模组」
    /// 无法从条目本身得知，只能靠随包数据反查：本类扫描导入包目录，把每个文件的每一行解析成
    /// 「标签 + 内容」键（解析口径与导入完全一致，见 <see cref="KnowledgeBaseImporter.TryParseLine"/>），
    /// 从而给出 `键 → (块名, 模组名)`。
    ///
    /// 只在首次使用时扫描一次并缓存（随包数据为静态）；扫描失败即返回空索引，
    /// 自建界面退化为「全部 / 本 mod / 玩家自建」三类，不影响使用。
    /// </summary>
    public static class KnowledgeSourceIndex
    {
        /// <summary>一个「模组」行：导入包中的一个条目文件（文件名去扩展名，含 <c>本体-&lt;DLC&gt;</c> 与 <c>_公共</c>）。</summary>
        public sealed class ModGroup
        {
            /// <summary>模组名（= 文件名去扩展名）。</summary>
            public string Name;

            /// <summary>该文件内全部条目的「标签 + 内容」键（按文件行序）。</summary>
            public readonly List<string> Keys = new List<string>();
        }

        /// <summary>一个「块」：导入包下的一个目录。</summary>
        public sealed class BlockGroup
        {
            /// <summary>块名（= 目录名）。</summary>
            public string Name;

            /// <summary>块内全部模组行（按文件名序）。</summary>
            public readonly List<ModGroup> Mods = new List<ModGroup>();
        }

        /// <summary>反查表：条目键 → 块名。</summary>
        private static readonly Dictionary<string, string> keyToBlock =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>反查表：条目键 → 模组名。</summary>
        private static readonly Dictionary<string, string> keyToMod =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>全部块（按目录名序）。</summary>
        private static readonly List<BlockGroup> blocks = new List<BlockGroup>();

        /// <summary>是否已扫描过（含失败：失败也不重试，避免每帧重复读盘）。</summary>
        private static bool built;

        /// <summary>全部块（按目录名序）；首次访问时触发扫描。</summary>
        public static IReadOnlyList<BlockGroup> Blocks
        {
            get
            {
                EnsureBuilt();
                return blocks;
            }
        }

        /// <summary>首次使用时扫描导入包并建索引；已扫描过则直接返回。</summary>
        public static void EnsureBuilt()
        {
            if (built)
            {
                return;
            }
            built = true;

            try
            {
                Build();
            }
            catch (Exception exception)
            {
                KnowledgeLog.Error("构建常识来源索引失败，界面将只按「全部 / 本 mod / 玩家自建」分类。", exception);
            }
        }

        /// <summary>丢弃缓存，下次访问重新扫描（供界面「刷新」用；随包数据为静态，通常无需调用）。</summary>
        public static void Reset()
        {
            built = false;
            keyToBlock.Clear();
            keyToMod.Clear();
            blocks.Clear();
        }

        /// <summary>反查某条常识的来源块与模组；未命中（如本 mod 自建 / 玩家手写条目）时返回 <c>false</c>。</summary>
        /// <param name="tag">条目标签（整串）。</param>
        /// <param name="content">条目正文。</param>
        /// <param name="block">输出：块名；未命中为 <c>null</c>。</param>
        /// <param name="mod">输出：模组名；未命中为 <c>null</c>。</param>
        public static bool TryGetSource(string tag, string content, out string block, out string mod)
        {
            EnsureBuilt();

            string key = RimTalkMemoryBridge.MakeTagContentKey(tag, content);
            bool found = keyToBlock.TryGetValue(key, out block);
            if (!keyToMod.TryGetValue(key, out mod))
            {
                mod = null;
            }
            return found;
        }

        /// <summary>执行一次扫描：定位导入包 → 逐块逐文件解析行 → 建二级分组与反查表。</summary>
        private static void Build()
        {
            string baseDir = KnowledgeBaseImporter.ResolveKnowledgeBaseDir();
            if (baseDir == null)
            {
                return;
            }

            string packDir = KnowledgeBaseImporter.FindBlockPackDir(baseDir);
            if (packDir == null)
            {
                return;
            }

            string[] dirs = Directory.GetDirectories(packDir);
            Array.Sort(dirs, StringComparer.Ordinal);

            for (int i = 0; i < dirs.Length; i++)
            {
                BlockGroup block = new BlockGroup
                {
                    Name = Path.GetFileName(dirs[i])
                };

                string[] files = Directory.GetFiles(
                    dirs[i], "*" + FactionKnowledgeConfig.KNOWLEDGE_BLOCK_FILE_EXTENSION);
                Array.Sort(files, StringComparer.Ordinal);

                for (int j = 0; j < files.Length; j++)
                {
                    ModGroup mod = new ModGroup
                    {
                        Name = Path.GetFileNameWithoutExtension(files[j])
                    };
                    AddFileLines(files[j], block.Name, mod);
                    if (mod.Keys.Count > 0)
                    {
                        block.Mods.Add(mod);
                    }
                }

                blocks.Add(block);
            }
        }

        /// <summary>把一个文件的所有行解析进「模组」行与反查表；读取失败输出告警并跳过该文件。</summary>
        /// <param name="path">文件绝对路径。</param>
        /// <param name="blockName">所属块名。</param>
        /// <param name="mod">该文件对应的模组行。</param>
        private static void AddFileLines(string path, string blockName, ModGroup mod)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                KnowledgeLog.Warn(string.Format(
                    FactionKnowledgeConfig.WARN_KNOWLEDGE_BLOCK_READ_FAILED, mod.Name, exception.Message));
                return;
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
                if (!KnowledgeBaseImporter.TryParseLine(trimmed, out tag, out content))
                {
                    continue;
                }

                string key = RimTalkMemoryBridge.MakeTagContentKey(tag, content);
                mod.Keys.Add(key);
                keyToBlock[key] = blockName;
                keyToMod[key] = mod.Name;
            }
        }
    }
}
