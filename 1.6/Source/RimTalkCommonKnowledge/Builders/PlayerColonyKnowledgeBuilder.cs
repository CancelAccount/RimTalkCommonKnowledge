using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTalkCommonKnowledge
{
    /// <summary>
    /// 我方派系（即玩家自己的殖民地）常识的内容构建。
    /// 从 <see cref="FactionKnowledgeBuilder"/> 中独立出来：派系侧的通用逻辑（目标筛选、普通派系五段、
    /// 标签净化、条目类型）留在那边，这里只管「本殖民地」专属的八段与质变指纹。
    /// 内容强制**单行无换行**：上游导入导出按 <c>\n</c> 切行，换行会破坏格式。
    /// </summary>
    public static class PlayerColonyKnowledgeBuilder
    {
        /// <summary>
        /// 追加我方派系的八段：身份 / 人口 / 机械族 / 据点 / 气候 / 逆重飞船 / 建立时长 / 财富。
        /// 其中「开局剧本名」作为一句话附在 ① 身份段之内，**不单独占一段**。
        /// 飞船集合由调用方传入：标签也要用同一份飞船名，避免重复遍历、也避免两处口径漂移。
        /// </summary>
        /// <returns>
        /// 质变指纹 = 派系名 + 据点集合 + 飞船集合 + 殖民者数 + 奴隶数。
        /// 这五者变化才算「质变」（立即重写）；建立时长、财富、囚犯数与临时成员数持续在变
        /// （后者来去频繁），**刻意不进指纹**，它们的更新交由覆写冷却控制。
        /// 机械族型号构成、气候、开局剧本名同理不进指纹（机械族会持续充放电、机械师更替也频繁；剧本名整局不变，只有切换语言时才会变）。
        /// </returns>
        public static string AppendSections(
            StringBuilder builder, string name, List<PlayerGravship> gravships)
        {
            builder.Append(string.Format(FactionKnowledgeConfig.PLAYER_SEG_IDENTITY_PREFIX, name));
            builder.Append(FactionKnowledgeConfig.PLAYER_SEG_IDENTITY_SUFFIX);

            // 开局剧本句紧跟身份句：先说明「我们是谁」，再说明这局从哪种处境开始
            AppendScenarioSection(builder);

            ColonySnapshot snapshot = CollectSnapshot();
            builder.Append(string.Format(
                FactionKnowledgeConfig.PLAYER_SEG_POPULATION,
                snapshot.Colonists,
                FormatPopulationExtras(snapshot)));

            AppendMechSection(builder, snapshot);

            List<string> settlements = CollectSettlementNames();
            builder.Append(settlements.Count == 0
                ? FactionKnowledgeConfig.PLAYER_SEG_SETTLEMENTS_NONE
                : string.Format(
                    FactionKnowledgeConfig.PLAYER_SEG_SETTLEMENTS,
                    string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, settlements.ToArray())));

            AppendClimateSection(builder);

            for (int i = 0; i < gravships.Count; i++)
            {
                PlayerGravship gravship = gravships[i];
                builder.Append(gravship.StayTicks.HasValue
                    ? string.Format(
                        FactionKnowledgeConfig.PLAYER_SEG_GRAVSHIP,
                        gravship.Name,
                        FormatDuration(gravship.StayTicks.Value))
                    : string.Format(FactionKnowledgeConfig.PLAYER_SEG_GRAVSHIP_BARE, gravship.Name));
            }

            builder.Append(string.Format(
                FactionKnowledgeConfig.PLAYER_SEG_AGE, FormatDuration(CurrentTicks)));

            int? wealth = GetHomeWealth();
            if (wealth.HasValue)
            {
                builder.Append(string.Format(FactionKnowledgeConfig.PLAYER_SEG_WEALTH, wealth.Value));
            }

            StringBuilder fingerprint = new StringBuilder(name);
            fingerprint.Append('|');
            for (int i = 0; i < settlements.Count; i++)
            {
                fingerprint.Append(settlements[i]).Append(FactionKnowledgeConfig.LIST_SEPARATOR);
            }
            fingerprint.Append('|');
            for (int i = 0; i < gravships.Count; i++)
            {
                fingerprint.Append(gravships[i].Name).Append(FactionKnowledgeConfig.LIST_SEPARATOR);
            }
            // 殖民者与奴隶的数量变化视为「质变」：谁是我们的人，是叙事上的硬事实。
            // 囚犯数、临时成员数与机械族台数不进指纹——来去或充放电频繁，交由覆写冷却控制。
            fingerprint.Append('|');
            fingerprint.Append(snapshot.Colonists).Append(FactionKnowledgeConfig.LIST_SEPARATOR);
            fingerprint.Append(snapshot.Slaves);
            return fingerprint.ToString();
        }

        /// <summary>
        /// 拼我方派系的触发标签：主词为派系名，其后并列我方逆重飞船名——
        /// 玩家或殖民者提到飞船名时也能带出我方派系的介绍。
        /// ⚠ 我方派系内容不含「成员构成」段，故**不并列特征异种人名**，
        ///   否则会出现「提到某异种人却带出不含该信息的条目」的错配。
        /// </summary>
        public static string BuildTag(string primaryKey, List<PlayerGravship> gravships)
        {
            StringBuilder builder = new StringBuilder(primaryKey);
            for (int i = 0; i < gravships.Count; i++)
            {
                // 净化与普通派系同一口径，避免两处实现漂移
                string shipName = FactionKnowledgeBuilder.SanitizeTagPart(gravships[i].Name);
                if (string.IsNullOrEmpty(shipName))
                {
                    continue;
                }
                builder.Append(FactionKnowledgeConfig.TAG_SEPARATOR).Append(shipName);
            }
            return builder.ToString();
        }

        /// <summary>本殖民地一次快照：人力构成 + 友方机械族型号台数。</summary>
        private sealed class ColonySnapshot
        {
            /// <summary>自由殖民者：不含奴隶 / 囚犯 / 临时成员。</summary>
            public int Colonists;

            /// <summary>临时成员：任务寄居者（难民 / 做客等）与由我方招待的来客。</summary>
            public int Temporary;

            /// <summary>我方囚犯。</summary>
            public int Prisoners;

            /// <summary>我方奴隶。</summary>
            public int Slaves;

            /// <summary>友方机械族：型号名（<c>PawnKindDef.LabelCap</c>）→ 台数。</summary>
            public readonly Dictionary<string, int> MechsByKind = new Dictionary<string, int>();
        }

        /// <summary>
        /// 统计本殖民地的人力构成与友方机械族型号。
        /// 人力判定按「互斥优先」顺序：囚犯 → 奴隶 → 临时成员 → 殖民者，一名单位只计一类。
        /// 遍历范围是「所有地图上 + 商队 / 运输船 / 当前逆重飞船里活着的单位」，
        /// 因此外派商队中的殖民者也会被计入；亚人与动物一律排除，机械族另计型号。
        /// </summary>
        private static ColonySnapshot CollectSnapshot()
        {
            ColonySnapshot result = new ColonySnapshot();
            Faction player = Faction.OfPlayer;
            if (player == null)
            {
                return result;
            }

            List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null)
                {
                    continue;
                }

                // 机械族非人形，必须在下面的人形过滤**之前**统计，否则会被一并排掉。
                // IsColonyMech 自身已含 ModsConfig.BiotechActive 判定，未启用 Biotech 时恒为假。
                if (pawn.IsColonyMech)
                {
                    CountMechByKind(result, pawn);
                    continue;
                }

                // 名单里的 AllMaps 一侧是 mapPawns.AllPawns（= 已生成 + 容器内未生成者，尸体已被其内部排掉），
                // 故此处只需排掉非人形：动物等。
                if (!pawn.RaceProps.Humanlike)
                {
                    continue;
                }

                if (pawn.IsPrisonerOfColony)
                {
                    result.Prisoners++;
                }
                else if (pawn.Faction != null && pawn.IsSlaveOfColony)
                {
                    result.Slaves++;
                }
                else if (IsTemporaryMember(pawn, player))
                {
                    result.Temporary++;
                }
                else if (pawn.IsFreeNonSlaveColonist)
                {
                    result.Colonists++;
                }
            }
            return result;
        }

        /// <summary>
        /// 把一台友方机械族按其型号名累加进快照。型号取 <c>PawnKindDef.LabelCap</c>
        /// （机械族的「型号」在游戏里就是它的兵种，如清扫机 / 建造机）。
        /// </summary>
        private static void CountMechByKind(ColonySnapshot snapshot, Pawn mech)
        {
            if (mech.kindDef == null)
            {
                return;
            }
            string kindLabel = mech.kindDef.LabelCap;
            if (string.IsNullOrEmpty(kindLabel))
            {
                return;
            }

            int count;
            snapshot.MechsByKind.TryGetValue(kindLabel, out count);
            snapshot.MechsByKind[kindLabel] = count + 1;
        }

        /// <summary>
        /// 是否为我方「临时成员」：任务寄居者（难民、来做客的等）或由我方招待的来客。
        /// 两个判据都走引擎自身语义（<c>QuestUtility.IsQuestLodger</c> 与 <c>guest</c> 的招待状态），
        /// 不自行猜 Def。
        /// </summary>
        private static bool IsTemporaryMember(Pawn pawn, Faction player)
        {
            if (pawn.IsQuestLodger())
            {
                return true;
            }
            return pawn.HostFaction == player
                && pawn.guest != null
                && pawn.guest.GuestStatus == GuestStatus.Guest;
        }

        /// <summary>
        /// 拼「另有 N 名临时成员、M 名囚犯、K 名奴隶」子句：只列人数非零的类别；
        /// 三类全为零时返回空串（此时整句只有殖民者数）。
        /// </summary>
        private static string FormatPopulationExtras(ColonySnapshot snapshot)
        {
            List<string> items = new List<string>();
            if (snapshot.Temporary > 0)
            {
                items.Add(string.Format(
                    FactionKnowledgeConfig.PLAYER_SEG_POPULATION_EXTRA_ITEM,
                    snapshot.Temporary,
                    FactionKnowledgeConfig.PLAYER_POP_LABEL_TEMPORARY));
            }
            if (snapshot.Prisoners > 0)
            {
                items.Add(string.Format(
                    FactionKnowledgeConfig.PLAYER_SEG_POPULATION_EXTRA_ITEM,
                    snapshot.Prisoners,
                    FactionKnowledgeConfig.PLAYER_POP_LABEL_PRISONER));
            }
            if (snapshot.Slaves > 0)
            {
                items.Add(string.Format(
                    FactionKnowledgeConfig.PLAYER_SEG_POPULATION_EXTRA_ITEM,
                    snapshot.Slaves,
                    FactionKnowledgeConfig.PLAYER_POP_LABEL_SLAVE));
            }
            if (items.Count == 0)
            {
                return string.Empty;
            }
            return FactionKnowledgeConfig.PLAYER_SEG_POPULATION_EXTRA_LEAD
                + string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, items.ToArray());
        }

        /// <summary>
        /// 我方派系 ① 身份段之内追加的开局剧本句：写出本局所选剧本名（如「迫降」「失落的部落」）。
        /// 读不到剧本（非游戏场景）或剧本名为空时整句省略。
        /// ⚠ 剧本名**不进质变指纹**：整局不变，只有切换语言时才会变（本地化文本），
        ///   更新交由覆写冷却控制。取值 <c>Find.Scenario.name</c>，即玩家在剧本界面看到的原文。
        /// </summary>
        private static void AppendScenarioSection(StringBuilder builder)
        {
            Scenario scenario = Find.Scenario;
            if (scenario == null || string.IsNullOrEmpty(scenario.name))
            {
                return;
            }

            // 剧本名来自 Def / 创意工坊 / 玩家自定义剧本，属外部文本；
            // 上游按 \n 切行，故与其他外部文本一样先压成单行，避免破坏导入导出格式
            string scenarioName = FactionKnowledgeBuilder.ToSingleLine(scenario.name);
            builder.Append(string.Format(FactionKnowledgeConfig.PLAYER_SEG_SCENARIO, scenarioName));
        }

        /// <summary>
        /// 我方派系 ③ 友方机械族段：按型号列出「N 台某型」。
        /// 未启用 Biotech 时整段跳过（此时不存在机械族）；启用但尚未拥有机械族时写明「没有」。
        /// ⚠ 型号构成**不进质变指纹**：机械族会持续充放电、机械师更替也频繁，
        ///   其更新交由覆写冷却控制（见 <see cref="AppendSections"/> 的返回说明）。
        /// </summary>
        private static void AppendMechSection(StringBuilder builder, ColonySnapshot snapshot)
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            List<string> items = FormatMechComposition(snapshot);
            builder.Append(items.Count == 0
                ? FactionKnowledgeConfig.PLAYER_SEG_MECHS_NONE
                : string.Format(
                    FactionKnowledgeConfig.PLAYER_SEG_MECHS,
                    string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, items.ToArray())));
        }

        /// <summary>
        /// 把「型号 → 台数」整理成「N 台某型」列表。
        /// 按型号名排序（而非遍历顺序）：遍历顺序会随后续增删而变，排序后内容稳定可复现。
        /// </summary>
        private static List<string> FormatMechComposition(ColonySnapshot snapshot)
        {
            List<string> kindLabels = new List<string>(snapshot.MechsByKind.Keys);
            kindLabels.Sort(StringComparer.Ordinal);

            List<string> items = new List<string>();
            for (int i = 0; i < kindLabels.Count; i++)
            {
                int count = snapshot.MechsByKind[kindLabels[i]];
                if (count <= 0)
                {
                    continue;
                }
                items.Add(string.Format(
                    FactionKnowledgeConfig.PLAYER_SEG_MECHS_ITEM, count, kindLabels[i]));
            }
            return items;
        }

        /// <summary>
        /// 我方派系 ⑤ 气候段：主基地所属生物群系名（如「温带森林」）。
        /// 尚无主基地（例如刚开档还没落地）时整段省略。
        /// </summary>
        private static void AppendClimateSection(StringBuilder builder)
        {
            string biomeLabel = GetHomeBiomeLabel();
            if (!string.IsNullOrEmpty(biomeLabel))
            {
                builder.Append(string.Format(FactionKnowledgeConfig.PLAYER_SEG_CLIMATE, biomeLabel));
            }
        }

        /// <summary>
        /// 主基地所属生物群系名（<c>Map.Biome.LabelCap</c>，与游戏内世界地图口径一致）；
        /// 读不到主基地或群系时返回 <c>null</c>，调用方据此省略气候段。
        /// </summary>
        private static string GetHomeBiomeLabel()
        {
            Map home = Find.AnyPlayerHomeMap;
            if (home == null || home.Biome == null)
            {
                return null;
            }
            return home.Biome.LabelCap;
        }

        /// <summary>
        /// 本殖民地据点的名称：遍历玩家地图取 <c>IsPlayerHome</c> 者。
        /// 排除飞船着陆生成的地图——那种地图的名字由飞船名派生，会与「逆重飞船」段重复。
        /// </summary>
        private static List<string> CollectSettlementNames()
        {
            List<string> result = new List<string>();
            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return result;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !map.IsPlayerHome || map.wasSpawnedViaGravShipLanding)
                {
                    continue;
                }
                MapParent parent = map.Parent;
                if (parent == null)
                {
                    continue;
                }
                string label = parent.LabelCap;
                if (!string.IsNullOrEmpty(label) && !result.Contains(label))
                {
                    result.Add(label);
                }
            }
            return result;
        }

        /// <summary>
        /// 收集我方逆重飞船（需 Odyssey）：判定与取名都走引擎自身逻辑，不自行猜 Def。
        /// </summary>
        public static List<PlayerGravship> CollectGravships()
        {
            List<PlayerGravship> result = new List<PlayerGravship>();
            if (!ModsConfig.OdysseyActive)
            {
                return result;
            }

            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return result;
            }

            int ticks = CurrentTicks;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !GravshipUtility.PlayerHasGravEngine(map))
                {
                    continue;
                }

                if (!GravshipUtility.TryGetNameOfGravshipOnMap(map, out string gravshipName) ||
                    string.IsNullOrEmpty(gravshipName))
                {
                    continue;
                }

                PlayerGravship item = new PlayerGravship { Name = gravshipName };
                if (map.wasSpawnedViaGravShipLanding)
                {
                    int stay = ticks - map.generationTick;
                    if (stay > 0)
                    {
                        item.StayTicks = stay;
                    }
                }
                result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// 主基地地图的财富总值；读不到时返回 <c>null</c>，调用方据此省略财富段。
        /// </summary>
        /// <remarks>
        /// <c>WealthTotal</c> 的 getter 会按需触发一次**全图财富重算**（间隔 5000 tick），
        /// 重算里按 <c>cachedTerrainMarketValue[terrainDef.index]</c> 取地形价值。
        /// 若地图上存在「未被 <c>DefDatabase</c> 登记」的地形（其 <c>Def.index</c> 停在默认值
        /// <c>ushort.MaxValue</c>），该索引会越界抛 <c>IndexOutOfRangeException</c>——
        /// 这是外部数据异常，不应连累整条我方派系条目，故在此就地降级为「省略财富段」并只告警一次。
        /// </remarks>
        private static int? GetHomeWealth()
        {
            Map home = Find.AnyPlayerHomeMap;
            if (home == null || home.wealthWatcher == null)
            {
                return null;
            }

            try
            {
                return (int)home.wealthWatcher.WealthTotal;
            }
            catch (Exception exception)
            {
                KnowledgeLog.WarnOnce(
                    KnowledgeLogConfig.MODULE_BUILDER,
                    FactionKnowledgeConfig.LOG_KEY_PLAYER_WEALTH_UNAVAILABLE,
                    string.Format(FactionKnowledgeConfig.WARN_PLAYER_WEALTH_UNAVAILABLE, exception.Message));
                return null;
            }
        }

        /// <summary>把 tick 数转成游戏本地化的「X 年 Y 天」式时长文本（不含秒）。</summary>
        private static string FormatDuration(int ticks)
        {
            return GenDate.ToStringTicksToPeriod(ticks, allowSeconds: false);
        }

        /// <summary>当前游戏 tick；<c>TickManager</c> 不可用时返回 0。</summary>
        private static int CurrentTicks
        {
            get { return Find.TickManager != null ? Find.TickManager.TicksGame : 0; }
        }
    }

    /// <summary>一艘我方逆重飞船的展示信息。</summary>
    public sealed class PlayerGravship
    {
        /// <summary>飞船名（取自地图上的重力引擎）。</summary>
        public string Name;

        /// <summary>
        /// 着陆停留 tick，**读不到时为 <c>null</c>**。
        /// 只有飞船自己生成的地图才可反推（其生成时刻即着陆时刻）；
        /// 停靠在主基地等已有地图上时没有任何字段记录着陆时刻，此时宁可省略也不编造。
        /// </summary>
        public int? StayTicks;
    }
}
