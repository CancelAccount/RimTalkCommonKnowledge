using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 派系常识的筛选与文案构建。
    /// 内容强制**单行无换行**：上游导入导出按 <c>\n</c> 切行，换行会破坏格式。
    /// </summary>
    public static class FactionKnowledgeBuilder
    {
        /// <summary>
        /// 收集本次需要注入常识的派系，**含玩家自己的派系与隐藏派系**。
        /// 隐藏派系（原版机械族 / 虫族，以及 mod 自带的世界观派系）同样注入——它们常是世界观的组成部分；
        /// 但这类派系缺数据（无自定义名、无与玩家的关系、无领袖、无意识形态），
        /// 各段模板会逐段判缺并跳过，**不写引擎兜底值或臆造值**（见 <see cref="TryBuild"/> 的分支说明）。
        /// 仅排除**临时派系**（<c>Faction.temporary</c>：任务期的临时势力，随任务生灭）。
        /// 遍历的是 <c>FactionManager</c> 的内部列表引用，**只读不增删**。
        /// </summary>
        public static List<Faction> CollectTargets()
        {
            List<Faction> result = new List<Faction>();
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                Faction faction = all[i];
                if (faction == null || faction.temporary)
                {
                    continue;
                }
                result.Add(faction);
            }
            return result;
        }

        /// <summary>
        /// 为单个派系构建一条常识。普通派系走「身份 / 意识形态 / 关系 / 领袖据点 / 成员构成」五段；
        /// 玩家自己的派系走「身份 / 人口 / 机械族 / 据点 / 气候 / 逆重飞船 / 建立时长 / 财富」八段。
        /// **隐藏派系同样走普通五段**，但每段都按「数据是否真实可读」判缺：读不到即整段（或整句）跳过，
        /// 不写「科技水平为Undefined」「关系是中立」这类引擎兜底值 / 不适用值。
        /// 派系名不可读时返回 <c>false</c>：不写入无主键的条目，避免白占注入名额。
        /// </summary>
        /// <param name="faction">目标派系。</param>
        /// <param name="suppressDescription">
        /// 为真时**省略「定义原文」句**：用于社区常识库已覆盖该 Def（社区条目已是定义原文的改写）的情形，
        /// 避免同一段介绍被写两遍。只影响定义原文，动态段照常输出。我方派系条目不受此参数影响
        /// （玩家侧模板里本就没有定义原文段）。
        /// </param>
        /// <param name="entry">输出：本次构建的全部字段。</param>
        public static bool TryBuild(Faction faction, bool suppressDescription, out FactionKnowledgeEntry entry)
        {
            entry = null;

            if (faction == null || faction.def == null)
            {
                return false;
            }

            // Faction.Name 在 HasName 为假时已自动回退 def.LabelCap
            string name = faction.Name;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // 内容里用原名（给人看），标签与主键用净化名（给上游切分）
            string primaryKey = SanitizeTagPart(name);

            StringBuilder builder = new StringBuilder();
            builder.Append(FactionKnowledgeConfig.FACTION_CONTENT_PREFIX);

            string qualitativeKey;
            string tag;
            if (faction.IsPlayer)
            {
                // 「本殖民地」专属逻辑整体在 PlayerColonyKnowledgeBuilder 里，这里只做分派。
                // 飞船名既进内容也进标签：玩家提到飞船名时也应能带出我方派系的介绍，
                // 故一次性收集、两处共用同一份，避免重复遍历与口径漂移。
                List<PlayerGravship> gravships = PlayerColonyKnowledgeBuilder.CollectGravships();
                qualitativeKey = PlayerColonyKnowledgeBuilder.AppendSections(builder, name, gravships);
                tag = PlayerColonyKnowledgeBuilder.BuildTag(primaryKey, gravships);
            }
            else
            {
                AppendIdentitySection(builder, faction, name, suppressDescription);
                AppendIdeologySection(builder, faction);
                AppendRelationSection(builder, faction, name);
                AppendLeaderAndSettlementSection(builder, faction, name);
                AppendMemberCompositionSection(builder, faction, name);
                qualitativeKey = BuildQualitativeKey(faction);
                tag = BuildTag(faction, primaryKey);
            }

            entry = new FactionKnowledgeEntry
            {
                PrimaryKey = primaryKey,
                Content = builder.ToString(),
                Tag = tag,
                QualitativeKey = qualitativeKey
            };
            return true;
        }

        /// <summary>
        /// 拼普通派系的触发标签：主词为派系名；再并列该派系**唯一的特征词**——
        /// 优先「唯一成员种族」（HAR / alien race 派系，其种族不在异种人体系里），
        /// 否则「唯一显式异种人」（如赫血种 / 骠骑种派系）。
        /// 这样玩家提到种族名或异种人名时也能带出该派系的常识。
        /// 我方派系不走这里（标签含飞船名，且内容不含成员构成段），见
        /// <see cref="PlayerColonyKnowledgeBuilder.BuildTag"/>。
        /// ⚠ 只能写进 tag：上游只认 tag（<c>GetTags()</c> 按 5 种分隔符切分），
        ///   <c>CommonKnowledgeEntry.keywords</c> 字段只被序列化、不参与任何匹配。
        /// </summary>
        private static string BuildTag(Faction faction, string primaryKey)
        {
            // 关闭双向引用后，派系条目不再含成员构成段，标签也不应并列特征异种人名 / 成员种族名——
            // 否则会出现「提到某异种人却带出不含该信息的条目」的错配。
            if (!FactionInfoSettings.IncludeFactionXenotypeRefs)
            {
                return primaryKey;
            }

            string featureLabel = null;
            ThingDef primaryRace = RaceKnowledgeBuilder.FindPrimaryMemberRace(faction);
            if (primaryRace != null)
            {
                featureLabel = primaryRace.LabelCap;
            }
            else
            {
                XenotypeDef dominantXenotype = XenotypeKnowledgeBuilder.FindDominantXenotype(faction);
                if (dominantXenotype != null)
                {
                    featureLabel = dominantXenotype.LabelCap;
                }
            }

            if (string.IsNullOrEmpty(featureLabel))
            {
                return primaryKey;
            }

            string sanitizedFeature = SanitizeTagPart(featureLabel);
            if (string.IsNullOrEmpty(sanitizedFeature))
            {
                return primaryKey;
            }
            return primaryKey + FactionKnowledgeConfig.TAG_SEPARATOR + sanitizedFeature;
        }

        /// <summary>
        /// 净化标签用词：上游按 5 种分隔符把标签切成多段，名字里若含这些字符，
        /// 主键比对会失效、条目将反复堆积；故把分隔符一律替换为空格（不删字，避免两侧文字粘连）。
        /// 我方派系列条也走同一实现（见 <see cref="PlayerColonyKnowledgeBuilder.BuildTag"/>），故为 <c>public</c>。
        /// </summary>
        public static string SanitizeTagPart(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            char[] separators = FactionKnowledgeConfig.TAG_SEPARATOR_CHARS;
            StringBuilder builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                builder.Append(Array.IndexOf(separators, c) >= 0 ? ' ' : c);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 普通派系的质变指纹：可谈关系的派系取关系种类（敌对 / 中立 / 盟友）。
        /// **隐藏 / 临时派系改用固定键**——它们的 <c>HasGoodwill</c> 为假，引擎里根本没有与玩家的关系条目，
        /// 硬取 <c>PlayerRelationKind</c> 会先打一条引擎 <c>Log.Error</c>（"has null relation with …"）
        /// 再返回默认中立这种兜底值，既污染日志又让指纹失真。
        /// </summary>
        private static string BuildQualitativeKey(Faction faction)
        {
            return faction.HasGoodwill
                ? faction.PlayerRelationKind.ToString()
                : FactionKnowledgeConfig.QUALITATIVE_KEY_NO_RELATION;
        }

        /// <summary>
        /// ① 基础身份：派系名 + 类型标签 + 科技水平，随后追加定义原文（原文为空则省略该句）。
        /// 两处判缺降级：① <c>FactionDef.techLevel</c> 为 <c>TechLevel.Undefined</c>（Def 未声明）→ 省略科技水平；
        /// ② 名字与类型标签同字（隐藏派系 <c>Name</c> 回退为 <c>def.LabelCap</c>）→ 省去「是一支 X 派系」。
        /// <paramref name="suppressDescription"/> 为真时也不写定义原文句（社区常识库已覆盖该 Def，避免重复介绍）。
        /// </summary>
        private static void AppendIdentitySection(
            StringBuilder builder, Faction faction, string name, bool suppressDescription)
        {
            if (!FactionInfoSettings.IncludeIdentity)
            {
                return;
            }

            // 名称，科技以及类型标签是否和 Def 标签同字
            string defLabel = faction.def.LabelCap;
            bool hasTechLevel = faction.def.techLevel != TechLevel.Undefined;
            string techLevel = hasTechLevel ? faction.def.techLevel.ToStringHuman() : null;
            bool sameAsTypeLabel = !string.IsNullOrEmpty(defLabel)
                && string.Equals(name, defLabel, StringComparison.Ordinal);

            if (sameAsTypeLabel)
            {
                builder.Append(hasTechLevel
                    ? string.Format(FactionKnowledgeConfig.FACTION_SEG_IDENTITY_SAME, name, techLevel)
                    : string.Format(FactionKnowledgeConfig.FACTION_SEG_IDENTITY_SAME_NO_TECH, name));
            }
            else
            {
                builder.Append(hasTechLevel
                    ? string.Format(FactionKnowledgeConfig.FACTION_SEG_IDENTITY, name, defLabel, techLevel)
                    : string.Format(FactionKnowledgeConfig.FACTION_SEG_IDENTITY_NO_TECH, name, defLabel));
            }

            // 社区常识库已覆盖该 Def 时让位：社区条目本就是这段描述的改写（更详细 / 更个性化），
            // 再写一遍只是重复；只省略这一句，关系 / 好感度 / 领袖 / 据点 / 成员等动态段照常注入。
            if (suppressDescription)
            {
                return;
            }

            // 取 description **字段**（Def 继承来的原始介绍），不要取 FactionDef.Description 属性：
            // 后者会追加「成员异种人概率」段且用 \n 换行，会破坏上游导入导出格式
            string description = ToSingleLine(faction.def.description);
            if (!string.IsNullOrEmpty(description))
            {
                builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_DESCRIPTION, description));
            }
        }

        /// <summary>② 意识形态：主理念名 + 其信条列表；无 Ideology DLC / 非人形派系 / 无主理念 → 整段跳过。</summary>
        private static void AppendIdeologySection(StringBuilder builder, Faction faction)
        {
            if (!FactionInfoSettings.IncludeIdeology)
            {
                return;
            }

            // faction.ideos 非人形派系为 null
            if (!ModsConfig.IdeologyActive || faction.ideos == null)
            {
                return;
            }
            Ideo ideo = faction.ideos.PrimaryIdeo;
            if (ideo == null)
            {
                return;
            }

            // Ideo 的名称是公有字段 name（小写）
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

        /// <summary>
        /// ③ 与我方关系：关系标签 + 好感度数值。
        /// **无可谈关系的派系整段跳过**（隐藏 / 临时派系的 <c>HasGoodwill</c> 为假）：
        /// 引擎里没有它们与玩家的关系条目，硬取会落入 <c>RelationWith(other, allowNull: false)</c>
        /// 的「缺失关系」分支——先打一条引擎 <c>Log.Error</c>、再返回默认中立，那是兜底值而非真实数据。
        /// </summary>
        private static void AppendRelationSection(StringBuilder builder, Faction faction, string name)
        {
            if (!FactionInfoSettings.IncludeRelation)
            {
                return;
            }

            if (!faction.HasGoodwill)
            {
                return;
            }

            builder.Append(string.Format(
                FactionKnowledgeConfig.FACTION_SEG_RELATION, name, faction.PlayerRelationKind.GetLabelCap()));
            builder.Append(string.Format(
                FactionKnowledgeConfig.FACTION_SEG_GOODWILL, faction.PlayerGoodwill));
        }

        /// <summary>
        /// ④ 领袖与据点：领袖姓名（无领袖则写明，但隐藏派系整句省略）+ 据点数量 + 可读时的最近据点距离。
        /// 无据点时据点句整句省略（写「共有 0 处定居点」没有信息量）。
        /// </summary>
        private static void AppendLeaderAndSettlementSection(StringBuilder builder, Faction faction, string name)
        {
            if (!FactionInfoSettings.IncludeSettlements)
            {
                return;
            }

            Pawn leader = faction.leader;
            if (leader != null)
            {
                string leaderName = leader.NameFullColored;
                builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_LEADER, name, leaderName));
            }
            else if (!faction.Hidden)
            {
                // 隐藏派系（机械族 / 虫族等）本就不存在「领袖」这一概念，
                // 对它们写「目前没有已知的领袖」等于把「不适用」当事实陈述，故整句省略；
                // 非隐藏派系无领袖仍是有效信息（如领袖空缺中的部落）。
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

            // 据点句整句省略：无据点时写「共有 0 处定居点」没有信息量
            if (count == 0)
            {
                return;
            }
            builder.Append(string.Format(FactionKnowledgeConfig.FACTION_SEG_SETTLEMENT_COUNT, count));

            // 多层星球（Odyssey）下距离可能取到 int.MaxValue 等无效值 → 省略距离子句
            if (nearest > 0f && nearest < int.MaxValue)
            {
                int distanceTiles = (int)Math.Round(nearest);
                builder.Append(string.Format(
                    FactionKnowledgeConfig.FACTION_SEG_SETTLEMENT_NEAREST, distanceTiles));
            }
        }

        /// <summary>
        /// ⑤ 成员面貌：优先按**成员种族**表达（非人类人形种族，如 HAR / alien race 派系）；
        /// 否则按游戏自身口径（<c>CollectMemberComposition</c>）列出各异种人占比。
        /// 两者**互斥**：含非人类人形种族时，异种人口径只会退化为「智人种」（HAR 种族在 Biotech 里
        /// 就登记为 <c>Baseliner</c>），写出即误导，故让位给种族句。
        /// 未启用 Biotech / 非人形派系 / **派系级分布不可信**（<c>IsFactionCompositionUnreliable</c>）→ 整段跳过。
        /// </summary>
        private static void AppendMemberCompositionSection(StringBuilder builder, Faction faction, string name)
        {
            // 「成员构成」段属于派系与异种人的双向引用（FR-7 / D13）：关闭后整段缺席。
            if (!FactionInfoSettings.IncludeFactionXenotypeRefs)
            {
                return;
            }

            // 种族优先：HAR / alien race 派系的成员在 Biotech 异种人体系里一律是智人种，
            // 写异种人构成只会得到「智人种 100%」这类误导内容。
            List<ThingDef> races = RaceKnowledgeBuilder.CollectMemberRaces(faction);
            if (races.Count > 0)
            {
                builder.Append(string.Format(
                    FactionKnowledgeConfig.FACTION_SEG_MEMBER_RACES, JoinRaceLabels(races)));
                return;
            }

            List<XenotypeChance> composition = XenotypeKnowledgeBuilder.CollectMemberComposition(faction);
            if (composition == null || composition.Count == 0)
            {
                return;
            }

            // 派系级分布不可信时整段跳过（宁可沉默，不给错误的世界观常识）：
            // 派系级 xenotypeSet 缺失、异种人只写在兵种上 → 照抄会写出「智人种 100%」。
            if (XenotypeKnowledgeBuilder.IsFactionCompositionUnreliable(faction))
            {
                KnowledgeLog.Detail("跳过成员构成段（派系级异种人分布不可读）：" + name);
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
        /// 构建派系界面（派系列表悬停提示 / 派系信息卡）用的「实际成员」文本块；
        /// 无可补充内容时返回 <c>null</c>。供 Harmony 补丁调用，见 <c>FactionCompositionUiPatch</c>。
        /// </summary>
        /// <remarks>
        /// 与常识 ⑤ 段同口径，但**只输出游戏自身看不出的部分**（避免与游戏自带文本重复）：
        /// ① 含非人类人形种族（HAR / alien race）→ 列种族；
        /// ② 否则派系级异种人分布不可信（<c>xenotypeSet</c> 缺失、异种人只写在兵种上）→ 列兵种级异种人；
        /// ③ 其余情形返回 <c>null</c>——此时游戏自带的「成员异种人概率」已正确，再写一遍只是噪声。
        /// 只读 <see cref="FactionDef"/> 级数据，**不需要 <see cref="Faction"/> 实例**：
        /// 该补丁的宿主 <c>FactionDef.Description</c> 可能在任何游戏状态下被访问（含无存档的界面）。
        /// </remarks>
        /// <param name="def">目标派系 Def。</param>
        public static string BuildUiCompositionBlock(FactionDef def)
        {
            if (def == null)
            {
                return null;
            }

            List<ThingDef> races = RaceKnowledgeBuilder.CollectMemberRaces(def);
            if (races.Count > 0)
            {
                // 种族在 Def 层没有概率声明（PawnKindDef.race 是单一确定值），只列名称、不写占比；
                // 排版对齐原版成员段：标题行 + 每行「  - 名称」。
                return BuildNameListBlock(
                    FactionKnowledgeConfig.UI_COMPOSITION_MEMBER_RACES_TITLE, RaceLabels(races));
            }

            if (!XenotypeKnowledgeBuilder.IsFactionCompositionUnreliable(def))
            {
                return null;
            }

            List<XenotypeDef> kindLevel = XenotypeKnowledgeBuilder.CollectKindLevelXenotypes(def);
            if (kindLevel.Count == 0)
            {
                return null;
            }
            return string.Format(
                FactionKnowledgeConfig.UI_COMPOSITION_KIND_XENOTYPES, JoinXenotypeLabels(kindLevel));
        }

        /// <summary>
        /// 按游戏原版成员段的排版拼一个「名称块」：着色标题行 + 换行 + 每行「  - 名称」。
        /// 原版范式见 <c>FactionDef.Description</c>：
        /// <c>("\n\n" + (标题 + ":").AsTipTitle() + "\n") + 各项.ToLineList("  - ", false)</c>；
        /// 前导空行由调用方（<see cref="FactionCompositionUi.Append"/>）负责，本方法只出「标题 + 换行 + 列表」。
        /// </summary>
        /// <param name="title">块标题（不加冒号，方法内补）。</param>
        /// <param name="labels">名称列表，至少一项。</param>
        /// <returns>可直接追加到派系介绍末尾的多行文本。</returns>
        private static string BuildNameListBlock(string title, List<string> labels)
        {
            return (title + ":").AsTipTitle() + "\n" +
                labels.ToLineList(FactionKnowledgeConfig.UI_LIST_ITEM_PREFIX, false);
        }

        /// <summary>取种族名的展示列表（<c>LabelCap</c>）。</summary>
        private static List<string> RaceLabels(List<ThingDef> races)
        {
            List<string> labels = new List<string>();
            for (int i = 0; i < races.Count; i++)
            {
                labels.Add(races[i].LabelCap);
            }
            return labels;
        }

        /// <summary>把种族列表拼成顿号连接的名称串（注入内容展示用，须单行）。</summary>
        private static string JoinRaceLabels(List<ThingDef> races)
        {
            return string.Join(
                FactionKnowledgeConfig.LIST_SEPARATOR, RaceLabels(races).ToArray());
        }

        /// <summary>把异种人列表拼成顿号连接的名称串（内容展示用）。</summary>
        private static string JoinXenotypeLabels(List<XenotypeDef> xenotypes)
        {
            List<string> labels = new List<string>();
            for (int i = 0; i < xenotypes.Count; i++)
            {
                labels.Add(xenotypes[i].LabelCap);
            }
            return string.Join(FactionKnowledgeConfig.LIST_SEPARATOR, labels.ToArray());
        }

        /// <summary>
        /// 取玩家自己的第一处据点作为距离参照点。
        /// 照抄引擎自身写法：遍历 <c>Find.WorldObjects.Settlements</c> 取 <c>Faction == Faction.OfPlayer</c> 的第一个。
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

        /// <summary>
        /// 把多行文本压成单行：换行（含 Windows 的 <c>\r</c>）一律替换为空格。
        /// 我方派系的条目构建也用同一实现处理剧本名等外部文本（见 <see cref="PlayerColonyKnowledgeBuilder"/>），故为 <c>public</c>。
        /// </summary>
        public static string ToSingleLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }

    /// <summary>
    /// 一次派系常识构建的全部产出。
    /// 用类聚拢而不是一串 <c>out</c> 参数，避免调用方按位置取值时错位。
    /// </summary>
    public sealed class FactionKnowledgeEntry
    {
        /// <summary>完整触发标签（可能并列特征异种人名）。</summary>
        public string Tag;

        /// <summary>稳定主键：净化后的派系名，用于查库与管理本 mod 条目。</summary>
        public string PrimaryKey;

        /// <summary>单行注入内容。</summary>
        public string Content;

        /// <summary>质变指纹：与上次写入不同即视为质变，不受覆写冷却限制。</summary>
        public string QualitativeKey;
    }
}
