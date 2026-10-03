using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 本 mod 的入口：<see cref="Verse.Mod"/> 子类由引擎在启动期反射实例化
    /// （<c>LoadedModManager.CreateModClasses</c>，构造签名必须为 <c>(ModContentPack)</c>）。
    /// 职责：① 载入可持久化设置；② 应用 Harmony 补丁，把「实际成员构成」追加到派系界面。
    /// 与 <see cref="FactionKnowledgeComponent"/>（常识注入）互不依赖：本类只管 UI 增强。
    /// </summary>
    public class FactionInfoMod : Mod
    {
        /// <summary>
        /// 全局设置实例：构造时载入，供派系界面补丁只读访问。
        /// 补丁理论上晚于本构造（补丁就在本构造里应用），但访问方仍做 null 兜底。
        /// </summary>
        public static FactionInfoSettings Settings;

        /// <summary>本 mod 的 Harmony 实例，持有补丁集以便排查冲突。</summary>
        private readonly Harmony harmony;

        /// <summary>引擎反射实例化入口：载入设置后立即应用补丁。</summary>
        /// <param name="content">本 mod 的内容包。</param>
        public FactionInfoMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FactionInfoSettings>();

            harmony = new Harmony(FactionKnowledgeConfig.HARMONY_ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>Mod 设置页的分类名（返回空串会使本 mod 从设置列表消失）。</summary>
        public override string SettingsCategory()
        {
            return FactionKnowledgeConfig.MOD_SETTINGS_CATEGORY;
        }

        /// <summary>Mod 设置页内容：目前仅一个「派系界面显示实际成员构成」开关。</summary>
        /// <param name="inRect">设置页可用区域。</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                FactionKnowledgeConfig.MOD_SETTINGS_SHOW_UI_COMPOSITION,
                ref Settings.showCompositionInFactionUi,
                FactionKnowledgeConfig.MOD_SETTINGS_SHOW_UI_COMPOSITION_TIP);
            listing.End();
        }
    }
}
