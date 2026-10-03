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

        /// <summary>按条目 id 删除一条常识。</summary>
        public static bool Remove(string id)
        {
            return CommonKnowledgeAPI.RemoveKnowledge(id);
        }
    }
}
