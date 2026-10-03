using Verse;

namespace RimtalkAutoFactionInfo
{
    /// <summary>
    /// 本 mod 的可持久化设置（游戏「选项 → Mod 设置」页读写）。
    /// 字段一律 <c>public</c> 并由 <see cref="ExposeData"/> 落盘；改字段/键名会丢失存量设置。
    /// 实例由 <see cref="FactionInfoMod"/> 在构造时载入并存入其静态字段，供补丁只读访问。
    /// </summary>
    public class FactionInfoSettings : ModSettings
    {
        /// <summary>
        /// 是否在派系界面（派系列表悬停提示、派系信息卡）追加「实际成员」构成。
        /// 默认开启：游戏自带的「成员异种人概率」在 HAR 种族 / 兵种级异种人派系上会显示
        /// 「智人种 100%」这类误导内容，本项用于把那层修正显示出来；
        /// 不喜欢多余文本的玩家可关闭，关闭后常识注入不受影响。
        /// </summary>
        public bool showCompositionInFactionUi = true;

        /// <summary>读写设置文件（引擎在载入与关闭设置窗时调用）。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(
                ref showCompositionInFactionUi,
                FactionKnowledgeConfig.SETTINGS_KEY_SHOW_UI_COMPOSITION,
                true);
        }
    }
}
