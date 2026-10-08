using System;
using System.Collections.Generic;
using RimTalk.Memory;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 上游 RimTalk-Expand Memory 常识库的唯一桥接层。
    /// 本工程内只有此文件 <c>using RimTalk.Memory</c>，其余代码不感知上游类型，
    /// 便于日后上游改签名或改用反射时只改这一处。
    /// </summary>
    public static class RimTalkMemoryBridge
    {
        /// <summary>
        /// 上游常识库当前是否可用。
        /// 必须为真才可写入：<c>Current.Game == null</c> 时上游 <c>MemoryManager.GetCommonKnowledge()</c>
        /// 会返回一个**游离的空库**（不是 null），写进去的内容会静默丢失。
        /// </summary>
        public static bool IsLibraryAvailable
        {
            get { return Current.Game != null; }
        }

        /// <summary>
        /// 写入一条「世界观」类常识，并显式开启「可提取 / 可匹配」两个扩展开关。
        /// </summary>
        /// <param name="tag">触发标签。多个触发词用 <c>FactionKnowledgeConfig.TAG_SEPARATOR</c> 并列。</param>
        /// <param name="content">注入内容，**必须为单行**（换行会破坏上游导入导出格式）。</param>
        /// <param name="importance">重要度，0~1。</param>
        /// <returns>成功返回条目 id；失败返回 null（上游内部已 catch 并 <c>Log.Error</c>）。</returns>
        /// <remarks>
        /// 用 <c>AddKnowledgeEx</c> 而非 <c>AddKnowledge</c>：后者完全不动两个扩展开关，
        /// 而它们在 <c>ExtendedKnowledgeEntry</c> 侧表里新建时的默认值是 **false**
        /// —— 即 <c>AddKnowledge</c> 写出的条目既不提供链式线索、也不能被链式命中。
        /// 派系常识要求两者都放开。
        /// </remarks>
        public static string AddLore(string tag, string content, float importance)
        {
            string id = CommonKnowledgeAPI.AddKnowledgeEx(
                tag,
                content,
                importance,
                KeywordMatchMode.Any,
                FactionKnowledgeConfig.TARGET_PAWN_ALL,
                canBeExtracted: true,
                canBeMatched: true);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            // AddKnowledgeEx 同样不设置分类；按设置补设为「世界观」以让 UI 正确归类。
            // 关闭该设置则不写分类，退化为上游按 tag 自动猜分类（实测会落到「其它」，见证据 ⑦）。
            if (FactionInfoSettings.CategoryAlwaysLore)
            {
                CommonKnowledgeEntry entry = CommonKnowledgeAPI.FindKnowledgeById(id);
                if (entry != null)
                {
                    entry.category = KnowledgeEntryCategory.Lore;
                }
            }
            return id;
        }

        /// <summary>
        /// 找出「本 mod 管理的」条目 id：要求条目 tag 的**第一段**与 <paramref name="primaryTag"/> 全等，
        /// 且内容以 <paramref name="contentPrefix"/> 开头。
        /// </summary>
        /// <remarks>
        /// 两处都必须这么判：
        /// ① 上游 <c>FindKnowledge</c> 是 **tag 子串**匹配，查「帝国」会连带命中「新帝国」，
        ///    不再做一次全等判会误删/误判他人条目；
        /// ② 本 mod 会把特征异种人名追加到 tag 尾段，tag 会随派系成员构成变化，
        ///    因此只能用**稳定的第一段（派系名）**作主键，不能用整串 tag 比对。
        /// </remarks>
        public static List<string> FindManagedIds(string primaryTag, string contentPrefix)
        {
            string ignoredContent;
            return FindManagedIds(primaryTag, contentPrefix, out ignoredContent);
        }

        /// <summary>
        /// 找出「本 mod 管理的」条目 id，并同时给出**第一条**的内容。
        /// 定时刷新需要先用内容做比对，再用 id 列表整体清除（防止异常情况下残留多条）。
        /// </summary>
        /// <param name="primaryTag">主键：派系名（tag 第一段）。</param>
        /// <param name="contentPrefix">内容前缀，用于区分本 mod 的派系条目与异种人条目。</param>
        /// <param name="firstContent">输出：第一条条目的内容；无条目时为 <c>null</c>。</param>
        public static List<string> FindManagedIds(string primaryTag, string contentPrefix, out string firstContent)
        {
            firstContent = null;

            List<CommonKnowledgeEntry> entries = FindManagedEntries(primaryTag, contentPrefix);
            List<string> result = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                result.Add(entries[i].id);
            }

            if (entries.Count > 0)
            {
                firstContent = entries[0].content;
            }
            return result;
        }

        /// <summary>
        /// 本 mod 管理的条目对象集合。
        /// **上游类型只在本类内部流转**，不通过公开签名外泄。
        /// </summary>
        private static List<CommonKnowledgeEntry> FindManagedEntries(string primaryTag, string contentPrefix)
        {
            List<CommonKnowledgeEntry> result = new List<CommonKnowledgeEntry>();
            if (string.IsNullOrEmpty(primaryTag))
            {
                return result;
            }

            List<CommonKnowledgeEntry> found = CommonKnowledgeAPI.FindKnowledge(primaryTag);
            if (found == null)
            {
                return result;
            }

            for (int i = 0; i < found.Count; i++)
            {
                CommonKnowledgeEntry entry = found[i];
                if (entry == null || string.IsNullOrEmpty(entry.content))
                {
                    continue;
                }

                // 借上游自己的切分逻辑（带缓存的 public 方法），保持分隔符口径一致
                List<string> entryTags = entry.GetTags();
                if (entryTags == null || entryTags.Count == 0)
                {
                    continue;
                }

                if (string.Equals(entryTags[0], primaryTag, StringComparison.Ordinal) &&
                    entry.content.StartsWith(contentPrefix, StringComparison.Ordinal))
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        /// <summary>
        /// 按**内容里的固定句**找出本 mod 管理的条目 id，并给出**第一条**的内容。
        /// 专供「名字会变」的条目（我方派系）：派系改名或飞船改名后 tag 已变，
        /// 用主键查库会找不到旧条目、导致旧条目残留成重复项；而内容里的固定句与名字无关，
        /// 故改用它识别——旧条目无论当时叫什么名字都能被找回并删除。
        /// </summary>
        /// <param name="contentMarker">内容里固定出现的句子（与条目名字无关）。</param>
        /// <param name="contentPrefix">内容前缀，用于限定是本 mod 的哪一类条目。</param>
        /// <param name="firstContent">输出：第一条条目的内容；无条目时为 <c>null</c>。</param>
        public static List<string> FindManagedIdsByContent(
            string contentMarker, string contentPrefix, out string firstContent)
        {
            firstContent = null;
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(contentMarker))
            {
                return result;
            }

            // 上游这是**子串**匹配，故必须再用内容前缀限定一次，避免误伤他人条目
            List<CommonKnowledgeEntry> found = CommonKnowledgeAPI.FindKnowledgeByContent(contentMarker);
            if (found == null)
            {
                return result;
            }

            for (int i = 0; i < found.Count; i++)
            {
                CommonKnowledgeEntry entry = found[i];
                if (entry == null || string.IsNullOrEmpty(entry.id) || string.IsNullOrEmpty(entry.content))
                {
                    continue;
                }
                if (!entry.content.StartsWith(contentPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(entry.id);
                if (firstContent == null)
                {
                    firstContent = entry.content;
                }
            }
            return result;
        }

        /// <summary>
        /// 上游常识库里是否存在**非本 mod 注入**的、以 <paramref name="defLabel"/> 为**首段标签**的条目
        /// ——即「社区常识库是否已覆盖该 Def」。
        /// </summary>
        /// <param name="defLabel">目标 Def 的名称（<c>FactionDef.LabelCap</c> / <c>XenotypeDef.LabelCap</c>）。</param>
        /// <returns>命中社区条目返回 <c>true</c>；库不可用 / 无命中返回 <c>false</c>（此时本 mod 照常写全）。</returns>
        /// <remarks>
        /// 用途：社区条目通常已写入该 Def 的定义原文（甚至是更详细、更个性化的改写），
        /// 命中时本 mod 的「定义原文」段应让位，只保留社区写不出的运行时动态段。
        /// <para>
        /// 必须排除本 mod 自己的条目：隐藏派系的实例名与 <c>def.LabelCap</c> 同字，
        /// 本 mod 自己写下的条目会以同一标签命中，若不过滤会误判为「社区已覆盖」，
        /// 导致内容在「含 / 不含定义原文」之间来回抖动。
        /// </para>
        /// </remarks>
        public static bool HasExternalEntryForTag(string defLabel)
        {
            if (!IsLibraryAvailable || string.IsNullOrEmpty(defLabel))
            {
                return false;
            }

            // 上游这是**子串**匹配（且匹配的是整串 tag 字段），故下面还要用首段全等再判一次
            List<CommonKnowledgeEntry> found = CommonKnowledgeAPI.FindKnowledge(defLabel);
            if (found == null)
            {
                return false;
            }

            for (int i = 0; i < found.Count; i++)
            {
                CommonKnowledgeEntry entry = found[i];
                if (entry == null || string.IsNullOrEmpty(entry.content) || IsOwnContent(entry.content))
                {
                    continue;
                }

                List<string> entryTags = entry.GetTags();
                if (entryTags == null || entryTags.Count == 0)
                {
                    continue;
                }
                if (string.Equals(entryTags[0], defLabel, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>内容是否由本 mod 注入（以任一本 mod 内容前缀开头）。</summary>
        private static bool IsOwnContent(string content)
        {
            return content.StartsWith(FactionKnowledgeConfig.FACTION_CONTENT_PREFIX, StringComparison.Ordinal)
                || content.StartsWith(FactionKnowledgeConfig.XENOTYPE_CONTENT_PREFIX, StringComparison.Ordinal);
        }

        /// <summary>按条目 id 删除一条常识。</summary>
        public static bool Remove(string id)
        {
            return CommonKnowledgeAPI.RemoveKnowledge(id);
        }

        /// <summary>
        /// 生成「标签 + 内容」判重键（D45）。
        /// 用控制字符 <see cref="FactionKnowledgeConfig.KNOWLEDGE_KEY_SEPARATOR"/> 分隔两部分，
        /// 避免标签或正文里出现相同拼接串而被误判为同一条。
        /// </summary>
        /// <param name="tag">条目标签（整串原样，不切分）。</param>
        /// <param name="content">条目内容。</param>
        public static string MakeTagContentKey(string tag, string content)
        {
            return (tag ?? string.Empty)
                + FactionKnowledgeConfig.KNOWLEDGE_KEY_SEPARATOR
                + (content ?? string.Empty);
        }

        /// <summary>
        /// 取上游常识库**现有全部条目**的「标签 + 内容」键集合，供预设库导入做差集（D45）。
        /// 上游 <c>ImportFromText</c> 为纯追加、零去重，故差集必须由本 mod 自己算。
        /// </summary>
        /// <returns>键集合；库不可用时为空集合（调用方应据此整体跳过导入）。</returns>
        public static HashSet<string> GetExistingTagContentKeys()
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            List<CommonKnowledgeEntry> all = CommonKnowledgeAPI.GetAllKnowledge();
            if (all == null)
            {
                return keys;
            }

            for (int i = 0; i < all.Count; i++)
            {
                CommonKnowledgeEntry entry = all[i];
                if (entry == null)
                {
                    continue;
                }
                keys.Add(MakeTagContentKey(entry.tag, entry.content));
            }
            return keys;
        }

        /// <summary>
        /// 取库内**全部条目**的中立快照，供本 mod 自建界面（FR-16）读取。
        /// 上游类型只在本类内部流转，不外泄。
        /// </summary>
        /// <returns>快照列表；库不可用或为空时返回空列表（不返回 <c>null</c>）。</returns>
        public static List<KnowledgeEntrySnapshot> GetAllSnapshots()
        {
            List<KnowledgeEntrySnapshot> result = new List<KnowledgeEntrySnapshot>();
            List<CommonKnowledgeEntry> all = CommonKnowledgeAPI.GetAllKnowledge();
            if (all == null)
            {
                return result;
            }

            for (int i = 0; i < all.Count; i++)
            {
                CommonKnowledgeEntry entry = all[i];
                if (entry == null)
                {
                    continue;
                }

                result.Add(new KnowledgeEntrySnapshot
                {
                    Id = entry.id,
                    Tag = entry.tag,
                    Content = entry.content,
                    Importance = entry.importance,
                    IsEnabled = entry.isEnabled
                });
            }
            return result;
        }

        /// <summary>
        /// 把块文件原文导入上游常识库。
        /// </summary>
        /// <param name="text">块文件内容：每行一条，保持上游原格式。</param>
        /// <returns>上游实际写入的条数。</returns>
        /// <remarks>
        /// <c>clearExisting</c> 恒为 <c>false</c>：传 <c>true</c> 会清空玩家整个常识库（证据 ㊼）。
        /// 该 API **零去重**，差集须由调用方在导入前自行完成。
        /// </remarks>
        public static int ImportKnowledgeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            return CommonKnowledgeAPI.ImportFromText(text, false);
        }
    }
}
