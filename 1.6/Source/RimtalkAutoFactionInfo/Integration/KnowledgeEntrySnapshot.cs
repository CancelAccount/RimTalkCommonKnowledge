namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 常识条目的**中立只读快照**（FR-16）：供本 mod 自建界面读取，
    /// 使上游类型（<c>CommonKnowledgeEntry</c>）不外泄出 <see cref="RimTalkMemoryBridge"/>。
    /// 字段即界面所需的最小集。
    /// </summary>
    public sealed class KnowledgeEntrySnapshot
    {
        /// <summary>条目 id（上游 <c>CommonKnowledgeEntry.id</c>，形如 <c>ck-xxxxxxxxxxxx</c>）。自定义子页按此引用成员。</summary>
        public string Id;

        /// <summary>触发标签（整串原样，不切分）。</summary>
        public string Tag;

        /// <summary>条目正文（单行）。</summary>
        public string Content;

        /// <summary>重要度 0~1。</summary>
        public float Importance;

        /// <summary>是否启用。</summary>
        public bool IsEnabled;
    }
}
