using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 派系常识的筛选与文案构建（FR-2 筛选 / FR-3 内容 / FR-4 标签）。
    /// 内容强制**单行无换行**：上游导入导出按 <c>\n</c> 切行，换行会破坏格式（证据 ⑧，RK-4）。
    /// </summary>
    public static class FactionKnowledgeBuilder
    {
        /// <summary>
        /// 收集本次需要注入常识的派系（FR-2）。
        /// 排除：玩家派系、隐藏派系（机械族/虫族/古代人等）、临时派系。
        /// 遍历的是 <c>FactionManager</c> 的内部列表引用，**只读不增删**（证据 ⑨㉓）。
        /// </summary>
        public static List<Faction> CollectTargets()
        {
            List<Faction> result = new List<Faction>();
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction faction = all[i];
                if (faction == null || faction.IsPlayer || faction.Hidden || faction.temporary)
                {
                    continue;
                }
                result.Add(faction);
            }
            return result;
        }

        /// <summary>
        /// 为单个派系构建一条常识：<c>tag</c> = 派系名（必要时并列特征异种人名），
        /// <c>content</c> = FR-3 五段模板拼接出的单行文本。
        /// 派系名不可读时返回 <c>false</c>：不写入无主键的条目，避免白占注入名额。
        /// </summary>
        /// <param name="faction">目标派系。</param>
        /// <param name="tag">输出：触发标签。</param>
        /// <param name="content">输出：单行注入内容。</param>
        public static bool TryBuild(Faction faction, out string tag, out string content)
        {
            tag = null;
            content = null;

            if (faction == null || faction.def == null)
            {
                return false;
            }

            // Faction.Name 在 HasName 为假时已自动回退 def.LabelCap（证据 ㉓）
            string name = faction.Name;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(FactionKnowledgeConfig.FACTION_CONTENT_PREFIX);
            AppendIdentitySection(builder, faction, name);
            AppendIdeologySection(builder, faction);
            AppendRelationSection(builder, faction, name);
            AppendLeaderAndSettlementSection(builder, faction, name);
            AppendMemberCompositionSection(builder, faction, name);

            content = builder.ToString();

            // tag 主词为派系名；「以某异种人为主」的派系（如赫血种/骠骑种派系）并列该异种人名，
            // 这样玩家提到异种人名时也能带出该派系的常识。
            // ⚠ 只能写进 tag：上游只认 tag（GetTags() 按 5 种分隔符切分），
            //   CommonKnowledgeEntry.keywords 字段只被序列化、不参与任何匹配（证据 ㉗）。
            tag = name;
            XenotypeDef dominantXenotype = XenotypeKnowledgeBuilder.FindDominantXenotype(faction);
            if (dominantXenotype != null)
            {
                string xenotypeLabel = dominantXenotype.LabelCap;
                tag = name + FactionKnowledgeConfig.TAG_SEPARATOR + xenotypeLabel;
            }

            return true;
        }

        /// <summary>① 基础身份：派系名 + 类型标签 + 科技水平，随后追加定义原文（原文为空则省略该句）。</summary>
        private static void AppendIdentitySection(StringBuilder builder, Faction faction, string name)
        {
            string defLabel = faction.def.LabelCap;
            builder.Append(string.Format(
                FactionKnowledgeConfig.FACTION_SEG_IDENTITY,
                name,
                defLabel,
                faction.def.techLevel.ToStringHuman()));

            // ⚠ 取 description **字段**（Def 继承来的原始介绍），不要取 FactionDef.Description 属性：
            // 后者会追加「成员异种人概率」段且用 \n 换行，会破坏上游导入导出格式（证据 ⑧㉕）
            string description = ToSingleLine(faction.def.description);
            if (!string.IsNullOrEmpty(description))
            {
                builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_DESCRIPTION, description));
            }
        }

        /// <summary>② 意识形态：主理念名 + 其信条列表；无 Ideology DLC / 非人形派系 / 无主理念 → 整段跳过。</summary>
        private static void AppendIdeologySection(StringBuilder builder, Faction faction)
        {
            // faction.ideos 非人形派系为 null（证据 ⑮⑰）
            if (!ModsConfig.IdeologyActive || faction.ideos == null)
            {
                return;
            }
            Ideo ideo = faction.ideos.PrimaryIdeo;
            if (ideo == null)
            {
                return;
            }

            // Ideo 的名称是公有字段 name（小写），不存在 Ideo.Name 属性（证据 ⑮）
            string ideoName = ideo.name;
            if (string.IsNullOrEmpty(ideoName))
            {
                return;
            }

            List<string> memes = new List<string>();
            if (ideo.memes != null)
            {
                for (int i = 0; i < ideo.memes.Count; i++)
                {
                    MemeDef meme = ideo.memes[i];
                    if (meme == null)
                    {
                        continue;
                    }
                    string memeLabel = meme.LabelCap;
                    if (!memes.Contains(memeLabel))
                    {
                        memes.Add(memeLabel);
                    }
                }
            }

            builder.Append(memes.Count == 0
                ? string.Format(FactionKnowledgeConfig.FACTION_SEG_IDEO_NO_MEMES, ideoName)
                : string.Format(
                    FactionKnowledgeConfig.FACTION_SEG_IDEO,
                    ideoName,
                    string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, memes.ToArray())));
        }

        /// <summary>③ 与我方关系：关系标签，可读时再追加好感度数值。</summary>
        private static void AppendRelationSection(StringBuilder builder, Faction faction, string name)
        {
            builder.Append(string.Format(
                FactionKnowledgeConfig.FACTION_SEG_RELATION, name, faction.PlayerRelationKind.GetLabelCap()));

            // ⚠ PlayerGoodwill 不检 HasGoodwill：关系缺失时返回 dummy 值 100 并打 Error（证据 ⑬⑭）
            if (faction.HasGoodwill)
            {
                builder.Append(string.Format(
                    FactionKnowledgeConfig.FACTION_SEG_GOODWILL, faction.PlayerGoodwill));
            }
        }

        /// <summary>④ 领袖与据点：领袖姓名（无则写明）+ 据点数量 + 可读时的最近据点距离。</summary>
        private static void AppendLeaderAndSettlementSection(StringBuilder builder, Faction faction, string name)
        {
            Pawn leader = faction.leader;
            if (leader != null)
            {
                string leaderName = leader.NameFullColored;
                builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_LEADER, name, leaderName));
            }
            else
            {
                builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_LEADER_NONE, name));
            }

            if (Find.World == null || Find.WorldObjects == null || Find.WorldGrid == null)
            {
                return;
            }

            Settlement playerHome = FindPlayerHomeSettlement();
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            int count = 0;
            float nearest = float.MaxValue;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement == null || settlement.Faction != faction)
                {
                    continue;
                }
                count++;
                if (playerHome == null)
                {
                    continue;
                }
                float distance = Find.WorldGrid.ApproxDistanceInTiles(playerHome.Tile, settlement.Tile);
                if (distance > 0f && distance < nearest)
                {
                    nearest = distance;
                }
            }

            // 据点句整句省略：无据点时写「共有 0 处定居点」没有信息量（FR-3 ④ 缺省处理）
            if (count == 0)
            {
                return;
            }
            builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_SETTLEMENT_COUNT, count));

            // RK-7：多层星球（Odyssey）下距离可能取到 int.MaxValue 等无效值 → 省略距离子句
            if (nearest > 0f && nearest < int.MaxValue)
            {
                int distanceTiles = (int)Math.Round(nearest);
                builder.Append(string.Format(
                    FactionKnowledgeConfig.FACTION_SEG_SETTLEMENT_NEAREST, distanceTiles));
            }
        }

        /// <summary>
        /// ⑤ 成员异种人构成：按游戏自身口径（<c>CollectMemberComposition</c>）列出各异种人占比。
        /// 未启用 Biotech / 非人形派系 / RK-9 护栏命中 → 整段跳过。
        /// </summary>
        private static void AppendMemberCompositionSection(StringBuilder builder, Faction faction, string name)
        {
            List<XenotypeChance> composition = XenotypeKnowledgeBuilder.CollectMemberComposition(faction);
            if (composition == null || composition.Count == 0)
            {
                return;
            }

            // RK-9 护栏：派系级 xenotypeSet 缺失、但兵种级另有异种人设置时，派系级数据读不到真实分布，
            // 此时不得武断写「全部是智人种」，整段跳过（宁可沉默，不给错误的世界观常识）。
            if (faction.def.xenotypeSet == null && XenotypeKnowledgeBuilder.HasKindLevelXenotypeOverride(faction))
            {
                KnowledgeLog.Detail("跳过成员构成段（仅有兵种级异种人设置，派系级分布不可读）：" + name);
                return;
            }

            List<string> items = new List<string>();
            for (int i = 0; i < composition.Count; i++)
            {
                XenotypeChance item = composition[i];
                if (item == null || item.xenotype == null)
                {
                    continue;
                }
                string xenotypeLabel = item.xenotype.LabelCap;
                items.Add(string.Format(
                    FactionKnowledgeConfig.FACTION_SEG_MEMBERS_ITEM,
                    xenotypeLabel,
                    item.chance.ToStringPercent()));
            }

            if (items.Count == 0)
            {
                return;
            }
            builder.Append(string.Format(
                FactionKnowledgeConfig.FACTION_SEG_MEMBERS,
                string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, items.ToArray())));
        }

        /// <summary>
        /// 取玩家自己的第一处据点作为距离参照点。
        /// 照抄引擎自身写法：遍历 <c>Find.WorldObjects.Settlements</c> 取 <c>Faction == Faction.OfPlayer</c> 的第一个（证据 ㉒）。
        /// </summary>
        /// <returns>无玩家据点时返回 <c>null</c>。</returns>
        private static Settlement FindPlayerHomeSettlement()
        {
            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                return null;
            }
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement != null && settlement.Faction == playerFaction)
                {
                    return settlement;
                }
            }
            return null;
        }

        /// <summary>把多行文本压成单行：换行（含 Windows 的 <c>\r</c>）一律替换为空格（RK-4）。</summary>
        private static string ToSingleLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
