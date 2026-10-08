using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 注入时机与内容校正的挂载点。
    /// 靠 <c>GameComponent</c> 子类被引擎自动发现，**不需要 Def、不需要 Harmony**。
    /// 构造签名必须是 <c>(Game game)</c>：引擎用 <c>Activator.CreateInstance(type, game)</c> 反射实例化。
    /// </summary>
    public class FactionKnowledgeComponent : GameComponent
    {
        /// <summary>
        /// 上次实际写入的好感度（<c>faction.loadID</c> → 数值）。
        /// 仅用于「只有好感度小幅波动」的判定，**不持久化**：
        /// 读档后为空，会保守地重写一次，从而顺带校正存档期间产生的陈旧内容。
        /// </summary>
        private readonly Dictionary<int, int> lastWrittenGoodwill = new Dictionary<int, int>();

        /// <summary>
        /// 上次写入内容时的游戏 tick（<c>faction.loadID</c> → <c>TicksGame</c>），用于非质变覆写的冷却判定。
        /// **不持久化**：读档后为空则视为冷却已结束，允许立即校正一次陈旧内容。
        /// </summary>
        private readonly Dictionary<int, int> lastWriteTick = new Dictionary<int, int>();

        /// <summary>
        /// 上次写入时该派系的**质变指纹**（<c>faction.loadID</c> → 指纹）。
        /// 普通派系的指纹是关系种类（敌对 / 中立 / 盟友）；
        /// 我方派系的指纹是「派系名 + 据点集合 + 飞船集合 + 殖民者数 + 奴隶数」。
        /// 指纹变化即「质变」，不受覆写冷却限制。**不持久化**。
        /// </summary>
        private readonly Dictionary<int, string> lastQualitativeKey = new Dictionary<int, string>();

        /// <summary>
        /// 引擎反射实例化所需的构造：
        /// <c>Game.FillComponents()</c> 用 <c>Activator.CreateInstance(type, this)</c> 传 Game，
        /// 因此子类**必须**有 <c>(Game game)</c> 构造；
        /// 但 <c>GameComponent</c> 自身**没有** <c>(Game)</c> 构造（只有一个隐式无参构造），
        /// 故此处**不能**写 <c>: base(game)</c>（会报 CS1729）。引擎自带的
        /// <c>GameComponent_PsychicRitualManager(Game game)</c> 即为此写法。
        /// </summary>
        public FactionKnowledgeComponent(Game game)
        {
        }

        /// <summary>新开档：地图生成完毕、玩家派系与初始关系均已就绪后调用。</summary>
        public override void StartedNewGame()
        {
            RunInitialSync();
        }

        /// <summary>
        /// 读档：每次进入存档都调用；内容一致则完全不写，陈旧则自动校正。
        /// 受设置项 <c>enableBackfillOnLoad</c> 控制（D4）：关闭后读档不做任何写入，
        /// 此时仍可靠新开档与运行中定时校正在下次变化时补齐。
        /// </summary>
        public override void LoadedGame()
        {
            if (!FactionInfoSettings.EnableBackfillOnLoad)
            {
                KnowledgeLog.Summary(FactionKnowledgeConfig.LOG_BACKFILL_ON_LOAD_DISABLED);
                return;
            }
            RunInitialSync();
        }

        /// <summary>
        /// 设置页「立即对当前存档重新注入」按钮的入口：对当前存档重跑一次注入与校正。
        /// 供玩家改完设置后立即看效果、无需重开档；与读档校正开关无关，但**仍受总开关约束**。
        /// 无存档或找不到本组件时静默返回（按钮在无存档时已置灰）。
        /// </summary>
        public static void ReinjectCurrentGame()
        {
            Game game = Current.Game;
            if (game == null)
            {
                return;
            }

            FactionKnowledgeComponent component = game.GetComponent<FactionKnowledgeComponent>();
            if (component != null)
            {
                component.RunInitialSync();
            }
        }

        /// <summary>
        /// 新开档与读档共用的首次同步：先导入随包预设库（FR-6 / D44），再派系、后异种人，最后补一条合计。
        /// 各路各自已有计数汇总，此处再给一行「总计」，便于一眼确认本次注入了多少条。
        /// 总开关关闭时整体短路：只回报一句结论，不产生任何写入。
        /// </summary>
        private void RunInitialSync()
        {
            if (!FactionInfoSettings.EnableInjection)
            {
                KnowledgeLog.Summary(FactionKnowledgeConfig.LOG_INJECTION_DISABLED);
                return;
            }

            // 预设库导入（FR-6 / D44）走在前：D43 的「定义原文让位」查重依赖社区条目已入库，
            // 否则本 mod 会先写全、随后社区条目才到，导致让位口径首次不生效。
            KnowledgeBaseImporter.Run();

            int factionWritten = SyncFactionKnowledge(isPeriodic: false);
            int xenotypeWritten = SyncXenotypeKnowledge();
            KnowledgeLog.Summary(string.Format(
                FactionKnowledgeConfig.LOG_INJECTION_TOTAL, factionWritten, xenotypeWritten));
        }

        /// <summary>
        /// 每 tick 由引擎调用（<c>GameComponentUtility.GameComponentTick</c>），
        /// 此处自行节流到每 <see cref="FactionInfoSettings.RefreshIntervalTicks"/>
        /// （默认 1 游戏小时，可由设置页调整）执行一次内容校正。
        /// 关系质变、好感度累积变化、领袖更替、据点增减后，派系常识不会长期停留在开档快照。
        /// 总开关或「运行中定时校正」任一关闭时整体短路；
        /// 异种人条目**不参与**定时刷新：其内容几乎不变，读档补齐即可。
        /// </summary>
        public override void GameComponentTick()
        {
            if (!FactionInfoSettings.EnableInjection || !FactionInfoSettings.EnablePeriodicRefresh)
            {
                return;
            }
            if (!RimTalkMemoryBridge.IsLibraryAvailable || Find.TickManager == null)
            {
                return;
            }

            int interval = FactionInfoSettings.RefreshIntervalTicks;
            if (interval <= 0 || Find.TickManager.TicksGame % interval != 0)
            {
                return;
            }
            SyncFactionKnowledge(isPeriodic: true);
        }

        /// <summary>
        /// 同步全部有效派系常识（含我方派系）。
        /// 对每个派系：库内无条目 → 写入；内容一致 → 只校准基准；内容不同 → 判断下述三种情形。
        /// **质变**（指纹变化：普通派系看关系种类，我方派系看名 / 据点 / 飞船 / 殖民者数 / 奴隶数）立即重写；
        /// **仅好感度小幅波动**（差值未达阈值）直接不写；其余**非质变**改动要等到覆写冷却结束才重写。
        /// 新档、读档、定时轮询**共用这一套逻辑**，因此天然幂等，也不存在「新档必须先删后写」的特殊路径。
        /// 单个派系失败不中断整体（逐个 try/catch）。
        /// </summary>
        /// <param name="isPeriodic">为真表示这是定时轮询：无变化时保持静默，避免日志刷屏。</param>
        /// <returns>本次实际写入（含刷新）的条数，供调用方汇总合计。</returns>
        private int SyncFactionKnowledge(bool isPeriodic)
        {
            int written = 0;
            int skippedConsistent = 0;
            int skippedMinorGoodwill = 0;
            int skippedCooldown = 0;
            int skippedEmpty = 0;

            List<Faction> targets = FactionKnowledgeBuilder.CollectTargets();

            // D21：同名派系（tag 主键相同）不消歧、只告警一次；键 = tag 主键，值 = 先登记的 defName。
            Dictionary<string, string> tagOwnerDefNames = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < targets.Count; i++)
            {
                Faction faction = targets[i];
                try
                {
                    // 定义原文让位（D43）：查重键取 def 名（LabelCap），与社区条目首段标签同口径。
                    // 我方派系为全自建运行时内容，不参与查重，固定不让位。
                    bool suppressDescription =
                        !faction.IsPlayer &&
                        RimTalkMemoryBridge.HasExternalEntryForTag(faction.def.LabelCap);

                    FactionKnowledgeEntry entry;
                    if (!FactionKnowledgeBuilder.TryBuild(faction, suppressDescription, out entry))
                    {
                        skippedEmpty++;
                        KnowledgeLog.Detail("忽略（无派系名）：" + faction.def.defName);
                        continue;
                    }

                    string content = entry.Content;
                    string qualitativeKey = entry.QualitativeKey;

                    // D21：同名派系只告警、不消歧（不阻塞写入）。
                    WarnDuplicateFactionTag(tagOwnerDefNames, faction, entry.PrimaryKey);

                    // 查库口径按派系种类分流：
                    // · 普通派系：主键 = 稳定的派系名（tag 第一段，已净化分隔符），与整串标签无关（尾段会随特征异种人变化）；
                    // · 我方派系：主键会变（派系改名 / 飞船改名都会重拼标签），改用内容里的固定句识别——
                    //   否则改名后按新名查不到旧条目，旧条目会残留成重复项、且提到旧名仍命中陈旧内容。
                    string existingContent;
                    List<string> managedIds = faction.IsPlayer
                        ? RimTalkMemoryBridge.FindManagedIdsByContent(
                            FactionKnowledgeConfig.PLAYER_SEG_IDENTITY_SUFFIX,
                            FactionKnowledgeConfig.FACTION_CONTENT_PREFIX,
                            out existingContent)
                        : RimTalkMemoryBridge.FindManagedIds(
                            entry.PrimaryKey, FactionKnowledgeConfig.FACTION_CONTENT_PREFIX, out existingContent);

                    // ⚠ 玩家派系不能问「与自己的关系」：PlayerGoodwill → RelationWith(自己) 会先 Log.Error，
                    //   接着 GoodwillSituationManager.GetMaxGoodwill 对空关系 NRE。玩家派系本就不写好感度段，取 0 即可。
                    int currentGoodwill =
                        (!faction.IsPlayer && faction.HasGoodwill) ? faction.PlayerGoodwill : 0;
                    bool hasEntry = managedIds.Count > 0;

                    if (hasEntry &&
                        string.Equals(existingContent, content, StringComparison.Ordinal))
                    {
                        // 内容逐字一致：无需任何写入，只校准各项基准
                        lastWrittenGoodwill[faction.loadID] = currentGoodwill;
                        lastWriteTick[faction.loadID] = CurrentTick;
                        lastQualitativeKey[faction.loadID] = qualitativeKey;
                        skippedConsistent++;
                        continue;
                    }

                    if (hasEntry)
                    {
                        // 质变（指纹变化）不受冷却限制，立即重写；其余非质变改动才受冷却约束
                        bool qualitativeChanged = IsQualitativeChanged(faction, qualitativeKey);

                        // 差异仅来自好感度且未达阈值 → 直接不写（此判定独立于冷却）；
                        // 我方派系模板里没有好感度句，该判定不适用，直接跳过
                        if (!qualitativeChanged && !faction.IsPlayer &&
                            ShouldSkipMinorGoodwillChange(faction, existingContent, content, currentGoodwill))
                        {
                            skippedMinorGoodwill++;
                            continue;
                        }

                        if (!qualitativeChanged && !IsRewriteCooldownElapsed(faction))
                        {
                            skippedCooldown++;
                            // 我方派系的内容每小时都在变（财富、时长），若定时轮询也逐条输出，
                            // 这一行会每小时刷一次屏；故轮询期静默，只在首次同步（新档 / 读档）时说明原因。
                            if (!isPeriodic)
                            {
                                KnowledgeLog.Detail("跳过（非质变改动，覆写冷却中）：" + entry.Tag);
                            }
                            continue;
                        }
                    }

                    // 多条时整体清除，避免异常情况下残留（正常幂等只会有 1 条）
                    for (int j = 0; j < managedIds.Count; j++)
                    {
                        RimTalkMemoryBridge.Remove(managedIds[j]);
                    }

                    // 重要度分档（D43）：我方派系对标社区顶级档，其它派系对齐社区「派系本体」档
                    float importance = faction.IsPlayer
                        ? FactionInfoSettings.KnowledgeImportancePlayer
                        : FactionInfoSettings.KnowledgeImportanceOther;
                    string id = RimTalkMemoryBridge.AddLore(entry.Tag, content, importance);
                    if (string.IsNullOrEmpty(id))
                    {
                        KnowledgeLog.Error("写入失败（上游未返回条目 id）：" + entry.Tag);
                        continue;
                    }

                    lastWrittenGoodwill[faction.loadID] = currentGoodwill;
                    lastWriteTick[faction.loadID] = CurrentTick;
                    lastQualitativeKey[faction.loadID] = qualitativeKey;
                    written++;
                    KnowledgeLog.Detail((hasEntry ? "已刷新：" : "已写入：") + content);
                }
                catch (Exception exception)
                {
                    KnowledgeLog.Error(
                        "处理派系失败：" + (faction != null ? faction.Name : "null"), exception);
                }
            }

            if (isPeriodic)
            {
                // 轮询常态就是「什么都没变」，只在真的重写时才说话
                if (written > 0)
                {
                    KnowledgeLog.Summary("派系常识已刷新 " + written + " 条（关系 / 好感度 / 领袖 / 据点 / 我方飞船等有变）。");
                }
                return written;
            }

            KnowledgeLog.Summary(
                "派系常识：候选 " + targets.Count + " 个，" +
                "写入 " + written + " 条，" +
                "跳过 " + skippedConsistent + " 条（内容一致），" +
                "略过 " + skippedMinorGoodwill + " 条（仅好感度小幅波动），" +
                "略过 " + skippedCooldown + " 条（非质变改动，覆写冷却中），" +
                "忽略 " + skippedEmpty + " 个（无派系名）。");
            return written;
        }

        /// <summary>
        /// D21：登记 tag 主键的归属。若同一主键已被**另一个**派系占用（同名派系），
        /// 经 <see cref="KnowledgeLog.WarnOnce"/> 输出一次中文告警（带两个 <c>defName</c>）；
        /// 两条都照常写入、不消歧、不阻塞。
        /// </summary>
        /// <param name="tagOwnerDefNames">本次同步内「tag 主键 → defName」登记表。</param>
        /// <param name="faction">当前派系。</param>
        /// <param name="primaryKey">当前条目的 tag 主键（派系名）。</param>
        private static void WarnDuplicateFactionTag(
            Dictionary<string, string> tagOwnerDefNames, Faction faction, string primaryKey)
        {
            if (string.IsNullOrEmpty(primaryKey))
            {
                return;
            }

            string defName = faction.def != null ? faction.def.defName : faction.Name;
            string previousDefName;
            if (!tagOwnerDefNames.TryGetValue(primaryKey, out previousDefName))
            {
                tagOwnerDefNames[primaryKey] = defName;
                return;
            }

            if (!string.Equals(previousDefName, defName, StringComparison.Ordinal))
            {
                KnowledgeLog.WarnOnce(
                    FactionKnowledgeConfig.LOG_KEY_DUPLICATE_FACTION_TAG,
                    string.Format(
                        FactionKnowledgeConfig.WARN_DUPLICATE_FACTION_TAG, previousDefName, defName));
            }
        }

        /// <summary>
        /// 判断「内容差异是否只源于好感度的小幅波动」，是则本次可以不重写。
        /// </summary>
        /// <remarks>
        /// 判定手法：把库内内容里的「上次写入的好感度句」整句替换为「当前好感度句」——
        /// 因为该句模板固定且数值唯一，替换是安全的。替换后若与新建内容逐字相同，
        /// 说明关系、领袖、据点、成员等**其它各段都没变**，差异只来自好感度数值；
        /// 此时再要求差值达到阈值才重写。
        /// 任何一处不成立（无基准 / 关系句结构变化 / 其它段变化）都返回 <c>false</c>，即照常重写。
        /// </remarks>
        private bool ShouldSkipMinorGoodwillChange(
            Faction faction, string existingContent, string newContent, int currentGoodwill)
        {
            // 关系句 / 好感度句本身的出现与否变了 → 结构性变化，必须重写
            if (!faction.HasGoodwill)
            {
                return false;
            }

            int lastGoodwill;
            if (!lastWrittenGoodwill.TryGetValue(faction.loadID, out lastGoodwill))
            {
                // 无基准（首次校正或刚读档）→ 保守重写，宁可多写一次也不留陈旧内容
                return false;
            }

            string oldSentence = string.Format(FactionKnowledgeConfig.FACTION_SEG_GOODWILL, lastGoodwill);
            string newSentence = string.Format(FactionKnowledgeConfig.FACTION_SEG_GOODWILL, currentGoodwill);
            string normalizedExisting = existingContent.Replace(oldSentence, newSentence);
            if (!string.Equals(normalizedExisting, newContent, StringComparison.Ordinal))
            {
                // 除好感度外还有别的段变了 → 必须重写
                return false;
            }

            int delta = currentGoodwill - lastGoodwill;
            if (delta < 0)
            {
                delta = -delta;
            }
            return delta < FactionInfoSettings.GoodwillRefreshThreshold;
        }

        /// <summary>当前游戏 tick；<c>TickManager</c> 不可用时返回 0。</summary>
        private static int CurrentTick
        {
            get { return Find.TickManager != null ? Find.TickManager.TicksGame : 0; }
        }

        /// <summary>
        /// 该派系的质变指纹相对**上次写入**是否发生了变化——即「质变」。
        /// 质变意味着内容里的核心事实被推翻，必须立刻改写，因此**不受**覆写冷却约束。
        /// </summary>
        /// <remarks>无基准（首次校正或刚读档）时返回 <c>true</c>：既然内容已经不一致，就立即校正一次。</remarks>
        private bool IsQualitativeChanged(Faction faction, string qualitativeKey)
        {
            string lastKey;
            if (!lastQualitativeKey.TryGetValue(faction.loadID, out lastKey))
            {
                return true;
            }
            return !string.Equals(lastKey, qualitativeKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// 非质变覆写的冷却是否已结束。
        /// 冷却时长取 <see cref="KnowledgeDebug.CurrentRewriteCooldownTicks"/>：常态 3 天，调试「快速刷新」开启时 1 小时。
        /// </summary>
        /// <remarks>无记录（首次校正或刚读档）时返回 <c>true</c>：允许立即校正一次陈旧内容。</remarks>
        private bool IsRewriteCooldownElapsed(Faction faction)
        {
            int lastTick;
            if (!lastWriteTick.TryGetValue(faction.loadID, out lastTick))
            {
                return true;
            }
            return CurrentTick - lastTick >= KnowledgeDebug.CurrentRewriteCooldownTicks;
        }

        /// <summary>
        /// 同步异种人常识：缺失才补，不做定时刷新。
        /// 未启用 Biotech DLC 时整体跳过且不报错；
        /// 与派系同步互相独立：本方法自带外层 try/catch，自身失败不影响已完成的派系注入。
        /// </summary>
        /// <returns>本次实际写入的条数（未启用 Biotech 时为 0），供调用方汇总合计。</returns>
        private static int SyncXenotypeKnowledge()
        {
            if (!FactionInfoSettings.IncludeXenotypes)
            {
                KnowledgeLog.Summary(FactionKnowledgeConfig.LOG_XENOTYPE_INJECTION_DISABLED);
                return 0;
            }

            if (!ModsConfig.BiotechActive)
            {
                KnowledgeLog.Summary("异种人常识：未启用 Biotech DLC，本次跳过。");
                return 0;
            }

            int candidates = 0;
            int written = 0;
            int skippedExisting = 0;
            int skippedEmpty = 0;

            try
            {
                // 先建反向索引，再取条目集合：索引只统计显式声明的派系-异种人关系
                List<Faction> targets = FactionKnowledgeBuilder.CollectTargets();
                Dictionary<XenotypeDef, List<string>> factionNamesByXenotype =
                    BuildXenotypeFactionIndex(targets);
                List<XenotypeDef> xenotypes =
                    XenotypeKnowledgeBuilder.CollectEntryXenotypesFromAll(targets);
                candidates = xenotypes.Count;

                for (int i = 0; i < xenotypes.Count; i++)
                {
                    XenotypeDef xenotype = xenotypes[i];
                    try
                    {
                        List<string> factionNames;
                        if (!factionNamesByXenotype.TryGetValue(xenotype, out factionNames))
                        {
                            factionNames = new List<string>();
                        }

                        // 定义原文让位（D43）：查重键取异种人 LabelCap，与社区条目首段标签同口径
                        bool suppressDescription =
                            RimTalkMemoryBridge.HasExternalEntryForTag(xenotype.LabelCap);

                        string tag;
                        string content;
                        if (!XenotypeKnowledgeBuilder.TryBuild(
                            xenotype, factionNames, suppressDescription, out tag, out content))
                        {
                            skippedEmpty++;
                            KnowledgeLog.Detail("忽略（无名称）：" + (xenotype != null ? xenotype.defName : "null"));
                            continue;
                        }

                        List<string> managedIds = RimTalkMemoryBridge.FindManagedIds(
                            tag, FactionKnowledgeConfig.XENOTYPE_CONTENT_PREFIX);
                        if (managedIds.Count > 0)
                        {
                            skippedExisting++;
                            KnowledgeLog.Detail("跳过（已存在）：" + tag);
                            continue;
                        }

                        // 重要度分档（D43）：异种人归入「其它」档，对齐社区「派系本体」档
                        string id = RimTalkMemoryBridge.AddLore(
                            tag, content, FactionInfoSettings.KnowledgeImportanceOther);
                        if (string.IsNullOrEmpty(id))
                        {
                            KnowledgeLog.Error("写入失败（上游未返回条目 id）：" + tag);
                            continue;
                        }

                        written++;
                        KnowledgeLog.Detail("已写入：" + content);
                    }
                    catch (Exception exception)
                    {
                        KnowledgeLog.Error(
                            "处理异种人失败：" + (xenotype != null ? xenotype.defName : "null"), exception);
                    }
                }
            }
            catch (Exception exception)
            {
                KnowledgeLog.Error("异种人常识注入整体失败（派系常识不受影响）。", exception);
            }

            KnowledgeLog.Summary(
                "异种人常识：候选 " + candidates + " 个，" +
                "写入 " + written + " 条，" +
                "跳过 " + skippedExisting + " 条（已存在），" +
                "忽略 " + skippedEmpty + " 个。");
            return written;
        }

        /// <summary>
        /// 建立「异种人 → 出没派系名」反向索引。
        /// 只统计**显式声明**的关系（<c>FactionDef.xenotypeSet</c> ∪ 主理念 <c>memes[].xenotypeSet</c>
        /// ∪ 兵种级 <c>PawnKindDef.xenotypeSet</c>），**不含基础异种人补差**：
        /// 否则每个派系都会把智人种列进去，列表噪声极大。
        /// </summary>
        private static Dictionary<XenotypeDef, List<string>> BuildXenotypeFactionIndex(List<Faction> factions)
        {
            Dictionary<XenotypeDef, List<string>> index = new Dictionary<XenotypeDef, List<string>>();
            if (factions == null)
            {
                return index;
            }

            for (int i = 0; i < factions.Count; i++)
            {
                Faction faction = factions[i];
                if (faction == null)
                {
                    continue;
                }
                string name = faction.Name;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                List<XenotypeDef> xenotypes = XenotypeKnowledgeBuilder.CollectExplicitXenotypes(faction);
                for (int j = 0; j < xenotypes.Count; j++)
                {
                    XenotypeDef xenotype = xenotypes[j];
                    List<string> names;
                    if (!index.TryGetValue(xenotype, out names))
                    {
                        names = new List<string>();
                        index[xenotype] = names;
                    }
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
            }
            return index;
        }
    }
}
