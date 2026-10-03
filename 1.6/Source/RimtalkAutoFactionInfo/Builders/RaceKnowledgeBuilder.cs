using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 派系**成员种族**的取值：从 <c>PawnKindDef.race</c> 中挑出**非人类的人形种族**。
    /// </summary>
    /// <remarks>
    /// 用途：HAR / alien race 类派系（如米莉拉、绮罗）的「特色」由 **race** 表达，
    /// 在 Biotech 的异种人体系里它们只是智人种（<c>Baseliner</c>）——
    /// 只读 <c>xenotypeSet</c> 会得到「智人种 100%」这类误导内容，读 race 才能还原成员面貌。
    /// 机械族（<c>RaceProperties.IsMechanoid</c>）与人类（<c>ThingDefOf.Human</c>）不计入。
    /// 未启用任何 DLC 也可用：本类不触碰 Biotech 专有的 <c>XenotypeDef</c> API。
    /// </remarks>
    public static class RaceKnowledgeBuilder
    {
        /// <summary>
        /// 该派系兵种里出现过的**非人类人形种族**（去重，按首次出现顺序）。
        /// 数据源：<c>FactionDef.pawnGroupMakers[].options[].kind.race</c>，只读。
        /// 无 <c>pawnGroupMakers</c>（部分隐藏派系）时返回空列表。
        /// </summary>
        /// <param name="faction">目标派系。</param>
        public static List<ThingDef> CollectMemberRaces(Faction faction)
        {
            return faction != null
                ? CollectMemberRaces(faction.def)
                : new List<ThingDef>();
        }

        /// <summary>
        /// 该派系 Def 的兵种里出现过的非人类人形种族（去重，按首次出现顺序）。
        /// 供**只有 <see cref="FactionDef"/> 的场景**使用（如派系界面的介绍文本补丁，
        /// 那里拿不到 <see cref="Faction"/> 实例）。
        /// </summary>
        /// <param name="def">目标派系 Def。</param>
        public static List<ThingDef> CollectMemberRaces(FactionDef def)
        {
            List<ThingDef> result = new List<ThingDef>();
            if (def == null)
            {
                return result;
            }

            List<PawnGroupMaker> groupMakers = def.pawnGroupMakers;
            if (groupMakers == null)
            {
                return result;
            }

            for (int i = 0; i < groupMakers.Count; i++)
            {
                PawnGroupMaker groupMaker = groupMakers[i];
                if (groupMaker == null || groupMaker.options == null)
                {
                    continue;
                }
                for (int j = 0; j < groupMaker.options.Count; j++)
                {
                    PawnGenOption option = groupMaker.options[j];
                    PawnKindDef kind = option != null ? option.kind : null;
                    ThingDef race = kind != null ? kind.race : null;
                    if (IsNonHumanHumanlike(race) && !result.Contains(race))
                    {
                        result.Add(race);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 该派系的「主成员种族」：非人类人形种族**唯一**时返回它，0 个或多种时返回 <c>null</c>。
        /// 与 <c>XenotypeKnowledgeBuilder.FindDominantXenotype</c> 同口径——多种并存时无从判断
        /// 谁是特征，宁可不下结论（既不入内容段，也不并入标签），免得武断声称派系血统。
        /// </summary>
        /// <param name="faction">目标派系。</param>
        public static ThingDef FindPrimaryMemberRace(Faction faction)
        {
            return faction != null ? FindPrimaryMemberRace(faction.def) : null;
        }

        /// <summary>该派系 Def 的「主成员种族」：非人类人形种族唯一时返回它，否则返回 <c>null</c>。</summary>
        /// <param name="def">目标派系 Def。</param>
        public static ThingDef FindPrimaryMemberRace(FactionDef def)
        {
            List<ThingDef> races = CollectMemberRaces(def);
            return races.Count == 1 ? races[0] : null;
        }

        /// <summary>
        /// 判是否「非人类的人形种族」：<c>race.race.Humanlike</c> 为真、且既不是人类也不是机械族。
        /// 机械族虽可能满足人形智能判定，但属另一套体系（另有机械族口径），不在此统计。
        /// </summary>
        /// <param name="race">待判定的种族 <c>ThingDef</c>，可为 <c>null</c>（非 Pawn 类 Def 无 race）。</param>
        private static bool IsNonHumanHumanlike(ThingDef race)
        {
            if (race == null || race == ThingDefOf.Human || race.race == null)
            {
                return false;
            }
            return race.race.Humanlike && !race.race.IsMechanoid;
        }
    }
}
