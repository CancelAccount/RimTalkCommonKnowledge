using System.Collections.Generic;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 玩家自建的「自定义子页」（FR-16 / D49）：一个有名字的条目集合，成员按条目 id 引用。
    /// 落在本 mod 设置（<see cref="FactionInfoSettings.customKnowledgePages"/>）里，
    /// 随设置文件持久化，不写入上游常识库、不污染其数据。
    /// </summary>
    public class KnowledgeSubPage : IExposable
    {
        /// <summary>子页名称（玩家可改）。</summary>
        public string name;

        /// <summary>成员条目 id 列表（上游 <c>CommonKnowledgeEntry.id</c>）。</summary>
        public List<string> entryIds = new List<string>();

        /// <summary>供 <c>Scribe_Collections</c> 反序列化用的无参构造。</summary>
        public KnowledgeSubPage()
        {
        }

        /// <summary>按名新建一个空子页。</summary>
        /// <param name="name">子页名称。</param>
        public KnowledgeSubPage(string name)
        {
            this.name = name;
        }

        /// <summary>读写存档（本 mod 设置文件）。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", null);
            Scribe_Collections.Look(ref entryIds, "entryIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && entryIds == null)
            {
                entryIds = new List<string>();
            }
        }
    }
}
