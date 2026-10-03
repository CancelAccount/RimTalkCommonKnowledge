# Spec 目录导览

本目录是 **Rimtalk Auto Faction Info**（`Cancelation.RimtalkAutoFactionInfo`）的设计与决策记录，随仓库分发（clone 即可阅读）。

## 目录约定

| 位置 | 含义 |
| --- | --- |
| `Spec/*.md` | **活跃文档**：仍有未完成项、长期有效的约定或规范 |
| `Spec/需求文档/*.md` | **需求正文**：`00-需求总览.md` 只做状态索引（状态段 + 篇目索引表），决策表与变更日志随各自 `NN-*.md` 正文 |
| `Spec/需求文档/*证据表*.md` | **取证底账**：反编译实证结论，带 `文件:行号`；跨需求长期复用的常识 |
| `Spec/_archive/*.md` | **归档文档**：任务已完结，仅作历史留痕；其中的引用路径可能已失效 |
| `Spec/README.md` | 本索引 |

维护规则：

1. 任务全部完结后**先迁出活性内容**（仍在生效的约束 → 对应活跃文档），再移入 `_archive/`；
2. 被后续文档完全吸收、无独立价值的文档**直接删除**（删除前须先纳入 git）；
3. 归档文档内部互相引用的相对链接**保持原样**（历史现场），不追改。

## 活跃文档

| 文档 | 作用 | 当前状态 |
| --- | --- | --- |
| [需求文档 / 00-需求总览.md](需求文档/00-需求总览.md) | 本 Spec 的篇目状态索引（总览）：一句话目标 + 状态段 + 篇目索引表 | **已实施**：2026-09-27 按 `spec-doc-format` 精简（原 R / D / OP 表已下沉至 01） |
| [需求文档 / 01-派系常识注入.md](需求文档/01-派系常识注入.md) | 主需求正文：派系常识（FR-1\~FR-8）+ 异种人常识（FR-9）+ 扩展开关（FR-10）+ 内容校正与覆写冷却（FR-11/FR-12）+ 我方派系介绍（FR-13）+ 派系界面「实际成员」显示（FR-14）；含决策表 D1\~D42、实施步骤 S1\~S13、变更日志 | **已实施 · 验证待做**：S1\~S6、S9\~S13 落地，S7 游戏内复验待做（见该篇头部 **口径** 行） |
| [需求文档 / 02-接口取证证据表.md](需求文档/02-接口取证证据表.md) | 反编译实证底账：上游注入 API 与匹配规则、原版时机链、Biotech/异种人 API、`GameComponent` 钩子与 `DebugAction`、我方派系取值（开局剧本 / 人口 / 机械族型号 / 据点 / 气候 / 飞船 / 财富 / 时长）、玩家派系自身不可取关系、财富重算越界陷阱、名字可变的改名识别口径、隐藏派系数据面、异种人来源口径（含兵种级）、HAR/alien race 成员种族口径、派系界面文本源与 `Verse.Mod`/`ModSettings` 设置机制、Harmony 补丁口径（证据 **①\~㊺**） | **已实施**：核查完成（2026-09-29，2026-10-03 补证据 ㊸㊹㊺），接口变更时须优先复跑 |

## 相关外部文档

- `About/About.xml`（项目根）：依赖声明（`cj.rimtalk.expandmemory` 为硬依赖；`loadAfter` 含 `brrainz.harmony`，为 FR-14 界面补丁提供 Harmony 运行时）。
- `e:\steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll`（HarmonyMod）：编译期引用来源；游戏自身 `Managed` 目录**不含**此 dll（证据 ㊺）。
- 上游取证对象：
  - `e:\steam\steamapps\workshop\content\294100\3608181242\1.6\Assemblies\RimTalkMemoryPatch.dll`（注入 API 提供方）
  - `e:\steam\steamapps\common\RimWorld\Source_Decompiled\Assembly-CSharp`（原版 1.6 反编译）
  - `e:\steam\steamapps\common\RimWorld\Data\Biotech`（异种人 / 基因 Def 与中文译文；证据 ㉕）
